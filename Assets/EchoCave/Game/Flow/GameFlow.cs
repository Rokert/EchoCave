using System;
using System.Collections.Generic;
using EchoCave.Core;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Menu -> Playing -> Won / Lost, and which level is loaded. Plain C#: the composition root feeds it
    // taps and calls Tick every frame.
    //
    // The player's intents (RequestPlay, RequestRetry, ...) only set a flag when they make sense in the
    // current state; the state machine consumes the flag on the next Tick. Won and Lost wait for the move
    // animation to finish, so a popup never shows while a bat is still flying (D-017).
    //
    // Each attempt gets its own EchoService, so a retry restores the pings. The echo clock only runs while
    // Playing, and a ping acts at once instead of waiting for a Tick (D-019).
    public sealed class GameFlow
    {
        private readonly IBoardView _view;
        private readonly IReadOnlyList<LevelDefinition> _levels;
        private readonly MoveResolver _resolver;
        private readonly EventBus _events;
        private readonly IEchoStrategy _echoStrategy;
        private readonly StateMachine<FlowState> _machine = new StateMachine<FlowState>();

        private BoardPresenter _presenter;
        private EchoService _echo;
        private bool _playRequested;
        private bool _retryRequested;
        private bool _nextRequested;
        private bool _menuRequested;

        public int LevelIndex { get; private set; }
        public int LevelCount => _levels.Count;
        public LevelDefinition CurrentLevel => _levels[LevelIndex];

        // Null in the menu.
        public LevelSession Session { get; private set; }

        // Null in the menu.
        public EchoService Echo => _echo;

        // Paused: no taps, no pings, the echo clock stands still, and Won or Lost wait for the resume.
        // Only a level in play can be paused, and retry or the menu end the pause.
        public bool IsPaused { get; private set; }

        public GameState State => _machine.CurrentState.Id;

        public GameFlow(
            IBoardView view,
            IReadOnlyList<LevelDefinition> levels,
            MoveResolver resolver,
            EventBus events,
            IEchoStrategy echoStrategy)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _echoStrategy = echoStrategy ?? throw new ArgumentNullException(nameof(echoStrategy));

            if (levels.Count == 0) throw new ArgumentException("The game needs at least one level.", nameof(levels));

            // Fail at startup with the level's number, not halfway through a game when the player taps Next.
            for (int i = 0; i < levels.Count; i++)
            {
                IReadOnlyList<string> errors = LevelValidator.Validate(levels[i]);
                if (errors.Count > 0)
                    throw new ArgumentException($"Level {i + 1} of the catalog is invalid: {string.Join(" ", errors)}", nameof(levels));
            }

            var menu = new MenuState(this);
            var playing = new PlayingState(this);
            var won = new WonState(this);
            var lost = new LostState(this);

            // Registration order matters: the first transition that matches wins.
            _machine.AddTransition(menu, playing, () => _playRequested);
            _machine.AddTransition(playing, won, () => Settled(LevelStatus.Won));
            _machine.AddTransition(playing, lost, () => Settled(LevelStatus.Lost));
            _machine.AddTransition(playing, menu, () => _menuRequested);
            _machine.AddTransition(won, playing, () => _retryRequested || _nextRequested);
            _machine.AddTransition(won, menu, () => _menuRequested);
            _machine.AddTransition(lost, playing, () => _retryRequested);
            _machine.AddTransition(lost, menu, () => _menuRequested);

            // A stale flag must never fire a transition in a later state.
            _machine.StateChanged += (_, __) => ClearIntents();
            _machine.Start(menu);
        }

        public void Tick(float deltaSeconds)
        {
            _machine.Tick();

            // After the transitions, so a level that was just won or lost stops its clock this frame.
            if (State == GameState.Playing && !IsPaused) _echo.Tick(deltaSeconds);
        }

        public void HandleTap(Vector2 world)
        {
            if (State == GameState.Playing && !IsPaused) _presenter.HandleTap(world);
        }

        // Menu only.
        public void RequestPlay(int levelIndex = 0)
        {
            if (levelIndex < 0 || levelIndex >= _levels.Count)
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, null);
            if (State != GameState.Menu) return;

            LevelIndex = levelIndex;
            _playRequested = true;
        }

        // Playing restarts the level in place; Won and Lost go back into it. Ignored in the menu.
        public void RequestRetry()
        {
            switch (State)
            {
                case GameState.Playing:
                    BeginLevel();
                    break;
                case GameState.Won:
                case GameState.Lost:
                    _retryRequested = true;
                    break;
            }
        }

        // Won only. After the last level it returns to the menu.
        public void RequestNext()
        {
            // A second press before the next Tick must not move on again and skip a level.
            if (State != GameState.Won || _nextRequested || _menuRequested) return;

            if (LevelIndex + 1 < _levels.Count)
            {
                LevelIndex++;
                _nextRequested = true;
            }
            else
            {
                _menuRequested = true;
            }
        }

        // Anywhere but the menu.
        public void RequestMenu()
        {
            if (State != GameState.Menu) _menuRequested = true;
        }

        // Playing only. Works at once, even while a move animates. A ping that cannot do anything tells the
        // player: the HUD gets PingRefused with the reason and the bats shake.
        public void RequestPing()
        {
            if (State != GameState.Playing || IsPaused) return;

            PingResult result = _echo.TryPing(Session.Board);
            if (result == PingResult.Pinged) return;

            _events.Publish(new PingRefused(result));
            _view.ShowRejected();
        }

        // Playing only.
        public void RequestPause()
        {
            if (State == GameState.Playing) SetPaused(true);
        }

        public void RequestResume()
        {
            SetPaused(false);
        }

        internal void ShowMenu()
        {
            DetachEcho();
            DetachPresenter();
            SetPaused(false);
            Session = null;
            _view.Clear();
            _events.Publish(new MenuOpened());
        }

        internal void BeginLevel()
        {
            DetachEcho();
            DetachPresenter();
            SetPaused(false);

            LevelDefinition level = CurrentLevel;
            Session = LevelBuilder.Build(level);
            _presenter = new BoardPresenter(Session, _resolver, new CommandHistory(), _view);
            _presenter.MoveExecuted += OnMoveExecuted;
            _presenter.Start();

            _echo = new EchoService(_echoStrategy, level.PingBudget);
            _echo.Pinged += OnPinged;
            _echo.Expired += OnExpired;

            _events.Publish(new LevelStarted(LevelIndex, level.MoveLimit, level.PingBudget));
        }

        internal void ReportWon()
        {
            int moves = Session.MovesUsed;
            _events.Publish(new LevelWon(LevelIndex, moves, CurrentLevel.StarsFor(moves)));
        }

        // Shows where every hidden bat was pointing, so the player sees what went wrong.
        internal void ReportLost()
        {
            var hidden = new List<int>();
            foreach (Bat bat in Session.Board.Bats)
            {
                if (bat.IsHidden) hidden.Add(bat.Id);
            }

            _view.SetRevealed(hidden);
            _events.Publish(new LevelLost(LevelIndex, Session.MovesUsed));
        }

        private void OnMoveExecuted(MoveResult result)
        {
            _events.Publish(new MoveSpent(Session.MovesUsed, CurrentLevel.MoveLimit, result.Outcome));
        }

        private void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;

            IsPaused = paused;
            _events.Publish(new PauseChanged(paused));
        }

        private void DetachPresenter()
        {
            if (_presenter == null) return;

            _presenter.MoveExecuted -= OnMoveExecuted;
            _presenter = null;
        }

        private void OnPinged(EchoPulse pulse)
        {
            _view.ShowPing(_echo.RevealedBatIds);
            _events.Publish(new PingUsed(_echo.PingsLeft, pulse.DurationSeconds));
        }

        // The service has already cleared its set, so this hides every bat again.
        private void OnExpired()
        {
            _view.SetRevealed(_echo.RevealedBatIds);
            _events.Publish(new EchoExpired());
        }

        private void DetachEcho()
        {
            if (_echo == null) return;

            _echo.Pinged -= OnPinged;
            _echo.Expired -= OnExpired;
            _echo = null;
        }

        // True once the level has reached this status and no move is still animating.
        private bool Settled(LevelStatus status)
        {
            return !IsPaused && !_presenter.IsBusy && LevelRules.Evaluate(Session, CurrentLevel) == status;
        }

        private void ClearIntents()
        {
            _playRequested = false;
            _retryRequested = false;
            _nextRequested = false;
            _menuRequested = false;
        }
    }
}
