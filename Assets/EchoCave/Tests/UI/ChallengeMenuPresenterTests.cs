using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class ChallengeMenuPresenterTests
    {
        private FakeMenuView _view;
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeMenuView();
            _bus = new EventBus();
        }

        [Test]
        public void TheChallengesButton_ShowsOnlyWhenThereIsAListToOpen()
        {
            using (new MenuPresenter(_view, _bus, () => { }, true, challenges: () => { })) Assert.IsTrue(_view.ChallengesVisible);
            using (new MenuPresenter(_view, _bus, () => { }, true)) Assert.IsFalse(_view.ChallengesVisible);
        }

        [Test]
        public void Pressing_HidesTheMenu_AndOpensTheList()
        {
            int opened = 0;
            using (new MenuPresenter(_view, _bus, () => { }, true, challenges: () => opened++))
            {
                _view.PressChallenges();

                Assert.IsFalse(_view.Visible);
                Assert.AreEqual(1, opened);
            }
        }

        [Test]
        public void Show_BringsTheMenuBack_ForTheListToCallWhenItCloses()
        {
            using (var presenter = new MenuPresenter(_view, _bus, () => { }, true, challenges: () => { }))
            {
                _view.PressChallenges();

                presenter.Show();

                Assert.IsTrue(_view.Visible);
            }
        }

        [Test]
        public void AfterDispose_TheButtonDoesNothing()
        {
            int opened = 0;
            var presenter = new MenuPresenter(_view, _bus, () => { }, true, challenges: () => opened++);
            presenter.Dispose();

            _view.PressChallenges();

            Assert.AreEqual(0, opened);
        }

        [Test]
        public void TheNewTag_ShowsWhileAChallengeIsUnseen_AndGoesWhenTheMenuIsShownAgain()
        {
            bool isNew = true;
            using (var presenter = new MenuPresenter(_view, _bus, () => { }, true, challenges: () => { }, challengesAreNew: () => isNew))
            {
                Assert.IsTrue(_view.ChallengesNew);

                isNew = false;
                presenter.Show();

                Assert.IsFalse(_view.ChallengesNew);
            }
        }

        [Test]
        public void TheNewTag_IsRefreshedWhenTheFlowReturnsToTheMenu()
        {
            bool isNew = false;
            using (new MenuPresenter(_view, _bus, () => { }, false, challenges: () => { }, challengesAreNew: () => isNew))
            {
                isNew = true;

                _bus.Publish(new MenuOpened());

                Assert.IsTrue(_view.ChallengesNew);
            }
        }

        [Test]
        public void WithoutAChallengesAction_ThereIsNoNewTag()
        {
            using (new MenuPresenter(_view, _bus, () => { }, true, challengesAreNew: () => true))
            {
                Assert.IsFalse(_view.ChallengesNew);
            }
        }
    }
}
