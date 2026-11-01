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
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardCamera boardCamera;
        [SerializeField] private TapInput tapInput;
        [Tooltip("Plays the sounds. Without it the game runs silent.")]
        [SerializeField] private SoundPlayer soundPlayer;

        public GameFlow Flow { get; private set; }

        private IAdService _ads;
        private ISaveService _save;
        private IAudioService _audio;
        private IAnalyticsService _analytics;
        private ProgressRecorder _recorder;
        private SoundCuePlayer _cues;
        private IReadOnlyList<int> _levelIds;

        private void Awake()
        {
            // Progress is saved by level id, so a catalog with a missing or repeated id would corrupt saves: stop here, loudly.
            _levelIds = catalog.ToLevelIds();
            IReadOnlyList<string> idErrors = LevelIdRules.Validate(_levelIds);
            if (idErrors.Count > 0)
                throw new System.InvalidOperationException("The level catalog has bad level ids:\n" + string.Join("\n", idErrors));

            // Replace, not Register: with Reload Domain off the registry outlives a Play session.
            _ads = new NullAdService();
            _save = new SaveService(new PlayerPrefsSaveStore());
            if (soundPlayer != null)
            {
                _audio = new AudioService(soundPlayer);
            }
            else
            {
                Debug.LogError("GameRoot has no Sound Player assigned, so the game will be silent.", this);
                _audio = new NullAudioService();
            }

            _analytics = new NullAnalyticsService();
            ServiceLocator.Replace<IAdService>(_ads);
            ServiceLocator.Replace<ISaveService>(_save);
            ServiceLocator.Replace<IAudioService>(_audio);
            ServiceLocator.Replace<IAnalyticsService>(_analytics);

            var echoStrategy = new RevealAllHiddenStrategy(echoSettings.RevealSeconds);
            Flow = new GameFlow(boardView, catalog.ToDefinitions(), new MoveResolver(), EventBus.Global, echoStrategy);
        }

        private void OnEnable()
        {
            tapInput.Tapped += OnTapped;
            _recorder = new ProgressRecorder(EventBus.Global, _save, _audio, _levelIds);
            _cues = new SoundCuePlayer(EventBus.Global, _audio);
            boardView.BatRevealed += OnBatRevealed;
            EventBus.Global.Subscribe<LevelStarted>(OnLevelStarted);
        }

        private void OnDisable()
        {
            tapInput.Tapped -= OnTapped;
            _recorder.Dispose();
            _cues.Dispose();
            boardView.BatRevealed -= OnBatRevealed;
            EventBus.Global.Unsubscribe<LevelStarted>(OnLevelStarted);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(_ads);
            ServiceLocator.Unregister(_save);
            ServiceLocator.Unregister(_audio);
            ServiceLocator.Unregister(_analytics);
        }

        private void Update()
        {
            Flow.Tick(Time.deltaTime);
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
    }
}
