using EchoCave.Ads;
using System.Collections.Generic;
using EchoCave.Core;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Composition root of the Game scene: builds the flow, hands it the taps, ticks it, and frames the
    // camera whenever a level starts (boards can differ in size from level to level).
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private EchoSettings echoSettings;
        [SerializeField] private HintSettings hintSettings;
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardCamera boardCamera;
        [SerializeField] private TapInput tapInput;
        [Tooltip("Plays the sounds. Without it the game runs silent.")]
        [SerializeField] private SoundPlayer soundPlayer;
        [Tooltip("Plays the music. Without it the game has no music.")]
        [SerializeField] private MusicPlayer musicPlayer;

        [Header("Challenges")]
        [Tooltip("The challenges. Without it the game has none.")]
        [SerializeField] private ChallengeCatalog challengeCatalog;

        [Header("Tutorial")]
        [Tooltip("The tutorial lessons. Without it the game has no tutorial and no \"How to play\" button.")]
        [SerializeField] private TutorialCatalog tutorialCatalog;
        [Tooltip("Per-build choices about the tutorial. Without it the tutorial starts by itself on the first Play.")]
        [SerializeField] private TutorialSettings tutorialSettings;

        public GameFlow Flow { get; private set; }

        // The audio service of this scene, for the UI to play its own sounds through. Set in Awake.
        public IAudioService Audio => _audio;

        // The hint stock, for the HUD to show and for the ad refill to top up. Set in Awake.
        public IHintStock Hints => _hintStock;

        // The challenge list, for the menu to show. Set in Awake; empty when the game has no challenges.
        public IReadOnlyList<ChallengeEntry> Challenges => _challenges;

        // The best results on every challenge: this device's, for now. Set in Awake.
        public IChallengeLeaderboard Leaderboard => _leaderboard;

        // The challenges open once every numbered level has been won.
        public bool ChallengesUnlocked => ChallengeAccess.IsUnlocked(_save.Progress, _levelIds);

        // How many numbered levels are still to win before the challenges open.
        public int ChallengeLevelsLeft => ChallengeAccess.LevelsLeft(_save.Progress, _levelIds);

        // How many times the player has started this challenge, solved or not.
        public int ChallengeAttempts(string challengeId) => _save.Challenges.Get(challengeId).Attempts;

        // The board and its camera, for the tutorial overlay to find a bat on the screen.
        public BoardView Board => boardView;
        public Camera WorldCamera => boardCamera.GetComponent<Camera>();

        // True when a rewarded ad can bring hints back, so the HUD can offer it.
        public bool HintsCanBeRefilled => hintSettings.RefillWithAds;

        private IAdService _ads;
        private ISaveService _save;
        private IAudioService _audio;
        private IAnalyticsService _analytics;
        private AnalyticsRecorder _analyticsRecorder;
        private ProgressRecorder _recorder;
        private SoundCuePlayer _cues;
        private ChallengeSelfCheck _challengeCheck;
        private ChallengeRecorder _challengeRecorder;
        private IChallengeLeaderboard _leaderboard;
        private IReadOnlyList<ChallengeEntry> _challenges = new ChallengeEntry[0];
        private MusicConductor _music;
        private IHintStock _hintStock;
        private HintRefill _hintRefill;
        private HintRewardService _rewards;
        private readonly IDayClock _clock = new SystemDayClock();
        private bool _started;
        private IReadOnlyList<int> _levelIds;

        private void Awake()
        {
            if (hintSettings == null) throw new System.InvalidOperationException("GameRoot has no Hint Settings assigned.");

            // Progress is saved by level id, so a catalog with a missing or repeated id would corrupt saves: stop here, loudly.
            _levelIds = catalog.ToLevelIds();
            IReadOnlyList<string> idErrors = LevelIdRules.Validate(_levelIds);
            if (idErrors.Count > 0)
                throw new System.InvalidOperationException("The level catalog has bad level ids:\n" + string.Join("\n", idErrors));

            // Replace, not Register: with Reload Domain off the registry outlives a Play session.
            _ads = new NullAdService();
            _save = new SaveService(new PlayerPrefsSaveStore(), hintSettings.StartingHints);
            _hintStock = new HintStock(_save, hintSettings.MaxHints, EventBus.Global, hintSettings.ReserveSlots);
            _leaderboard = new LocalChallengeLeaderboard(_save);
            if (soundPlayer != null)
            {
                if (musicPlayer == null) Debug.LogWarning("GameRoot has no Music Player assigned, so the game will have no music.", this);
                _audio = new AudioService(soundPlayer, musicPlayer);
            }
            else
            {
                Debug.LogError("GameRoot has no Sound Player assigned, so the game will be silent.", this);
                _audio = new NullAudioService();
            }

            // The switches the player left at the end of the last session.
            _audio.SetEnabled(AudioChannel.Sound, _save.SoundOn);
            _audio.SetEnabled(AudioChannel.Music, _save.MusicOn);

            // No provider yet (D-037): the editor and development builds log the events, a release build counts nothing.
            _analytics = Application.isEditor || Debug.isDebugBuild
                ? (IAnalyticsService)new LogAnalyticsService(Debug.Log)
                : new NullAnalyticsService();
            ServiceLocator.Replace<IAdService>(_ads);
            ServiceLocator.Replace<ISaveService>(_save);
            ServiceLocator.Replace<IAudioService>(_audio);
            ServiceLocator.Replace<IAnalyticsService>(_analytics);
            ServiceLocator.Replace<IChallengeLeaderboard>(_leaderboard);

            var echoStrategy = new RevealAllHiddenStrategy(echoSettings.RevealSeconds);
            IReadOnlyList<TutorialLesson> lessons = tutorialCatalog != null ? tutorialCatalog.ToLessons() : null;
            if (challengeCatalog != null) _challenges = challengeCatalog.ToEntries();
            Flow = new GameFlow(
                boardView, catalog.ToDefinitions(), new MoveResolver(), EventBus.Global, echoStrategy, _hintStock, lessons,
                _challenges, () => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _hintRefill = new HintRefill(Flow, _hintStock, _ads, hintSettings.HintsPerAd, hintSettings.RefillWithAds);
        }

        private void OnEnable()
        {
            tapInput.Tapped += OnTapped;

            // Each strategy picks up where the save left off: the streak so far, the last day a gift was given.
            var strategies = new List<IHintRewardStrategy>
            {
                new DailyReward(hintSettings.HintsPerDay, _save.GetRewardState(DailyReward.StateKey)),
                new StreakReward(hintSettings.WinStreakLength, hintSettings.HintsPerStreak, _save.GetRewardState(StreakReward.StateKey))
            };
            _rewards = new HintRewardService(strategies, _hintStock, _save, EventBus.Global, _clock);
            _recorder = new ProgressRecorder(EventBus.Global, _save, _audio, _levelIds, _rewards);
            _analyticsRecorder = new AnalyticsRecorder(EventBus.Global, _analytics, _levelIds, Debug.LogException);
            _cues = new SoundCuePlayer(EventBus.Global, _audio);
            if (musicPlayer != null && musicPlayer.Bank != null)
            {
                MusicBank music = musicPlayer.Bank;
                foreach (string problem in music.FindProblems()) Debug.LogWarning($"MusicBank: {problem}", music);
                _music = new MusicConductor(EventBus.Global, _audio, music.MenuTrack, music.PlayTracks, music.SecondsPerTrack);
            }
            if (_challenges.Count > 0)
            {
                _challengeCheck = new ChallengeSelfCheck(
                    EventBus.Global, _challenges, new MoveResolver(), challengeCatalog.MinSecondsPerTap, Debug.Log, Debug.LogError);
                _challengeRecorder = new ChallengeRecorder(EventBus.Global, _save);
            }

            boardView.BatRevealed += OnBatRevealed;
            EventBus.Global.Subscribe<LevelStarted>(OnLevelStarted);
            EventBus.Global.Subscribe<ChallengeStarted>(OnChallengeStarted);
        }

        private void OnDisable()
        {
            tapInput.Tapped -= OnTapped;
            _recorder.Dispose();
            _rewards.Dispose();
            _cues.Dispose();
            _music?.Dispose();
            _music = null;
            boardView.BatRevealed -= OnBatRevealed;
            EventBus.Global.Unsubscribe<LevelStarted>(OnLevelStarted);
            EventBus.Global.Unsubscribe<ChallengeStarted>(OnChallengeStarted);
            _analyticsRecorder?.Dispose();
            _analyticsRecorder = null;
            _challengeCheck?.Dispose();
            _challengeCheck = null;
            _challengeRecorder?.Dispose();
            _challengeRecorder = null;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(_ads);
            ServiceLocator.Unregister(_save);
            ServiceLocator.Unregister(_audio);
            ServiceLocator.Unregister(_analytics);
            ServiceLocator.Unregister(_leaderboard);
        }

        // The UI composes in its own Start, which runs first (UiRoot has an earlier execution order), so the HUD
        // is listening when a daily gift arrives.
        private void Start()
        {
            _started = true;
            _rewards.CheckDay();
        }

        // A phone can stay in the background past midnight: the gift is checked again on the way back.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _started) _rewards.CheckDay();
        }

        private void Update()
        {
            Flow.Tick(Time.deltaTime);
            _music?.Tick(Time.unscaledDeltaTime);
        }

        // What the Play button calls. The very first time, and until the tutorial has been finished or skipped, Play
        // starts the tutorial (unless this build turned that off) and the first level follows it; otherwise it starts
        // the first level.
        public void RequestPlay()
        {
            bool autoStart = tutorialSettings == null || tutorialSettings.AutoStart;
            if (TutorialEntry.ShouldAutoStart(Flow.HasTutorial, autoStart, _save.TutorialSeen)) Flow.RequestTutorial(thenStartGame: true);
            else Flow.RequestPlay();
        }

        // What the hint button calls: a hint, or a rewarded ad first when none are left and the settings allow it.
        public void RequestHint()
        {
            _hintRefill.Request();
        }

        private void OnTapped(Vector2 world)
        {
            Flow.HandleTap(world);
        }

        private void OnBatRevealed()
        {
            _audio.Play(SoundCue.Reveal);
        }

        private void OnLevelStarted(LevelStarted started)
        {
            boardCamera.Frame(boardView);
        }

        // A challenge board can be a different size from every level, so the camera frames it too.
        private void OnChallengeStarted(ChallengeStarted started)
        {
            boardCamera.Frame(boardView);
        }

        // What the challenge list calls: starts the challenge at this position, from the menu, once the challenges are
        // unlocked. Returns false when they are not.
        public bool RequestChallenge(int index)
        {
            if (!ChallengesUnlocked) return false;

            Flow.RequestChallenge(index);
            return true;
        }

        // Developer entries, in the component's context menu while the game is running. The challenges menu replaces them.
        [ContextMenu("Challenges/Play the first challenge")]
        private void DebugPlayFirstChallenge()
        {
            if (Application.isPlaying) Flow.RequestChallenge(0);
        }

        [ContextMenu("Challenges/Back to the menu")]
        private void DebugBackToMenu()
        {
            if (Application.isPlaying) Flow.RequestMenu();
        }
    }
}
