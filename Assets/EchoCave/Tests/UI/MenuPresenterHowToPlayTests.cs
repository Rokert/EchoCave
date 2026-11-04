using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class MenuPresenterHowToPlayTests
    {
        private FakeMenuView _view;
        private EventBus _bus;
        private int _plays;
        private int _howToPlays;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeMenuView();
            _bus = new EventBus();
            _plays = 0;
            _howToPlays = 0;
        }

        private MenuPresenter Create(bool withTutorial = true) =>
            new MenuPresenter(_view, _bus, () => _plays++, true, withTutorial ? () => _howToPlays++ : (System.Action)null);

        [Test]
        public void TheButton_ShowsOnlyWhenTheGameHasATutorial()
        {
            Create(withTutorial: true);
            Assert.IsTrue(_view.HowToPlayVisible);

            Create(withTutorial: false);
            Assert.IsFalse(_view.HowToPlayVisible);
        }

        [Test]
        public void Pressing_HidesTheMenuAtOnce_AndStartsTheTutorial_NotTheGame()
        {
            Create();

            _view.PressHowToPlay();

            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(1, _howToPlays);
            Assert.AreEqual(0, _plays);
        }

        [Test]
        public void PressingWithoutATutorial_DoesNothing()
        {
            Create(withTutorial: false);
            _view.Visible = true;

            _view.PressHowToPlay();

            Assert.IsTrue(_view.Visible);
            Assert.AreEqual(0, _howToPlays);
        }

        [Test]
        public void ALessonStarting_HidesTheMenu()
        {
            Create();
            _view.Visible = true;

            _bus.Publish(new LessonStarted(0, 5, 0, 0));

            Assert.IsFalse(_view.Visible, "a lesson is on screen, the menu must not cover it");
        }

        [Test]
        public void AfterDispose_TheButtonAndLessonsAreNotListenedTo()
        {
            MenuPresenter presenter = Create();
            presenter.Dispose();
            _view.Visible = true;

            _view.PressHowToPlay();
            _bus.Publish(new LessonStarted(0, 5, 0, 0));

            Assert.AreEqual(0, _howToPlays);
            Assert.IsTrue(_view.Visible);
        }
    }
}
