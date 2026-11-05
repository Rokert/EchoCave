using System;
using EchoCave.Game;
using Rokert.Core;

namespace EchoCave.UI
{
    // Keeps the HUD in step with the game by listening to the EventBus, and forwards the ping and hint buttons and
    // the pause button to the game. Plain C#; Dispose unsubscribes from everything.
    public sealed class HudPresenter : IDisposable
    {
        public const string LevelCaption = "LEVEL";
        public const string LessonCaption = "LESSON";
        public const string ChallengeCaption = "CHALLENGE";

        private readonly IHudView _view;
        private readonly EventBus _events;
        private readonly Action _onPing;
        private readonly Action _onHint;
        private readonly Action _onTogglePause;
        private readonly int _levelCount;
        private readonly bool _hintsCanBeRefilled;

        private int _pingsLeft;
        private int _hintsLeft;
        private bool _echoActive;

        // hintCount is what the player holds when the HUD is built; HintsChanged keeps it right afterwards. When hints
        // can be refilled with an ad, a button with none left offers "AD" instead of looking dead.
        public HudPresenter(
            IHudView view, EventBus events, Action onPing, Action onHint, Action onTogglePause, int levelCount, int hintCount,
            bool hintsCanBeRefilled, int reserveCount = 0)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _onPing = onPing ?? throw new ArgumentNullException(nameof(onPing));
            _onHint = onHint ?? throw new ArgumentNullException(nameof(onHint));
            _onTogglePause = onTogglePause ?? throw new ArgumentNullException(nameof(onTogglePause));
            _levelCount = levelCount;
            _hintsLeft = hintCount;
            _hintsCanBeRefilled = hintsCanBeRefilled;

            _view.PingPressed += _onPing;
            _view.HintPressed += _onHint;
            _view.PausePressed += _onTogglePause;

            _events.Subscribe<LevelStarted>(OnLevelStarted);
            _events.Subscribe<LessonStarted>(OnLessonStarted);
            _events.Subscribe<ChallengeStarted>(OnChallengeStarted);
            _events.Subscribe<MoveSpent>(OnMoveSpent);
            _events.Subscribe<PingUsed>(OnPingUsed);
            _events.Subscribe<EchoExpired>(OnEchoExpired);
            _events.Subscribe<PingRefused>(OnPingRefused);
            _events.Subscribe<HintsChanged>(OnHintsChanged);
            _events.Subscribe<HintRefused>(OnHintRefused);
            _events.Subscribe<HintReserveChanged>(OnHintReserveChanged);
            _events.Subscribe<HintRewarded>(OnHintRewarded);
            _events.Subscribe<PauseChanged>(OnPauseChanged);
            _events.Subscribe<MenuOpened>(OnMenuOpened);

            ShowHints();
            _view.SetHintReserve(reserveCount);

            // Nothing to show until a level starts.
            _view.SetVisible(false);
        }

        public void Dispose()
        {
            _view.PingPressed -= _onPing;
            _view.HintPressed -= _onHint;
            _view.PausePressed -= _onTogglePause;

            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
            _events.Unsubscribe<LessonStarted>(OnLessonStarted);
            _events.Unsubscribe<ChallengeStarted>(OnChallengeStarted);
            _events.Unsubscribe<MoveSpent>(OnMoveSpent);
            _events.Unsubscribe<PingUsed>(OnPingUsed);
            _events.Unsubscribe<EchoExpired>(OnEchoExpired);
            _events.Unsubscribe<PingRefused>(OnPingRefused);
            _events.Unsubscribe<HintsChanged>(OnHintsChanged);
            _events.Unsubscribe<HintRefused>(OnHintRefused);
            _events.Unsubscribe<HintReserveChanged>(OnHintReserveChanged);
            _events.Unsubscribe<HintRewarded>(OnHintRewarded);
            _events.Unsubscribe<PauseChanged>(OnPauseChanged);
            _events.Unsubscribe<MenuOpened>(OnMenuOpened);
        }

        private void OnLevelStarted(LevelStarted started)
        {
            ShowLevel(LevelCaption, started.LevelIndex + 1, _levelCount, started.MoveLimit, started.PingBudget);
        }

        // A tutorial lesson looks like a level to the HUD, with its own word and its own total.
        private void OnLessonStarted(LessonStarted started)
        {
            ShowLevel(LessonCaption, started.LessonIndex + 1, started.LessonCount, started.MoveLimit, started.PingBudget);
        }

        // A challenge looks like a level to the HUD, numbered by its place in the challenge list.
        private void OnChallengeStarted(ChallengeStarted started)
        {
            ShowLevel(ChallengeCaption, started.ChallengeIndex + 1, started.ChallengeCount, started.MoveLimit, started.PingBudget, hintsAvailable: false);
        }

        private void ShowLevel(string caption, int number, int total, int moveLimit, int pingBudget, bool hintsAvailable = true)
        {
            _pingsLeft = pingBudget;
            _echoActive = false;

            _view.SetVisible(true);
            _view.SetLevelCaption(caption);
            _view.SetLevel(number, total);
            _view.SetMoves(0, moveLimit);
            _view.SetPings(_pingsLeft);
            _view.SetPingState(CurrentPingState());
            _view.SetHintVisible(hintsAvailable);
            _view.SetPaused(false);
        }

        private void OnMoveSpent(MoveSpent spent)
        {
            _view.SetMoves(spent.MovesUsed, spent.MoveLimit);
        }

        private void OnPingUsed(PingUsed used)
        {
            _pingsLeft = used.PingsLeft;
            _echoActive = true;

            _view.SetPings(_pingsLeft);
            _view.SetPingState(CurrentPingState());
        }

        private void OnEchoExpired(EchoExpired expired)
        {
            _echoActive = false;
            _view.SetPingState(CurrentPingState());
        }

        private void OnPingRefused(PingRefused refused)
        {
            _view.ShakePing();
        }

        private void OnHintsChanged(HintsChanged changed)
        {
            _hintsLeft = changed.Count;
            ShowHints();
        }

        private void OnHintRefused(HintRefused refused)
        {
            _view.ShakeHint();
        }

        private void OnHintReserveChanged(HintReserveChanged changed)
        {
            _view.SetHintReserve(changed.Count);
        }

        // A free hint arrived, in the stock or in the reserve: the button pops.
        private void OnHintRewarded(HintRewarded rewarded)
        {
            _view.PulseHint();
        }

        private void ShowHints()
        {
            _view.SetHints(_hintsLeft);
            _view.SetHintState(HintState());
        }

        private HintButtonState HintState()
        {
            if (_hintsLeft > 0) return HintButtonState.Ready;
            return _hintsCanBeRefilled ? HintButtonState.WatchAd : HintButtonState.Empty;
        }

        private void OnPauseChanged(PauseChanged changed)
        {
            _view.SetPaused(changed.IsPaused);
        }

        private void OnMenuOpened(MenuOpened opened)
        {
            _view.SetVisible(false);
        }

        private PingButtonState CurrentPingState()
        {
            if (_echoActive) return PingButtonState.Active;
            return _pingsLeft > 0 ? PingButtonState.Ready : PingButtonState.Empty;
        }
    }
}
