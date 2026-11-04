using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    public class GameFlowTutorialTests
    {
        // Catalog: one bat that leaves freely.
        private static readonly LevelDefinition Easy = new LevelDefinition(
            3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 0, 1, 1);

        // Lesson 0: one bat, tap it.
        private static TutorialLesson Fly() => new TutorialLesson(
            "Fly",
            new LevelDefinition(3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 0, 1, 1),
            new[]
            {
                new TutorialStep("Tap the bat.", TutorialAdvance.Exited, TutorialFocus.Bat, 0, limitTaps: true),
                new TutorialStep("Nice.", TutorialAdvance.LevelWon)
            });

        // Lesson 1: bat 0 (left) is blocked by bat 1. Bounce first, then free the way.
        private static TutorialLesson Blocked() => new TutorialLesson(
            "Blocked",
            new LevelDefinition(
                4, 1,
                new[] { new BatDefinition(new Cell(0, 0), Direction.Right, false), new BatDefinition(new Cell(1, 0), Direction.Right, false) },
                0, 0, 2, 2),
            new[]
            {
                new TutorialStep("Tap the left bat.", TutorialAdvance.Bounced, TutorialFocus.Bat, 0, limitTaps: true),
                new TutorialStep("Now the other one.", TutorialAdvance.Exited, TutorialFocus.Bat, 1, limitTaps: true),
                new TutorialStep("Now it is free.", TutorialAdvance.LevelWon)
            });

        // Lesson 2: a demo hint, free.
        private static TutorialLesson Hint() => new TutorialLesson(
            "Hint",
            new LevelDefinition(3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 0, 1, 1),
            new[]
            {
                new TutorialStep("Press the hint button.", TutorialAdvance.HintUsed, TutorialFocus.HintButton),
                new TutorialStep("Follow it.", TutorialAdvance.LevelWon)
            },
            freeHint: true);

        // Lesson 3: a hidden bat and one ping.
        private static TutorialLesson Echo() => new TutorialLesson(
            "Echo",
            new LevelDefinition(3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, true) }, 0, 1, 1, 1),
            new[]
            {
                new TutorialStep("Ping.", TutorialAdvance.Pinged, TutorialFocus.PingButton),
                new TutorialStep("Tap it.", TutorialAdvance.LevelWon)
            });

        // Lesson 4: a limit of 4 for 2 bats, so four bounces lose it.
        private static TutorialLesson Tight() => new TutorialLesson(
            "Tight",
            new LevelDefinition(
                4, 1,
                new[] { new BatDefinition(new Cell(0, 0), Direction.Right, false), new BatDefinition(new Cell(1, 0), Direction.Right, false) },
                4, 0, 2, 2),
            new[] { new TutorialStep("Careful.", TutorialAdvance.LevelWon) });

        private FakeBoardView _view;
        private FakeSaveService _save;
        private EventBus _bus;
        private HintStock _stock;
        private GameFlow _flow;

        private readonly List<LevelStarted> _started = new List<LevelStarted>();
        private readonly List<LevelWon> _won = new List<LevelWon>();
        private readonly List<LevelLost> _lost = new List<LevelLost>();
        private readonly List<LessonStarted> _lessonStarted = new List<LessonStarted>();
        private readonly List<LessonWon> _lessonWon = new List<LessonWon>();
        private readonly List<LessonLost> _lessonLost = new List<LessonLost>();
        private readonly List<TutorialEnded> _ended = new List<TutorialEnded>();
        private readonly List<HintRefused> _refused = new List<HintRefused>();
        private readonly List<HintUsed> _hintUsed = new List<HintUsed>();
        private int _menuOpened;

        [SetUp]
        public void SetUp()
        {
            _started.Clear();
            _won.Clear();
            _lost.Clear();
            _lessonStarted.Clear();
            _lessonWon.Clear();
            _lessonLost.Clear();
            _ended.Clear();
            _refused.Clear();
            _hintUsed.Clear();
            _menuOpened = 0;

            _view = new FakeBoardView();
            _save = new FakeSaveService(3);
            _bus = new EventBus();
            _bus.Subscribe<LevelStarted>(e => _started.Add(e));
            _bus.Subscribe<LevelWon>(e => _won.Add(e));
            _bus.Subscribe<LevelLost>(e => _lost.Add(e));
            _bus.Subscribe<LessonStarted>(e => _lessonStarted.Add(e));
            _bus.Subscribe<LessonWon>(e => _lessonWon.Add(e));
            _bus.Subscribe<LessonLost>(e => _lessonLost.Add(e));
            _bus.Subscribe<TutorialEnded>(e => _ended.Add(e));
            _bus.Subscribe<HintRefused>(e => _refused.Add(e));
            _bus.Subscribe<HintUsed>(e => _hintUsed.Add(e));
            _bus.Subscribe<MenuOpened>(e => _menuOpened++);

            _stock = new HintStock(_save, 5, _bus);
        }

        private GameFlow Build(params TutorialLesson[] lessons)
        {
            _flow = new GameFlow(_view, new[] { Easy }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f), _stock, lessons);
            return _flow;
        }

        private static Vector2 Center(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);

        private void Tap(int x, int y)
        {
            _flow.HandleTap(Center(x, y));
            _view.FinishAnimation();
            _flow.Tick(0f);
        }

        private void StartTutorial(bool thenStartGame = false)
        {
            _flow.RequestTutorial(thenStartGame);
            _flow.Tick(0f);
        }

        // Plays a one-bat lesson to its win and lets the flow settle in Won.
        private void WinFly()
        {
            Tap(1, 0);
        }

        [Test]
        public void ARequestFromTheMenu_StartsTheFirstLesson_WithLessonEvents_AndNoLevelEvents()
        {
            Build(Fly(), Blocked());

            StartTutorial();

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.IsTrue(_flow.IsTutorial);
            Assert.AreEqual(TutorialPhase.InStep, _flow.Tutorial.Phase);
            Assert.AreEqual(1, _lessonStarted.Count);
            Assert.AreEqual(0, _lessonStarted[0].LessonIndex);
            Assert.AreEqual(2, _lessonStarted[0].LessonCount);
            Assert.IsEmpty(_started, "lessons never publish LevelStarted");
            Assert.AreEqual(1, _flow.Session.Board.Bats.Count, "the board is the lesson's, not the catalog's");
        }

        [Test]
        public void WithoutLessons_TheTutorialDoesNothing()
        {
            _flow = new GameFlow(_view, new[] { Easy }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f), _stock);

            _flow.RequestTutorial();
            _flow.Tick(0f);

            Assert.IsFalse(_flow.HasTutorial);
            Assert.AreEqual(GameState.Menu, _flow.State);
            Assert.IsEmpty(_lessonStarted);
        }

        [Test]
        public void AnInvalidLesson_StopsTheGameAtStartup_WithItsNumber()
        {
            var broken = new TutorialLesson("Broken", Fly().Level, new[] { new TutorialStep("Tap.", TutorialAdvance.BatTapped) });

            var thrown = Assert.Throws<System.ArgumentException>(
                () => new GameFlow(_view, new[] { Easy }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f), _stock, new[] { Fly(), broken }));

            StringAssert.Contains("Lesson 2", thrown.Message);
        }

        [Test]
        public void ALimitedStep_RefusesATapOnAnotherBat_VisiblyAndWithoutAMove()
        {
            Build(Blocked());
            StartTutorial();

            _flow.HandleTap(Center(1, 0));
            _view.FinishAnimation();

            Assert.AreEqual(1, _view.RejectedCount, "the bats shake");
            Assert.IsEmpty(_view.Played);
            Assert.AreEqual(0, _flow.Session.MovesUsed);
            Assert.AreEqual(0, _flow.Tutorial.StepIndex);
        }

        [Test]
        public void TheLessonSteps_FollowTheMoves()
        {
            Build(Blocked());
            StartTutorial();

            Tap(0, 0);
            Assert.AreEqual(1, _flow.Tutorial.StepIndex, "the bounce moved on");

            Tap(0, 0);
            Assert.AreEqual(1, _flow.Tutorial.StepIndex, "bat 0 is not allowed now: the step wants bat 1");

            Tap(1, 0);
            Assert.AreEqual(2, _flow.Tutorial.StepIndex, "bat 1 flew off");
        }

        [Test]
        public void WinningALesson_PublishesLessonWon_NotLevelWon_AndWaitsForContinue()
        {
            Build(Fly(), Blocked());
            StartTutorial();

            WinFly();

            Assert.AreEqual(GameState.Won, _flow.State);
            Assert.AreEqual(1, _lessonWon.Count);
            Assert.AreEqual(0, _lessonWon[0].LessonIndex);
            Assert.IsEmpty(_won, "no stars, no progress, no streak");
            Assert.AreEqual(TutorialPhase.LessonComplete, _flow.Tutorial.Phase);
        }

        [Test]
        public void ALessonWin_IsNotSaved_AndNothingIsRewarded()
        {
            var save = new SaveService(new InMemorySaveStore(), 3);
            using (var recorder = new ProgressRecorder(_bus, save, new NullAudioService(), new[] { 1 }))
            {
                Build(Fly());
                StartTutorial();

                WinFly();

                Assert.AreEqual(0, save.Progress.CompletedCount);
            }
        }

        [Test]
        public void Continue_AfterAWonLesson_OpensTheNextLesson()
        {
            Build(Fly(), Blocked());
            StartTutorial();
            WinFly();

            _flow.TutorialContinue();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(1, _flow.LevelIndex);
            Assert.AreEqual(2, _lessonStarted.Count);
            Assert.AreEqual(1, _lessonStarted[1].LessonIndex);
            Assert.AreEqual(2, _flow.Session.Board.Bats.Count);
            Assert.AreEqual(0, _flow.Tutorial.StepIndex);
        }

        [Test]
        public void Continue_OnTheLastLesson_WithTheGameToFollow_StartsLevelOne()
        {
            Build(Fly());
            StartTutorial(thenStartGame: true);
            WinFly();

            _flow.TutorialContinue();
            _flow.Tick(0f);

            Assert.IsFalse(_flow.IsTutorial);
            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(0, _flow.LevelIndex);
            Assert.AreEqual(1, _started.Count, "the real first level starts");
            Assert.AreEqual(1, _ended.Count);
            Assert.AreEqual(TutorialEnd.Completed, _ended[0].Reason);
            Assert.IsTrue(_ended[0].Seen);
        }

        [Test]
        public void Continue_OnTheLastLesson_OfAReplay_ReturnsToTheMenu()
        {
            Build(Fly());
            _menuOpened = 0;
            StartTutorial(thenStartGame: false);
            WinFly();

            _flow.TutorialContinue();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Menu, _flow.State);
            Assert.AreEqual(1, _menuOpened, "the menu opens once, after the tutorial (the opening one was cleared above)");
            Assert.AreEqual(1, _ended.Count, "finishing is announced once; the menu does not add a second ending");
            Assert.AreEqual(TutorialEnd.Completed, _ended[0].Reason);
        }

        [Test]
        public void Skip_OnTheFirstRun_StartsLevelOneAtOnce()
        {
            Build(Fly(), Blocked());
            StartTutorial(thenStartGame: true);

            _flow.TutorialSkip();

            Assert.IsFalse(_flow.IsTutorial);
            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(1, _started.Count);
            Assert.AreEqual(0, _started[0].LevelIndex);
            Assert.AreEqual(TutorialEnd.Skipped, _ended[0].Reason);
            Assert.IsTrue(_ended[0].Seen);
            Assert.AreEqual(1, _flow.Session.Board.Bats.Count);
        }

        [Test]
        public void Skip_OfAReplay_ReturnsToTheMenu()
        {
            Build(Fly(), Blocked());
            StartTutorial(thenStartGame: false);

            _flow.TutorialSkip();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Menu, _flow.State);
            Assert.AreEqual(TutorialEnd.Skipped, _ended[0].Reason);
        }

        [Test]
        public void Skip_AfterWinningALesson_AlsoLeaves()
        {
            Build(Fly(), Blocked());
            StartTutorial(thenStartGame: true);
            WinFly();

            _flow.TutorialSkip();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.IsFalse(_flow.IsTutorial);
            Assert.AreEqual(1, _started.Count);
        }

        [Test]
        public void GoingToTheMenuInTheMiddle_IsNotSeen()
        {
            Build(Fly(), Blocked());
            StartTutorial();

            _flow.RequestMenu();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Menu, _flow.State);
            Assert.IsFalse(_flow.IsTutorial);
            Assert.AreEqual(1, _ended.Count);
            Assert.AreEqual(TutorialEnd.Left, _ended[0].Reason);
            Assert.IsFalse(_ended[0].Seen);
        }

        [Test]
        public void ARetry_StartsTheSameLessonAgain_FromItsFirstStep()
        {
            Build(Blocked());
            StartTutorial();
            Tap(0, 0);
            Assert.AreEqual(1, _flow.Tutorial.StepIndex);

            _flow.RequestRetry();

            Assert.AreEqual(0, _flow.Tutorial.StepIndex);
            Assert.AreEqual(2, _lessonStarted.Count);
            Assert.AreEqual(0, _lessonStarted[1].LessonIndex);
            Assert.AreEqual(0, _flow.Session.MovesUsed);
        }

        [Test]
        public void ALostLesson_PublishesLessonLost_AndARetryReopensIt()
        {
            Build(Tight());
            StartTutorial();

            for (int i = 0; i < 4; i++) Tap(0, 0);

            Assert.AreEqual(GameState.Lost, _flow.State);
            Assert.AreEqual(1, _lessonLost.Count);
            Assert.IsEmpty(_lost, "no level popup for a lesson");

            _flow.RequestRetry();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(2, _lessonStarted.Count);
            Assert.AreEqual(0, _flow.Session.MovesUsed);
        }

        [Test]
        public void ThePingStep_AdvancesWhenThePlayerPings()
        {
            Build(Echo());
            StartTutorial();

            _flow.RequestPing();

            Assert.AreEqual(1, _flow.Tutorial.StepIndex);
        }

        [Test]
        public void ALessonWithoutAHint_RefusesTheHintButton_VisiblyAndSpendsNothing()
        {
            Build(Fly());
            StartTutorial();

            _flow.RequestHint();

            Assert.AreEqual(HintStatus.NotInLesson, _flow.CheckHint());
            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(HintRefusal.NotInLesson, _refused[0].Reason);
            Assert.AreEqual(3, _save.Hints);
            Assert.AreEqual(1, _view.RejectedCount);
            Assert.IsEmpty(_hintUsed);
        }

        [Test]
        public void TheDemoHint_PointsAtABat_AdvancesTheLesson_AndTakesNothingFromTheStock()
        {
            Build(Hint());
            StartTutorial();

            _flow.RequestHint();

            Assert.AreEqual(3, _save.Hints, "a lesson's hint is free");
            CollectionAssert.AreEqual(new[] { 0 }, _view.HintedBatIds);
            Assert.AreEqual(1, _hintUsed.Count);
            Assert.AreEqual(1, _flow.Tutorial.StepIndex);
        }

        [Test]
        public void TheDemoHint_WorksWithAnEmptyStock_AndNeverAsksForAnAd()
        {
            _save.SetHints(0);
            Build(Hint());
            StartTutorial();

            Assert.AreEqual(HintStatus.Ready, _flow.CheckHint(), "NoHintsLeft is the only status that offers an ad");
            _flow.RequestHint();

            Assert.AreEqual(1, _hintUsed.Count);
            Assert.AreEqual(0, _save.Hints);
        }

        [Test]
        public void RequestNext_IsIgnoredInALesson()
        {
            Build(Fly(), Blocked());
            StartTutorial();
            WinFly();

            _flow.RequestNext();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Won, _flow.State, "only the tutorial's Continue moves on");
        }

        [Test]
        public void TheTutorial_CanBeStartedFromALevel_FromAWonScreen_AndFromALostScreen()
        {
            Build(Fly());

            _flow.RequestPlay(0);
            _flow.Tick(0f);
            Assert.IsFalse(_flow.IsTutorial);
            _flow.RequestTutorial();
            Assert.IsTrue(_flow.IsTutorial, "from a level in play: at once");
            Assert.AreEqual(GameState.Playing, _flow.State);

            _flow.RequestMenu();
            _flow.Tick(0f);
            _flow.RequestPlay(0);
            _flow.Tick(0f);
            Tap(1, 0);
            Assert.AreEqual(GameState.Won, _flow.State);
            Assert.AreEqual(1, _won.Count);

            _flow.RequestTutorial();
            _flow.Tick(0f);
            Assert.IsTrue(_flow.IsTutorial, "from the won screen");
            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(0, _flow.Tutorial.LessonIndex);
        }

        [Test]
        public void StartingTheTutorialAgain_RestartsItFromTheFirstLesson()
        {
            Build(Fly(), Blocked());
            StartTutorial();
            WinFly();
            _flow.TutorialContinue();
            _flow.Tick(0f);
            Assert.AreEqual(1, _flow.LevelIndex);

            _flow.RequestTutorial();

            Assert.AreEqual(0, _flow.LevelIndex);
            Assert.AreEqual(0, _flow.Tutorial.LessonIndex);
            Assert.AreEqual(0, _flow.Tutorial.StepIndex);
        }

        [Test]
        public void ReadingAStep_NeedsContinue_AndTheFlowDoesNotMoveForIt()
        {
            var reading = new TutorialLesson(
                "Read",
                Fly().Level,
                new[]
                {
                    new TutorialStep("Read this.", TutorialAdvance.Continue),
                    new TutorialStep("Tap.", TutorialAdvance.LevelWon)
                });
            Build(reading);
            StartTutorial();

            _flow.HandleTap(Center(1, 0));
            Assert.IsEmpty(_view.Played, "taps are blocked while reading");

            _flow.TutorialContinue();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(1, _flow.Tutorial.StepIndex);
            Assert.AreEqual(1, _lessonStarted.Count, "no new lesson was started");
        }

        [Test]
        public void TheOverlay_IsToldEveryChange_ThroughTheBus()
        {
            var changes = new List<TutorialChanged>();
            _bus.Subscribe<TutorialChanged>(e => changes.Add(e));
            Build(Blocked());

            StartTutorial();
            Tap(0, 0);

            Assert.IsTrue(changes.Count >= 2);
            TutorialChanged last = changes[changes.Count - 1];
            Assert.AreEqual(TutorialPhase.InStep, last.Phase);
            Assert.AreEqual("Now the other one.", last.Step.Text);
            Assert.AreEqual(1, last.LessonCount);
        }
    }
}
