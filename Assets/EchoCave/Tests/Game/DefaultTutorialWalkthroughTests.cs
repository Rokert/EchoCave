using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    // Plays the shipped lessons through the real flow, the way the text asks, and checks that every lesson can be won
    // and that the steps follow the player.
    public class DefaultTutorialWalkthroughTests
    {
        private static readonly LevelDefinition Catalog = new LevelDefinition(
            3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 0, 1, 1);

        private FakeBoardView _view;
        private FakeSaveService _save;
        private EventBus _bus;
        private GameFlow _flow;
        private readonly List<TutorialEnded> _ended = new List<TutorialEnded>();
        private readonly List<LessonStarted> _lessonStarted = new List<LessonStarted>();
        private readonly List<HintRefused> _refused = new List<HintRefused>();

        [SetUp]
        public void SetUp()
        {
            _ended.Clear();
            _lessonStarted.Clear();
            _refused.Clear();
            _view = new FakeBoardView();
            _save = new FakeSaveService(3);
            _bus = new EventBus();
            _bus.Subscribe<TutorialEnded>(e => _ended.Add(e));
            _bus.Subscribe<LessonStarted>(e => _lessonStarted.Add(e));
            _bus.Subscribe<HintRefused>(e => _refused.Add(e));
            _flow = new GameFlow(
                _view, new[] { Catalog }, new MoveResolver(), _bus, new RevealAllHiddenStrategy(2f),
                new HintStock(_save, 5, _bus), DefaultTutorial.Lessons());
        }

        private static Vector2 Center(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);

        private void Tap(int x, int y)
        {
            _flow.HandleTap(Center(x, y));
            _view.FinishAnimation();
            _flow.Tick(0f);
        }

        private void Continue()
        {
            _flow.TutorialContinue();
            _flow.Tick(0f);
        }

        [Test]
        public void TheWholeTutorial_CanBePlayedToTheEnd_AndLeadsToLevelOne()
        {
            _flow.RequestTutorial(thenStartGame: true);
            _flow.Tick(0f);

            // Lesson 1: three free bats.
            Assert.AreEqual(0, _flow.Tutorial.LessonIndex);
            Tap(1, 1);
            Assert.AreEqual(1, _flow.Tutorial.StepIndex);
            Tap(0, 0);
            Tap(2, 0);
            Assert.AreEqual(GameState.Won, _flow.State);
            Continue();

            // Lesson 2: bounce, then free the path.
            Assert.AreEqual(1, _flow.Tutorial.LessonIndex);
            Tap(0, 1);
            Assert.AreEqual(1, _flow.Tutorial.StepIndex, "the bounce moved on");
            Tap(2, 1);
            Assert.AreEqual(2, _flow.Tutorial.StepIndex);
            Tap(0, 1);
            Assert.AreEqual(GameState.Won, _flow.State);
            Continue();

            // Lesson 3: three in a row, from the end.
            Assert.AreEqual(2, _flow.Tutorial.LessonIndex);
            Tap(2, 1);
            Assert.AreEqual(1, _flow.Tutorial.StepIndex);
            Tap(1, 1);
            Tap(0, 1);
            Assert.AreEqual(GameState.Won, _flow.State);
            Continue();

            // Lesson 4: the echo.
            Assert.AreEqual(3, _flow.Tutorial.LessonIndex);
            _flow.RequestPing();
            Assert.AreEqual(1, _flow.Tutorial.StepIndex);
            Tap(0, 1);
            Tap(2, 1);
            Tap(1, 0);
            Assert.AreEqual(GameState.Won, _flow.State);
            Continue();

            // Lesson 5: the free hint.
            Assert.AreEqual(4, _flow.Tutorial.LessonIndex);
            _flow.RequestHint();
            Assert.AreEqual(1, _flow.Tutorial.StepIndex);
            CollectionAssert.AreEqual(new[] { 2 }, _view.HintedBatIds, "the top bat is the only free one");
            Tap(1, 2);
            Tap(1, 1);
            Tap(1, 0);
            Assert.AreEqual(GameState.Won, _flow.State);
            Continue();

            Assert.IsFalse(_flow.IsTutorial);
            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(0, _flow.LevelIndex);
            Assert.AreEqual(1, _ended.Count);
            Assert.AreEqual(TutorialEnd.Completed, _ended[0].Reason);
            Assert.AreEqual(5, _lessonStarted.Count);
            Assert.AreEqual(3, _save.Hints, "the hint of lesson 5 was free");
            Assert.IsEmpty(_refused);
        }

        [Test]
        public void TheFirstLesson_RefusesTheOtherBats_UntilTheFocusedOneHasGone()
        {
            _flow.RequestTutorial();
            _flow.Tick(0f);

            _flow.HandleTap(Center(0, 0));

            Assert.IsEmpty(_view.Played);
            Assert.AreEqual(1, _view.RejectedCount);
        }

        [Test]
        public void TheEchoAndHintLessons_RefuseTheBats_UntilTheButtonIsPressed()
        {
            _flow.RequestTutorial();
            _flow.Tick(0f);
            Tap(1, 1);
            Tap(0, 0);
            Tap(2, 0);
            Continue();
            Tap(0, 1);
            Tap(2, 1);
            Tap(0, 1);
            Continue();
            Tap(2, 1);
            Tap(1, 1);
            Tap(0, 1);
            Continue();

            // Lesson 4 waits for the echo button.
            int rejected = _view.RejectedCount;
            _flow.HandleTap(Center(0, 1));
            Assert.AreEqual(rejected + 1, _view.RejectedCount, "the bats shake");
            Assert.AreEqual(0, _flow.Session.MovesUsed, "and nothing moved");

            _flow.RequestPing();
            Tap(0, 1);
            Tap(2, 1);
            Tap(1, 0);
            Continue();

            // Lesson 5 waits for the hint button.
            rejected = _view.RejectedCount;
            _flow.HandleTap(Center(1, 2));
            Assert.AreEqual(rejected + 1, _view.RejectedCount);
            Assert.AreEqual(0, _flow.Session.MovesUsed);

            _flow.RequestHint();
            Tap(1, 2);
            Assert.AreEqual(1, _flow.Session.MovesUsed, "after the hint the bat can be tapped");
        }

        [Test]
        public void TheEchoLesson_RefusesTheHintButton()
        {
            _flow.RequestTutorial();
            _flow.Tick(0f);
            Tap(1, 1);
            Tap(0, 0);
            Tap(2, 0);
            Continue();
            Tap(0, 1);
            Tap(2, 1);
            Tap(0, 1);
            Continue();
            Tap(2, 1);
            Tap(1, 1);
            Tap(0, 1);
            Continue();
            Assert.AreEqual(3, _flow.Tutorial.LessonIndex);

            _flow.RequestHint();

            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(HintRefusal.NotInLesson, _refused[0].Reason);
        }

        [Test]
        public void SkippingOnTheFirstRun_StartsLevelOne_AndCountsAsSeen()
        {
            _flow.RequestTutorial(thenStartGame: true);
            _flow.Tick(0f);

            _flow.TutorialSkip();

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.IsFalse(_flow.IsTutorial);
            Assert.IsTrue(_ended[0].Seen);
        }
    }
}
