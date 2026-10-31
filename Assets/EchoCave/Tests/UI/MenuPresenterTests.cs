using System;
using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class MenuPresenterTests
    {
        private FakeMenuView _view;
        private EventBus _bus;
        private int _plays;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeMenuView();
            _bus = new EventBus();
            _plays = 0;
        }

        private MenuPresenter Create(bool startInMenu) => new MenuPresenter(_view, _bus, () => _plays++, startInMenu);

        [Test]
        public void ShowsTheMenu_WhenTheUiIsBuiltWhileTheGameIsInTheMenu()
        {
            Create(true);

            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void KeepsTheMenuHidden_WhenTheUiIsBuiltAfterTheGameLeftTheMenu()
        {
            _view.Visible = true;

            Create(false);

            Assert.IsFalse(_view.Visible, "a UI built mid-level must not cover the board");
        }

        [Test]
        public void PressingPlay_AsksTheGameToPlay_AndHidesTheMenuAtOnce()
        {
            Create(true);

            _view.PressPlay();

            Assert.AreEqual(1, _plays);
            Assert.IsFalse(_view.Visible, "the button answers before the level has loaded");
        }

        [Test]
        public void StartingALevel_HidesTheMenu()
        {
            Create(true);

            _bus.Publish(new LevelStarted(0, 9, 0));

            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void GoingBackToTheMenu_ShowsItAgain()
        {
            Create(true);
            _bus.Publish(new LevelStarted(0, 9, 0));

            _bus.Publish(new MenuOpened());

            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void AfterDispose_NothingReachesTheViewOrTheGame()
        {
            MenuPresenter presenter = Create(true);
            presenter.Dispose();
            int shows = _view.ShowCount;
            int hides = _view.HideCount;

            _bus.Publish(new MenuOpened());
            _bus.Publish(new LevelStarted(0, 9, 0));
            _view.PressPlay();

            Assert.AreEqual(shows, _view.ShowCount);
            Assert.AreEqual(hides, _view.HideCount);
            Assert.AreEqual(0, _plays);
            Assert.IsFalse(_view.HasPlaySubscriber);
        }

        [Test]
        public void ConstructorRejectsNulls()
        {
            Assert.Throws<ArgumentNullException>(() => new MenuPresenter(null, _bus, () => { }, true));
            Assert.Throws<ArgumentNullException>(() => new MenuPresenter(_view, null, () => { }, true));
            Assert.Throws<ArgumentNullException>(() => new MenuPresenter(_view, _bus, null, true));
        }
    }
}
