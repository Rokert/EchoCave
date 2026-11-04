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
        private const int LevelCount = 10;
        private const int StartingHints = 3;

        private int _pingCalls;
        private int _hintCalls;
        private int _pauseCalls;

        [SetUp]
        public void SetUp()
        {
            _pingCalls = 0;
            _hintCalls = 0;
            _pauseCalls = 0;
            _view = new FakeHudView();
            _bus = new EventBus();
            _presenter = new HudPresenter(_view, _bus, () => _pingCalls++, () => _hintCalls++, () => _pauseCalls++, LevelCount, StartingHints, false);
        }

        private void StartLevel(int moveLimit, int pingBudget)
        {
            _bus.Publish(new LevelStarted(0, moveLimit, pingBudget));
        }

        [TestCase(0, 1)]
        [TestCase(4, 5)]
        [TestCase(9, 10)]
        public void LevelStarted_ShowsTheLevelNumberFromOne_OutOfTheTotal(int levelIndex, int expectedNumber)
        {
            _bus.Publish(new LevelStarted(levelIndex, 9, 0));

            Assert.AreEqual(expectedNumber, _view.LevelNumber);
            Assert.AreEqual(LevelCount, _view.LevelCount);
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

            Assert.Throws<ArgumentNullException>(() => new HudPresenter(null, _bus, noop, noop, noop, LevelCount, StartingHints, false));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(_view, null, noop, noop, noop, LevelCount, StartingHints, false));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(_view, _bus, null, noop, noop, LevelCount, StartingHints, false));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(_view, _bus, noop, null, noop, LevelCount, StartingHints, false));
            Assert.Throws<ArgumentNullException>(() => new HudPresenter(_view, _bus, noop, noop, null, LevelCount, StartingHints, false));
        }

        [Test]
        public void TheHintCount_IsShownFromTheStart_BeforeAnyLevel()
        {
            Assert.AreEqual(3, _view.Hints);
            Assert.AreEqual(HintButtonState.Ready, _view.HintState);
        }

        [Test]
        public void WhenTheHintsChange_TheCountAndTheStateFollow()
        {
            _bus.Publish(new HintsChanged(1));
            Assert.AreEqual(1, _view.Hints);
            Assert.AreEqual(HintButtonState.Ready, _view.HintState);

            _bus.Publish(new HintsChanged(0));
            Assert.AreEqual(0, _view.Hints);
            Assert.AreEqual(HintButtonState.Empty, _view.HintState);

            _bus.Publish(new HintsChanged(2));
            Assert.AreEqual(HintButtonState.Ready, _view.HintState, "a refill must bring the button back to life");
        }

        [Test]
        public void StartingWithNoHints_ShowsTheButtonEmpty()
        {
            var view = new FakeHudView();
            using (new HudPresenter(view, new EventBus(), () => { }, () => { }, () => { }, LevelCount, 0, false))
            {
                Assert.AreEqual(0, view.Hints);
                Assert.AreEqual(HintButtonState.Empty, view.HintState);
            }
        }

        [Test]
        public void ARefusedHint_ShakesTheHintButton_NotThePing()
        {
            _bus.Publish(new HintRefused(HintRefusal.NoHintsLeft));

            Assert.AreEqual(1, _view.HintShakeCount);
            Assert.AreEqual(0, _view.ShakeCount);
        }

        [Test]
        public void ARefusedPing_DoesNotShakeTheHintButton()
        {
            _bus.Publish(new PingRefused(PingResult.NoPingsLeft));

            Assert.AreEqual(0, _view.HintShakeCount);
        }

        [Test]
        public void PressingTheHintButton_AsksTheGame_AndNothingElse()
        {
            _view.PressHint();

            Assert.AreEqual(1, _hintCalls);
            Assert.AreEqual(0, _pingCalls);
            Assert.AreEqual(0, _pauseCalls);
        }

        [Test]
        public void AfterDispose_TheHintButtonAndTheHintEventsAreIgnored()
        {
            _presenter.Dispose();

            _view.PressHint();
            _bus.Publish(new HintsChanged(0));
            _bus.Publish(new HintRefused(HintRefusal.Busy));

            Assert.AreEqual(0, _hintCalls);
            Assert.AreEqual(3, _view.Hints);
            Assert.AreEqual(0, _view.HintShakeCount);
        }

        [Test]
        public void WithNoHintsAndAnAdToWatch_TheButtonOffersTheAd_InsteadOfLookingDead()
        {
            var view = new FakeHudView();
            using (new HudPresenter(view, new EventBus(), () => { }, () => { }, () => { }, LevelCount, 0, true))
            {
                Assert.AreEqual(HintButtonState.WatchAd, view.HintState);
            }
        }

        [Test]
        public void WithHintsLeft_TheButtonIsReady_EvenWhenAnAdCouldRefillIt()
        {
            var view = new FakeHudView();
            using (new HudPresenter(view, new EventBus(), () => { }, () => { }, () => { }, LevelCount, 2, true))
            {
                Assert.AreEqual(HintButtonState.Ready, view.HintState);
            }
        }

        [Test]
        public void SpendingTheLastHint_SwitchesToTheAdOffer_AndAGrantSwitchesBack()
        {
            var view = new FakeHudView();
            var bus = new EventBus();
            using (new HudPresenter(view, bus, () => { }, () => { }, () => { }, LevelCount, 1, true))
            {
                bus.Publish(new HintsChanged(0));
                Assert.AreEqual(HintButtonState.WatchAd, view.HintState);

                bus.Publish(new HintsChanged(1));
                Assert.AreEqual(HintButtonState.Ready, view.HintState);
            }
        }
    }
}
