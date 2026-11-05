using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    // A challenge looks like a level to the HUD, and clears the menu and the popups like one.
    public class ChallengeUiTests
    {
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
        }

        [Test]
        public void TheHud_ShowsAChallenge_WithItsCaption_ItsPlaceInTheList_AndNoMoveLimit()
        {
            var view = new FakeHudView();
            using (new HudPresenter(view, _bus, () => { }, () => { }, () => { }, 10, 3, false))
            {
                _bus.Publish(new ChallengeStarted(0, 3, "challenge-001", 0, 1, 1));

                Assert.IsTrue(view.Visible);
                Assert.AreEqual("CHALLENGE", view.LevelCaption);
                Assert.AreEqual(1, view.LevelNumber);
                Assert.AreEqual(3, view.LevelCount, "the number of challenges, not the 10 levels");
                Assert.AreEqual(0, view.MovesUsed);
                Assert.AreEqual(0, view.MoveLimit);
                Assert.AreEqual(1, view.Pings);
            }
        }

        [Test]
        public void TheHintButton_IsHiddenInAChallenge_AndComesBackForALevelOrALesson()
        {
            var view = new FakeHudView();
            using (new HudPresenter(view, _bus, () => { }, () => { }, () => { }, 10, 3, false))
            {
                _bus.Publish(new LevelStarted(0, 8, 1));
                Assert.IsTrue(view.HintVisible, "a level has hints");

                _bus.Publish(new ChallengeStarted(0, 1, "challenge-001", 0, 1, 1));
                Assert.IsFalse(view.HintVisible, "a challenge has none");

                _bus.Publish(new LevelStarted(1, 8, 1));
                Assert.IsTrue(view.HintVisible);

                _bus.Publish(new ChallengeStarted(0, 1, "challenge-001", 0, 1, 2));
                _bus.Publish(new LessonStarted(0, 5, 0, 0));
                Assert.IsTrue(view.HintVisible, "a lesson shows the button");
            }
        }

        [Test]
        public void ALevelAfterAChallenge_GoesBackToTheLevelCaption()
        {
            var view = new FakeHudView();
            using (new HudPresenter(view, _bus, () => { }, () => { }, () => { }, 10, 3, false))
            {
                _bus.Publish(new ChallengeStarted(0, 1, "challenge-001", 0, 1, 1));

                _bus.Publish(new LevelStarted(4, 8, 1));

                Assert.AreEqual("LEVEL", view.LevelCaption);
                Assert.AreEqual(5, view.LevelNumber);
                Assert.AreEqual(10, view.LevelCount);
            }
        }

        [Test]
        public void TheHud_StopsListeningToChallenges_AfterDispose()
        {
            var view = new FakeHudView();
            var presenter = new HudPresenter(view, _bus, () => { }, () => { }, () => { }, 10, 3, false);
            presenter.Dispose();
            view.Visible = false;

            _bus.Publish(new ChallengeStarted(0, 1, "challenge-001", 0, 1, 1));

            Assert.IsFalse(view.Visible);
        }

        [Test]
        public void ThePopups_ClosesWhenAChallengeStarts()
        {
            var view = new FakePopupsView();
            using (new PopupsPresenter(view, _bus, new FakePopupCommands()))
            {
                _bus.Publish(new LevelWon(0, 5, 3));
                Assert.AreEqual(ShownPopup.Complete, view.Shown);

                _bus.Publish(new ChallengeStarted(0, 1, "challenge-001", 0, 1, 1));

                Assert.AreEqual(ShownPopup.None, view.Shown);
            }
        }

        [Test]
        public void TheMenu_HidesWhenAChallengeStarts()
        {
            var view = new FakeMenuView();
            using (new MenuPresenter(view, _bus, () => { }, startInMenu: true))
            {
                Assert.IsTrue(view.Visible);

                _bus.Publish(new ChallengeStarted(0, 1, "challenge-001", 0, 1, 1));

                Assert.IsFalse(view.Visible);
            }
        }
    }
}
