using System;
using EchoCave.Game;
using Rokert.Core;

namespace EchoCave.UI
{
    // Keeps the HUD in step with the game by listening to the EventBus, and forwards the two buttons to the
    // game. Plain C#; Dispose unsubscribes from everything.
    public sealed class HudPresenter : IDisposable
    {
        private readonly IHudView _view;
        private readonly EventBus _events;
        private readonly Action _onPing;
        private readonly Action _onTogglePause;

        private int _pingsLeft;
        private bool _echoActive;

        public HudPresenter(IHudView view, EventBus events, Action onPing, Action onTogglePause)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _onPing = onPing ?? throw new ArgumentNullException(nameof(onPing));
            _onTogglePause = onTogglePause ?? throw new ArgumentNullException(nameof(onTogglePause));

            _view.PingPressed += _onPing;
            _view.PausePressed += _onTogglePause;

            _events.Subscribe<LevelStarted>(OnLevelStarted);
            _events.Subscribe<MoveSpent>(OnMoveSpent);
            _events.Subscribe<PingUsed>(OnPingUsed);
            _events.Subscribe<EchoExpired>(OnEchoExpired);
            _events.Subscribe<PingRefused>(OnPingRefused);
            _events.Subscribe<PauseChanged>(OnPauseChanged);
            _events.Subscribe<MenuOpened>(OnMenuOpened);

            // Nothing to show until a level starts.
            _view.SetVisible(false);
        }

        public void Dispose()
        {
            _view.PingPressed -= _onPing;
            _view.PausePressed -= _onTogglePause;

            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
            _events.Unsubscribe<MoveSpent>(OnMoveSpent);
            _events.Unsubscribe<PingUsed>(OnPingUsed);
            _events.Unsubscribe<EchoExpired>(OnEchoExpired);
            _events.Unsubscribe<PingRefused>(OnPingRefused);
            _events.Unsubscribe<PauseChanged>(OnPauseChanged);
            _events.Unsubscribe<MenuOpened>(OnMenuOpened);
        }

        private void OnLevelStarted(LevelStarted started)
        {
            _pingsLeft = started.PingBudget;
            _echoActive = false;

            _view.SetVisible(true);
            _view.SetMoves(0, started.MoveLimit);
            _view.SetPings(_pingsLeft);
            _view.SetPingState(CurrentPingState());
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
