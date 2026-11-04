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
    // The tutorial (D-034) runs on the same machinery in a mode of its own: IsTutorial swaps the catalog for the lessons,
    // LevelIndex then counts lessons, and the lessons publish LessonStarted/Won/Lost instead of the level events, so
    // progress, stars, rewards and popups never see them. Lessons spend no hints from the stock and never offer an ad.
    //
    // Each attempt gets its own EchoService, so a retry restores the pings. The echo clock only runs while
    // Playing, and a ping acts at once instead of waiting for a Tick (D-019).
    public sealed class GameFlow : IHintGate
    {
        private readonly IBoardView _view;
        private readonly IReadOnlyList<LevelDefinition> _levels;
        private readonly MoveResolver _resolver;
        private readonly EventBus _events;
        private readonly IEchoStrategy _echoStrategy;
        private readonly IHintStock _hints;
        private readonly HintPicker _hintPicker;
        private readonly IReadOnlyList<TutorialLesson> _lessons;
        private readonly StateMachine<FlowState> _machine = new StateMachine<FlowState>();

        private BoardPresenter _presenter;
        private EchoService _echo;

        // The bat the current hint points at, or -1. The hint stays valid for as long as that bat is on the board.
        private int _hintedBatId = -1;
        private bool _playRequested;
        private bool _retryRequested;
        private bool _nextRequested;
        private bool _menuRequested;

        // Set by RequestTutorial and consumed by BeginLevel. Not an intent: it must outlive the state change.
        private bool _tutorialPending;
        private bool _startGameAfterTutorial;

        // The catalog position, or the lesson number while IsTutorial.
        public int LevelIndex { get; private set; }
        public int LevelCount => _levels.Count;
        public LevelDefinition CurrentLevel => IsTutorial ? _lessons[LevelIndex].Level : _levels[LevelIndex];

        // Null when the game was built without lessons.
        public TutorialRunner Tutorial { get; }

        public bool HasTutorial => Tutorial != null;

        // True while the lessons are running instead of the catalog.
        public bool IsTutorial { get; private set; }

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
            IEchoStrategy echoStrategy,
            IHintStock hints = null,
            IReadOnlyList<TutorialLesson> lessons = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _echoStrategy = echoStrategy ?? throw new ArgumentNullException(nameof(echoStrategy));
            _hints = hints ?? new NoHintStock();
            _hintPicker = new HintPicker(_resolver);

            if (levels.Count == 0) throw new ArgumentException("The game needs at least one level.", nameof(levels));

            // Fail at startup with the level's number, not halfway through a game when the player taps Next.
            for (int i = 0; i < levels.Count; i++)
            {
                IReadOnlyList<string> errors = LevelValidator.Validate(levels[i]);
                if (errors.Count > 0)
                    throw new ArgumentException($"Level {i + 1} of the catalog is invalid: {string.Join(" ", errors)}", nameof(levels));
            }

            if (lessons != null && lessons.Count > 0)
            {
                IReadOnlyList<string> lessonErrors = TutorialValidator.Validate(lessons);
                if (lessonErrors.Count > 0)
                    throw new ArgumentException($"The tutorial is invalid: {string.Join(" ", lessonErrors)}", nameof(lessons));

                _lessons = lessons;
                Tutorial = new TutorialRunner(lessons);
                Tutorial.Changed += OnTutorialChanged;
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
                    if (IsTutorial) Tutorial.OnLevelRestarted();
                    BeginLevel();
                    break;
                case GameState.Won:
                case GameState.Lost:
                    if (IsTutorial) Tutorial.OnLevelRestarted();
                    _retryRequested = true;
                    break;
            }
        }

        // Won only. After the last level it returns to the menu.
        public void RequestNext()
        {
            // The lessons move on through TutorialContinue.
            if (IsTutorial) return;

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

        // Playing only. Spends one hint to point at a bat that can leave now; the bat pulses until it is tapped, and
        // a hidden one is revealed for the rest of the attempt. It costs no move. A hint that cannot be given
        // spends nothing and tells the player: the HUD gets HintRefused with the reason and the bats shake.
        public void RequestHint()
        {
            switch (CheckHint())
            {
                case HintStatus.Unavailable: return;
                case HintStatus.Busy: RefuseHint(HintRefusal.Busy); return;
                case HintStatus.AlreadyHinted: RefuseHint(HintRefusal.AlreadyHinted); return;
                case HintStatus.NoHintsLeft: RefuseHint(HintRefusal.NoHintsLeft); return;
                case HintStatus.NotInLesson: RefuseHint(HintRefusal.NotInLesson); return;
            }

            HintPick? pick = _hintPicker.Pick(Session.Board);
            if (!pick.HasValue)
            {
                RefuseHint(HintRefusal.NothingToHint);
                return;
            }

            // A lesson's demo hint is free: the player's stock is not touched.
            if (!IsTutorial && !_hints.TrySpend())
            {
                RefuseHint(HintRefusal.NoHintsLeft);
                return;
            }

            if (pick.Value.IsHidden)
            {
                _echo.Pin(pick.Value.BatId);
                _view.SetRevealed(_echo.RevealedBatIds);
            }

            _hintedBatId = pick.Value.BatId;
            _view.ShowHint(pick.Value.BatId);
            _events.Publish(new HintUsed(_hints.Count, pick.Value.BatId));
            if (IsTutorial) Tutorial.OnHintUsed();
        }

        // What a hint request would do right now, with no effect: nothing is spent and nothing is published.
        public HintStatus CheckHint()
        {
            if (State != GameState.Playing || IsPaused) return HintStatus.Unavailable;
            if (IsTutorial && !CurrentTutorialLesson.FreeHint) return HintStatus.NotInLesson;
            if (_presenter.IsBusy) return HintStatus.Busy;

            // Removing other bats never blocks a free bat (D-010), so the hinted one stays a good move until it is tapped.
            if (_hintedBatId >= 0 && Session.Board.TryGetBat(_hintedBatId, out _)) return HintStatus.AlreadyHinted;

            // A lesson never spends from the stock, so it never asks for an ad either.
            if (IsTutorial) return HintStatus.Ready;

            return _hints.Count <= 0 ? HintStatus.NoHintsLeft : HintStatus.Ready;
        }

        private void RefuseHint(HintRefusal reason)
        {
            _events.Publish(new HintRefused(reason));
            _view.ShowRejected();
        }

        // Starts the lessons from the first one. From the menu, from a level (paused or not) or from a won or lost
        // screen. thenStartGame says what happens when they end: true goes on to the first level (the very first run),
        // false returns to the menu (a replay from "How to play"). Ignored when the game has no lessons.
        public void RequestTutorial(bool thenStartGame = false)
        {
            if (Tutorial == null) return;

            _tutorialPending = true;
            _startGameAfterTutorial = thenStartGame;

            switch (State)
            {
                case GameState.Menu:
                    _playRequested = true;
                    break;
                case GameState.Playing:
                    BeginLevel();
                    break;
                case GameState.Won:
                    _nextRequested = true;
                    break;
                case GameState.Lost:
                    _retryRequested = true;
                    break;
            }
        }

        // The Continue button of the overlay. On a step that only has to be read it moves on; after a won lesson it opens
        // the next one, or ends the tutorial after the last.
        public void TutorialContinue()
        {
            if (!IsTutorial) return;

            bool lessonWasWon = Tutorial.Phase == TutorialPhase.LessonComplete;
            Tutorial.Continue();
            if (!lessonWasWon || State != GameState.Won) return;

            if (Tutorial.Phase == TutorialPhase.InStep)
            {
                LevelIndex = Tutorial.LessonIndex;
                _nextRequested = true;
            }
            else
            {
                LeaveTutorial(TutorialEnd.Completed);
            }
        }

        // The Skip button of the overlay: ends the tutorial at once, from any step.
        public void TutorialSkip()
        {
            if (!IsTutorial) return;

            Tutorial.Skip();
            LeaveTutorial(TutorialEnd.Skipped);
        }

        private TutorialLesson CurrentTutorialLesson => _lessons[LevelIndex];

        // Back to the catalog: its first level when the tutorial was the first run, the menu otherwise.
        private void LeaveTutorial(TutorialEnd reason)
        {
            IsTutorial = false;
            _events.Publish(new TutorialEnded(reason));

            if (!_startGameAfterTutorial)
            {
                RequestMenu();
                return;
            }

            LevelIndex = 0;
            switch (State)
            {
                case GameState.Playing:
                    BeginLevel();
                    break;
                case GameState.Won:
                    _nextRequested = true;
                    break;
                case GameState.Lost:
                    _retryRequested = true;
                    break;
            }
        }

        private void OnTutorialChanged()
        {
            _events.Publish(new TutorialChanged(Tutorial.Phase, Tutorial.LessonIndex, Tutorial.LessonCount, Tutorial.CurrentStep));
        }

        private void OnBatTapped(int batId)
        {
            Tutorial.OnBatTapped(batId);
        }

        // A tap the lesson does not allow: the bats shake, like every other refusal (D-019).
        private void OnTapRefused()
        {
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
            _hintedBatId = -1;
            DetachEcho();
            DetachPresenter();
            SetPaused(false);
            Session = null;
            _view.Clear();

            // Back in the menu in the middle of the lessons: nothing was finished, so it is not marked as seen.
            if (IsTutorial)
            {
                IsTutorial = false;
                _events.Publish(new TutorialEnded(TutorialEnd.Left));
            }

            _events.Publish(new MenuOpened());
        }

        internal void BeginLevel()
        {
            _hintedBatId = -1;
            DetachEcho();
            DetachPresenter();
            SetPaused(false);

            if (_tutorialPending)
            {
                _tutorialPending = false;
                IsTutorial = true;
                LevelIndex = 0;
                Tutorial.Start();
            }

            LevelDefinition level = CurrentLevel;
            Session = LevelBuilder.Build(level);
            _presenter = new BoardPresenter(Session, _resolver, new CommandHistory(), _view);
            _presenter.MoveExecuted += OnMoveExecuted;
            if (IsTutorial)
            {
                _presenter.CanTap = Tutorial.CanTap;
                _presenter.BatTapped += OnBatTapped;
                _presenter.TapRefused += OnTapRefused;
            }

            _presenter.Start();

            _echo = new EchoService(_echoStrategy, level.PingBudget);
            _echo.Pinged += OnPinged;
            _echo.Expired += OnExpired;

            if (IsTutorial) _events.Publish(new LessonStarted(LevelIndex, _lessons.Count, level.MoveLimit, level.PingBudget));
            else _events.Publish(new LevelStarted(LevelIndex, level.MoveLimit, level.PingBudget));
        }

        internal void ReportWon()
        {
            int moves = Session.MovesUsed;
            if (IsTutorial)
            {
                Tutorial.OnLevelWon();
                _events.Publish(new LessonWon(LevelIndex, moves));
                return;
            }

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
            if (IsTutorial) _events.Publish(new LessonLost(LevelIndex, Session.MovesUsed));
            else _events.Publish(new LevelLost(LevelIndex, Session.MovesUsed));
        }

        private void OnMoveExecuted(MoveResult result)
        {
            _events.Publish(new MoveSpent(Session.MovesUsed, CurrentLevel.MoveLimit, result.Outcome));
            if (IsTutorial) Tutorial.OnMove(result.Outcome);
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
            _presenter.BatTapped -= OnBatTapped;
            _presenter.TapRefused -= OnTapRefused;
            _presenter.CanTap = null;
            _presenter = null;
        }

        private void OnPinged(EchoPulse pulse)
        {
            _view.ShowPing(_echo.RevealedBatIds);
            _events.Publish(new PingUsed(_echo.PingsLeft, pulse.DurationSeconds));
            if (IsTutorial) Tutorial.OnPinged();
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
