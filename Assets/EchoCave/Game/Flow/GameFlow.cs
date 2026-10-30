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
    public sealed class GameFlow
    {
        private readonly IBoardView _view;
        private readonly IReadOnlyList<LevelDefinition> _levels;
        private readonly MoveResolver _resolver;
        private readonly EventBus _events;
        private readonly StateMachine<FlowState> _machine = new StateMachine<FlowState>();

        private BoardPresenter _presenter;
        private bool _playRequested;
        private bool _retryRequested;
        private bool _nextRequested;
        private bool _menuRequested;

        public int LevelIndex { get; private set; }
        public int LevelCount => _levels.Count;
        public LevelDefinition CurrentLevel => _levels[LevelIndex];

        // Null in the menu.
        public LevelSession Session { get; private set; }

        public GameState State => _machine.CurrentState.Id;

        public GameFlow(IBoardView view, IReadOnlyList<LevelDefinition> levels, MoveResolver resolver, EventBus events)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _events = events ?? throw new ArgumentNullException(nameof(events));

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

        public void Tick() => _machine.Tick();

        public void HandleTap(Vector2 world)
        {
            if (State == GameState.Playing) _presenter.HandleTap(world);
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
            if (State != GameState.Won) return;

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

        internal void ShowMenu()
        {
            _presenter = null;
            Session = null;
            _view.Clear();
        }

        internal void BeginLevel()
        {
            LevelDefinition level = CurrentLevel;
            Session = LevelBuilder.Build(level);
            _presenter = new BoardPresenter(Session, _resolver, new CommandHistory(), _view);
            _presenter.Start();
            _events.Publish(new LevelStarted(LevelIndex, level.MoveLimit));
        }

        internal void ReportWon()
        {
            int moves = Session.MovesUsed;
            _events.Publish(new LevelWon(LevelIndex, moves, CurrentLevel.StarsFor(moves)));
        }

        internal void ReportLost()
        {
            _events.Publish(new LevelLost(LevelIndex, Session.MovesUsed));
        }

        // True once the level has reached this status and no move is still animating.
        private bool Settled(LevelStatus status)
        {
            return !_presenter.IsBusy && LevelRules.Evaluate(Session, CurrentLevel) == status;
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
