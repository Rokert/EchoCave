using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    // A tutorial lesson looks like a level to the HUD, with its own caption and total.
    public class HudPresenterLessonTests
    {
        private FakeHudView _view;
        private EventBus _bus;
        private HudPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeHudView();
            _bus = new EventBus();
            _presenter = new HudPresenter(_view, _bus, () => { }, () => { }, () => { }, 10, 3, false);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        [Test]
        public void ALessonStarting_ShowsTheHud_WithTheLessonCaptionAndItsOwnTotal()
        {
            _bus.Publish(new LessonStarted(1, 5, 4, 2));

            Assert.IsTrue(_view.Visible);
            Assert.AreEqual("LESSON", _view.LevelCaption);
            Assert.AreEqual(2, _view.LevelNumber);
            Assert.AreEqual(5, _view.LevelCount, "the lessons' total, not the 10 levels");
            Assert.AreEqual(0, _view.MovesUsed);
            Assert.AreEqual(4, _view.MoveLimit);
            Assert.AreEqual(2, _view.Pings);
        }

        [Test]
        public void ALevelStartingAfterTheLessons_GoesBackToTheLevelCaption()
        {
            _bus.Publish(new LessonStarted(0, 5, 0, 0));

            _bus.Publish(new LevelStarted(2, 8, 1));

            Assert.AreEqual("LEVEL", _view.LevelCaption);
            Assert.AreEqual(3, _view.LevelNumber);
            Assert.AreEqual(10, _view.LevelCount);
        }

        [Test]
        public void AfterDispose_LessonsAreNotListenedTo()
        {
            _presenter.Dispose();

            _bus.Publish(new LessonStarted(0, 5, 0, 0));

            Assert.AreEqual(string.Empty, _view.LevelCaption);
        }
    }
}
