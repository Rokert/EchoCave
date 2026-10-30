using System;
using EchoCave.Core;
using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class HudPresenterTests
    {
        private FakeHudView _view;
        private EventBus _bus;
        private HudPresenter _presenter;
        private int _pingCalls;
        private int _pauseCalls;

        [SetUp]
        public void SetUp()
        {
            _pingCalls = 0;
            _pauseCalls = 0;
            _view = new FakeHudView();
            _bus = new EventBus();
            _presenter = new HudPresenter(_view, _bus, () => _pingCalls++, () => _pauseCalls++);
        }

        private void StartLevel(int moveLimit, int pingBudget)
        {
            _bus.Publish(new LevelStarted(0, moveLimit, pingBudget));
        }

        [Test]
        public void TheHud_StartsHidden_UntilALevelStarts()
        {
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void LevelStarted_ShowsTheHud_WithNoMovesAndAFullBudget()
        {
            StartLevel(9, 3);

            Assert.IsTrue(_view.Visible);
            Assert.AreEqual(0, _view.MovesUsed);
            Assert.AreEqual(9, _view.MoveLimit);
            Assert.AreEqual(3, _view.Pings);
            Assert.AreEqual(PingButtonState.Ready, _view.PingState);
            Assert.IsFalse(_view.Paused);
        }

        [Test]
        public void LevelStarted_WithNoPings_ShowsTheButtonEmpty()
        {
            StartLevel(0, 0);

            Assert.AreEqual(PingButtonState.Empty, _view.PingState);
            Assert.AreEqual(0, _view.Pings);
        }

        [Test]
        public void MoveSpent_UpdatesTheCounter()
        {
            StartLevel(9, 0);

            _bus.Publish(new MoveSpent(4, 9, MoveOutcome.Bounced));

            Assert.AreEqual(4, _view.MovesUsed);
            Assert.AreEqual(9, _view.MoveLimit);
        }

        [Test]
        public void PingUsed_ShowsTheButtonActive_WithTheNewCount()
        {
            StartLevel(0, 2);

            _bus.Publish(new PingUsed(1, 3f));

            Assert.AreEqual(1, _view.Pings);
            Assert.AreEqual(PingButtonState.Active, _view.PingState);
        }

        [Test]
        public void WhenTheEchoExpires_TheButtonIsReadyAgain_IfPingsRemain()
        {
            StartLevel(0, 2);
            _bus.Publish(new PingUsed(1, 3f));

            _bus.Publish(new EchoExpired());

            Assert.AreEqual(PingButtonState.Ready, _view.PingState);
        }

        [Test]
        public void WhenTheEchoExpires_TheButtonIsEmpty_IfTheLastPingWasUsed()
        {
            StartLevel(0, 1);
            _bus.Publish(new PingUsed(0, 3f));
            Assert.AreEqual(PingButtonState.Active, _view.PingState, "still revealing, so not empty yet");

            _bus.Publish(new EchoExpired());

            Assert.AreEqual(PingButtonState.Empty, _view.PingState);
        }

        [Test]
        public void AnewLevel_ResetsARevealThatWasRunning()
        {
            StartLevel(0, 2);
            _bus.Publish(new PingUsed(1, 3f));

            StartLevel(0, 2);

            Assert.AreEqual(PingButtonState.Ready, _view.PingState);
            Assert.AreEqual(2, _view.Pings);
        }

        [TestCase(PingResult.NoPingsLeft)]
        [TestCase(PingResult.AlreadyActive)]
        [TestCase(PingResult.NothingToReveal)]
        public void ARefusedPing_ShakesTheButton(PingResult reason)
        {
            StartLevel(0, 1);

            _bus.Publish(new PingRefused(reason));

            Assert.AreEqual(1, _view.ShakeCount);
        }

        [Test]
        public void PauseChanged_UpdatesTheButton()
        {
            StartLevel(0, 0);

            _bus.Publish(new PauseChanged(true));
            Assert.IsTrue(_view.Paused);

            _bus.Publish(new PauseChanged(false));
            Assert.IsFalse(_view.Paused);
        }

        [Test]
        public void MenuOpened_HidesTheHud()
        {
            StartLevel(0, 0);
            Assert.IsTrue(_view.Visible);

            _bus.Publish(new MenuOpened());

            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void TheButtons_AreForwardedToTheGame()
        {
            _view.PressPing();
            _view.PressPause();
            _view.PressPause();

            Assert.AreEqual(1, _pingCalls);
            Assert.AreEqual(2, _pauseCalls);
        }

        [Test]
        public void AfterDispose_NothingIsHeardAnymore()
        {
            StartLevel(9, 2);
            _presenter.Dispose();

            _bus.Publish(new MoveSpent(5, 9, MoveOutcome.Exited));
            _bus.Publish(new PingUsed(1, 3f));
            _bus.Publish(new PingRefused(PingResult.NoPingsLeft));
            _bus.Publish(new PauseChanged(true));
            _bus.Publish(new MenuOpened());
            _view.PressPing();
            _view.PressPause();

            Assert.AreEqual(0, _view.MovesUsed, "the counter must not follow events after Dispose");
            Assert.AreEqual(2, _view.Pings);
            Assert.AreEqual(0, _view.ShakeCount);
            Assert.IsFalse(_view.Paused);
            Assert.IsTrue(_view.Visible);
            Assert.AreEqual(0, _pingCalls);
            Assert.AreEqual(0, _pauseCalls);
        }

        [Test]
        public void Constructor_RejectsNulls()
        {
            Action noop = () => { };

            Assert.Throws<ArgumentNullException>(() => new HudPresenter(null, _bus, noop, noop));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(_view, null, noop, noop));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(_view, _bus, null, noop));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(_view, _bus, noop, null));
        }
    }
}
