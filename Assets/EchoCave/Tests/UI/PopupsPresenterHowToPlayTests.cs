using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class PopupsPresenterHowToPlayTests
    {
        private FakePopupsView _view;
        private FakePopupCommands _commands;
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _view = new FakePopupsView();
            _commands = new FakePopupCommands();
            _bus = new EventBus();
        }

        [Test]
        public void TheButton_ShowsOnlyWhenTheGameHasATutorial()
        {
            _commands.HasTutorial = true;
            using (new PopupsPresenter(_view, _bus, _commands)) Assert.IsTrue(_view.HowToPlayVisible);

            _commands.HasTutorial = false;
            using (new PopupsPresenter(_view, _bus, _commands)) Assert.IsFalse(_view.HowToPlayVisible);
        }

        [Test]
        public void Pressing_ClosesThePopup_AndStartsTheTutorial()
        {
            using (new PopupsPresenter(_view, _bus, _commands))
            {
                _bus.Publish(new PauseChanged(true));
                Assert.AreEqual(ShownPopup.Pause, _view.Shown);

                _view.Press(PopupAction.HowToPlay);

                Assert.AreEqual(ShownPopup.None, _view.Shown);
                Assert.AreEqual(1, _commands.HowToPlays);
                Assert.AreEqual(0, _commands.Resumes);
                Assert.AreEqual(0, _commands.Retries);
            }
        }

        [Test]
        public void ALessonStarting_ClosesAnOpenPopup()
        {
            using (new PopupsPresenter(_view, _bus, _commands))
            {
                _bus.Publish(new PauseChanged(true));

                _bus.Publish(new LessonStarted(0, 5, 0, 0));

                Assert.AreEqual(ShownPopup.None, _view.Shown);
            }
        }

        [Test]
        public void APausedLesson_ShowsThePausePopup_LikeALevel()
        {
            using (new PopupsPresenter(_view, _bus, _commands))
            {
                _bus.Publish(new LessonStarted(0, 5, 0, 0));

                _bus.Publish(new PauseChanged(true));

                Assert.AreEqual(ShownPopup.Pause, _view.Shown);
            }
        }

        [Test]
        public void AfterDispose_LessonsAreNotListenedTo()
        {
            var presenter = new PopupsPresenter(_view, _bus, _commands);
            _bus.Publish(new PauseChanged(true));
            presenter.Dispose();

            _bus.Publish(new LessonStarted(0, 5, 0, 0));

            Assert.AreEqual(ShownPopup.Pause, _view.Shown);
        }
    }
}
