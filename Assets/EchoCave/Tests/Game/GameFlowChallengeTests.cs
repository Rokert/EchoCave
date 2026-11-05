using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    public class GameFlowChallengeTests
    {
        private static readonly LevelDefinition Easy = new LevelDefinition(
            3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 0, 1, 1);

        // Bat 1 (middle) leaves; bat 0 (left) is blocked by it. Both hidden, one ping, no limit.
        private static readonly LevelDefinition Chain = new LevelDefinition(
            3, 1,
            new[] { new BatDefinition(new Cell(0, 0), Direction.Right, true), new BatDefinition(new Cell(1, 0), Direction.Right, true) },
            0, 1, 2, 2);

        // The same board with a limit of 2: two bounces lose it.
        private static readonly LevelDefinition Tight = new LevelDefinition(
            3, 1,
            new[] { new BatDefinition(new Cell(0, 0), Direction.Right, false), new BatDefinition(new Cell(1, 0), Direction.Right, false) },
            2, 0, 2, 2);

        private FakeBoardView _view;
        private FakeSaveService _save;
        private EventBus _bus;
        private HintStock _stock;
        private GameFlow _flow;
        private long _now;

        private readonly List<ChallengeStarted> _started = new List<ChallengeStarted>();
        private readonly List<ChallengeSolved> _solved = new List<ChallengeSolved>();
        private readonly List<ChallengeFailed> _failed = new List<ChallengeFailed>();
        private readonly List<LevelStarted> _levelStarted = new List<LevelStarted>();
        private readonly List<LevelWon> _levelWon = new List<LevelWon>();
        private readonly List<LevelLost> _levelLost = new List<LevelLost>();
        private readonly List<HintRefused> _refused = new List<HintRefused>();
        private readonly List<HintUsed> _hintUsed = new List<HintUsed>();
        private int _menuOpened;

        [SetUp]
        public void SetUp()
        {
            _started.Clear();
            _solved.Clear();
            _failed.Clear();
            _levelStarted.Clear();
            _levelWon.Clear();
            _levelLost.Clear();
            _refused.Clear();
            _hintUsed.Clear();
            _menuOpened = 0;
            _now = 1700000000L;

            _view = new FakeBoardView();
            _save = new FakeSaveService(3);
            _bus = new EventBus();
            _bus.Subscribe<ChallengeStarted>(e => _started.Add(e));
            _bus.Subscribe<ChallengeSolved>(e => _solved.Add(e));
            _bus.Subscribe<ChallengeFailed>(e => _failed.Add(e));
            _bus.Subscribe<LevelStarted>(e => _levelStarted.Add(e));
            _bus.Subscribe<LevelWon>(e => _levelWon.Add(e));
            _bus.Subscribe<LevelLost>(e => _levelLost.Add(e));
            _bus.Subscribe<HintRefused>(e => _refused.Add(e));
            _bus.Subscribe<HintUsed>(e => _hintUsed.Add(e));
            _bus.Subscribe<MenuOpened>(e => _menuOpened++);

            _stock = new HintStock(_save, 5, _bus);
        }

        private GameFlow Build(params LevelDefinition[] challenges)
        {
            var entries = challenges.Select((level, i) => new ChallengeEntry($"challenge-{i + 1:000}", level)).ToArray();
            _flow = new GameFlow(
                _view, new[] { Easy }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f), _stock, null, entries, () => _now);
            return _flow;
        }

        private static Vector2 Center(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);

        private void Tap(int x, int y)
        {
            _flow.HandleTap(Center(x, y));
            _view.FinishAnimation();
            _flow.Tick(0f);
        }

        private void StartChallenge(int index = 0)
        {
            _flow.RequestChallenge(index);
            _flow.Tick(0f);
        }

        [Test]
        public void ARequestFromTheMenu_StartsTheChallenge_WithChallengeEvents_AndNoLevelEvents()
        {
            Build(Chain, Tight);

            StartChallenge(1);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.IsTrue(_flow.IsChallenge);
            Assert.AreEqual("challenge-002", _flow.CurrentChallengeId);
            Assert.AreEqual(1, _started.Count);
            Assert.AreEqual(1, _started[0].ChallengeIndex);
            Assert.AreEqual(2, _started[0].ChallengeCount);
            Assert.AreEqual("challenge-002", _started[0].ChallengeId);
            Assert.AreEqual(2, _started[0].MoveLimit);
            Assert.AreEqual(1, _started[0].Attempt);
            Assert.IsEmpty(_levelStarted, "a challenge never publishes LevelStarted");
            Assert.AreEqual(2, _flow.Session.Board.Bats.Count, "the board is the challenge's, not the catalog's");
        }

        [Test]
        public void WithoutChallenges_TheRequestDoesNothing()
        {
            _flow = new GameFlow(_view, new[] { Easy }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f), _stock);

            _flow.RequestChallenge(0);
            _flow.Tick(0f);

            Assert.IsFalse(_flow.HasChallenges);
            Assert.AreEqual(GameState.Menu, _flow.State);
            Assert.IsEmpty(_started);
        }

        [Test]
        public void ARequestOutOfRange_Throws_AndOneOutsideTheMenu_IsIgnored()
        {
            Build(Chain);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _flow.RequestChallenge(1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _flow.RequestChallenge(-1));

            _flow.RequestPlay();
            _flow.Tick(0f);
            _flow.RequestChallenge(0);
            _flow.Tick(0f);

            Assert.IsFalse(_flow.IsChallenge);
            Assert.AreEqual(1, _levelStarted.Count);
            Assert.IsEmpty(_started);
        }

        [Test]
        public void InvalidChallenges_StopTheGameAtStartup_WithTheirPosition()
        {
            var duplicate = new[] { new ChallengeEntry("same", Chain), new ChallengeEntry("same", Tight) };

            var thrown = Assert.Throws<System.ArgumentException>(
                () => new GameFlow(_view, new[] { Easy }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f), _stock, null, duplicate));

            StringAssert.Contains("Challenge 2", thrown.Message);
        }

        [Test]
        public void TheRun_RecordsTapsAndPingsInOrder_AndAWinPublishesTheAttempt_NotALevelWin()
        {
            Build(Chain);
            StartChallenge();

            _flow.RequestPing();
            Tap(0, 0);
            Tap(1, 0);
            Tap(0, 0);

            Assert.AreEqual(GameState.Won, _flow.State);
            Assert.AreEqual(1, _solved.Count);
            Assert.IsEmpty(_levelWon, "a challenge never publishes LevelWon");
            ChallengeAttempt attempt = _solved[0].Attempt;
            Assert.AreEqual("challenge-001", attempt.ChallengeId);
            Assert.AreEqual(1700000000L, attempt.SolvedAtUnixSeconds);
            Assert.AreEqual(
                new[] { ChallengeMoveKind.Ping, ChallengeMoveKind.Tap, ChallengeMoveKind.Tap, ChallengeMoveKind.Tap },
                attempt.Moves.Select(m => m.Kind).ToArray());
            Assert.AreEqual(new[] { 0, 1, 0 }, attempt.Moves.Where(m => m.Kind == ChallengeMoveKind.Tap).Select(m => m.BatId).ToArray());
        }

        [Test]
        public void TheRecordedAttempt_IsOneTheCoreReplayAccepts()
        {
            Build(Chain);
            StartChallenge();
            _flow.RequestPing();
            Tap(1, 0);
            Tap(0, 0);

            ReplayResult result = ChallengeReplay.Verify(Chain, _solved[0].Attempt, new MoveResolver(), 0f);

            Assert.IsTrue(result.IsValid, result.Message);
            Assert.AreEqual(2, result.Moves);
            Assert.AreEqual(1, result.Pings);
        }

        [Test]
        public void ActiveSeconds_CountOnlyTimeOnTheBoardThatIsNotPaused()
        {
            Build(Chain);
            StartChallenge();

            _flow.Tick(1.5f);
            _flow.RequestPause();
            _flow.Tick(10f);
            _flow.RequestResume();
            _flow.Tick(2f);
            Tap(1, 0);
            Tap(0, 0);

            Assert.AreEqual(3.5f, _solved[0].Attempt.ActiveSeconds, 1e-4f);
        }

        [Test]
        public void ARetry_IsANewAttempt_WithAnEmptyRecord()
        {
            Build(Chain);
            StartChallenge();
            Tap(0, 0);
            _flow.Tick(4f);

            _flow.RequestRetry();

            Assert.AreEqual(2, _started.Count);
            Assert.AreEqual(2, _started[1].Attempt);
            Tap(1, 0);
            Tap(0, 0);
            Assert.AreEqual(2, _solved[0].Attempt.Attempts);
            Assert.AreEqual(2, _solved[0].Attempt.Moves.Count, "the bounce of the first attempt is not in this one");
            Assert.AreEqual(0f, _solved[0].Attempt.ActiveSeconds, 1e-4f);
        }

        [Test]
        public void ARetryFromTheWonScreen_StartsAnotherAttempt()
        {
            Build(Chain);
            StartChallenge();
            Tap(1, 0);
            Tap(0, 0);
            Assert.AreEqual(GameState.Won, _flow.State);

            _flow.RequestRetry();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.IsTrue(_flow.IsChallenge);
            Assert.AreEqual(2, _started.Count);
            Assert.AreEqual(2, _started[1].Attempt);
        }

        [Test]
        public void ANewChallenge_StartsItsAttemptsFromOne_AgainAfterTheMenu()
        {
            Build(Chain);
            StartChallenge();
            _flow.RequestRetry();
            Assert.AreEqual(2, _started[1].Attempt);

            _flow.RequestMenu();
            _flow.Tick(0f);
            StartChallenge();

            Assert.AreEqual(1, _started[2].Attempt);
        }

        [Test]
        public void AChallenge_TakesNoHints_AndRefusesVisibly_WithoutTouchingTheStock()
        {
            Build(Chain);
            StartChallenge();
            int before = _stock.Count;

            Assert.AreEqual(HintStatus.NotInChallenge, _flow.CheckHint());
            _flow.RequestHint();

            Assert.AreEqual(before, _stock.Count);
            Assert.IsEmpty(_hintUsed);
            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(HintRefusal.NotInChallenge, _refused[0].Reason);
            Assert.AreEqual(1, _view.RejectedCount, "the bats shake");
            Assert.IsEmpty(_view.HintedBatIds);
        }

        [Test]
        public void ThereIsNoNextChallenge_OrNextLevel_AfterASolve()
        {
            Build(Chain, Tight);
            StartChallenge();
            Tap(1, 0);
            Tap(0, 0);

            _flow.RequestNext();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Won, _flow.State);
            Assert.AreEqual(1, _started.Count);
        }

        [Test]
        public void ARunningOutOfTheMoveLimit_PublishesChallengeFailed_NotALevelLoss()
        {
            Build(Tight);
            StartChallenge();

            Tap(0, 0);
            Tap(0, 0);

            Assert.AreEqual(GameState.Lost, _flow.State);
            Assert.AreEqual(1, _failed.Count);
            Assert.AreEqual(2, _failed[0].MovesUsed);
            Assert.IsEmpty(_levelLost);
            Assert.IsEmpty(_solved);
        }

        [Test]
        public void TheMenu_EndsTheChallenge_AndTheNextPlayIsAnOrdinaryLevel()
        {
            Build(Chain);
            StartChallenge();
            int menuBefore = _menuOpened;

            _flow.RequestMenu();
            _flow.Tick(0f);

            Assert.IsFalse(_flow.IsChallenge);
            Assert.IsNull(_flow.CurrentChallengeId);
            Assert.AreEqual(menuBefore + 1, _menuOpened);

            _flow.RequestPlay();
            _flow.Tick(0f);
            Assert.IsFalse(_flow.IsChallenge);
            Assert.AreEqual(1, _levelStarted.Count);
            Assert.AreEqual(1, _flow.Session.Board.Bats.Count, "the catalog's level, not the challenge");
        }

        [Test]
        public void ALevelOnlyEverCountsInTheLevelEvents_AndAChallengeInItsOwn()
        {
            Build(Chain);
            _flow.RequestPlay();
            _flow.Tick(0f);
            Tap(1, 0);

            Assert.AreEqual(1, _levelWon.Count);
            Assert.IsEmpty(_solved);
        }

        [Test]
        public void StartingTheTutorialInTheMiddleOfAChallenge_LeavesTheChallenge()
        {
            var lesson = new TutorialLesson(
                "Fly", Easy, new[] { new TutorialStep("Tap the bat.", TutorialAdvance.Exited, TutorialFocus.Bat, 0, limitTaps: true), new TutorialStep("Nice.", TutorialAdvance.LevelWon) });
            _flow = new GameFlow(
                _view, new[] { Easy }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f), _stock, new[] { lesson },
                new[] { new ChallengeEntry("challenge-001", Chain) }, () => _now);
            StartChallenge();

            _flow.RequestTutorial();

            Assert.IsTrue(_flow.IsTutorial);
            Assert.IsFalse(_flow.IsChallenge);
            Assert.AreEqual(1, _flow.Session.Board.Bats.Count);
        }

        [Test]
        public void WithoutAClock_TheSolveTimeIsZero()
        {
            var entries = new[] { new ChallengeEntry("challenge-001", Easy) };
            _flow = new GameFlow(_view, new[] { Easy }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f), _stock, null, entries);
            StartChallenge();

            Tap(1, 0);

            Assert.AreEqual(0L, _solved[0].Attempt.SolvedAtUnixSeconds);
        }
    }
}
