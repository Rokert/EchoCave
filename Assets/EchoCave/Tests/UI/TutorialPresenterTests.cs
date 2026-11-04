using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class TutorialPresenterTests
    {
        private sealed class FakeTutorialView : ITutorialView
        {
            public event Action ContinuePressed;
            public event Action RetryPressed;
            public event Action SkipPressed;

            public bool Visible = true;
            public string Text = string.Empty;
            public bool ContinueVisible;
            public string ContinueLabel = string.Empty;
            public bool RetryVisible;
            public TutorialFocus Focus = TutorialFocus.None;
            public int FocusBat = -1;

            public void SetVisible(bool visible) => Visible = visible;
            public void SetText(string text) => Text = text;

            public void SetContinue(bool visible, string label)
            {
                ContinueVisible = visible;
                ContinueLabel = label;
            }

            public void SetRetry(bool visible) => RetryVisible = visible;

            public void SetFocus(TutorialFocus focus, int batId)
            {
                Focus = focus;
                FocusBat = batId;
            }

            public void PressContinue() => ContinuePressed?.Invoke();
            public void PressRetry() => RetryPressed?.Invoke();
            public void PressSkip() => SkipPressed?.Invoke();
        }

        private sealed class FakeCommands : ITutorialCommands
        {
            public readonly List<string> Calls = new List<string>();

            public void Continue() => Calls.Add("continue");
            public void Retry() => Calls.Add("retry");
            public void Skip() => Calls.Add("skip");
        }

        private FakeTutorialView _view;
        private FakeCommands _commands;
        private EventBus _bus;
        private TutorialPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeTutorialView();
            _commands = new FakeCommands();
            _bus = new EventBus();
            _presenter = new TutorialPresenter(_view, _bus, _commands);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        private static TutorialChanged InStep(TutorialStep step, int lesson = 0, int count = 3) =>
            new TutorialChanged(TutorialPhase.InStep, lesson, count, step);

        [Test]
        public void TheOverlay_StartsHidden()
        {
            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(TutorialFocus.None, _view.Focus);
        }

        [Test]
        public void AStepOnABat_ShowsItsText_AndPutsTheRingOnThatBat()
        {
            _bus.Publish(InStep(new TutorialStep("Tap the bat.", TutorialAdvance.Exited, TutorialFocus.Bat, 2, limitTaps: true)));

            Assert.IsTrue(_view.Visible);
            Assert.AreEqual("Tap the bat.", _view.Text);
            Assert.AreEqual(TutorialFocus.Bat, _view.Focus);
            Assert.AreEqual(2, _view.FocusBat);
            Assert.IsFalse(_view.ContinueVisible, "the step waits for a move, not for a button");
            Assert.IsFalse(_view.RetryVisible);
        }

        [TestCase(TutorialFocus.PingButton)]
        [TestCase(TutorialFocus.HintButton)]
        public void AStepOnAButton_PutsTheRingOnTheButton(TutorialFocus focus)
        {
            _bus.Publish(InStep(new TutorialStep("Press it.", TutorialAdvance.Pinged, focus)));

            Assert.AreEqual(focus, _view.Focus);
        }

        [Test]
        public void AReadingStep_OffersGotIt()
        {
            _bus.Publish(InStep(new TutorialStep("Read this.", TutorialAdvance.Continue)));

            Assert.IsTrue(_view.ContinueVisible);
            Assert.AreEqual(TutorialPresenter.ReadLabel, _view.ContinueLabel);
        }

        [Test]
        public void AWonLesson_SaysSo_AndOffersNext_OrDoneOnTheLastOne()
        {
            _bus.Publish(new TutorialChanged(TutorialPhase.LessonComplete, 0, 3, null));

            Assert.AreEqual(TutorialPresenter.LessonDoneText, _view.Text);
            Assert.IsTrue(_view.ContinueVisible);
            Assert.AreEqual(TutorialPresenter.NextLabel, _view.ContinueLabel);
            Assert.AreEqual(TutorialFocus.None, _view.Focus);

            _bus.Publish(new TutorialChanged(TutorialPhase.LessonComplete, 2, 3, null));

            Assert.AreEqual(TutorialPresenter.DoneLabel, _view.ContinueLabel);
        }

        [Test]
        public void ALostLesson_OffersTryAgain_NotContinue()
        {
            _bus.Publish(InStep(new TutorialStep("Careful.", TutorialAdvance.LevelWon)));

            _bus.Publish(new LessonLost(0, 5));

            Assert.AreEqual(TutorialPresenter.LessonLostText, _view.Text);
            Assert.IsTrue(_view.RetryVisible);
            Assert.IsFalse(_view.ContinueVisible);
            Assert.AreEqual(TutorialFocus.None, _view.Focus);
        }

        [Test]
        public void TheRetry_ThatRestartsTheLesson_BringsTheStepBack_AndHidesTryAgain()
        {
            var step = new TutorialStep("Tap the bat.", TutorialAdvance.Exited, TutorialFocus.Bat, 0, limitTaps: true);
            _bus.Publish(InStep(step));
            _bus.Publish(new LessonLost(0, 5));

            _bus.Publish(InStep(step));

            Assert.AreEqual("Tap the bat.", _view.Text);
            Assert.IsFalse(_view.RetryVisible);
            Assert.AreEqual(TutorialFocus.Bat, _view.Focus);
        }

        [Test]
        public void TheEnd_AndTheMenu_HideTheOverlay()
        {
            _bus.Publish(InStep(new TutorialStep("Tap.", TutorialAdvance.Exited, TutorialFocus.Bat, 0)));

            _bus.Publish(new TutorialEnded(TutorialEnd.Skipped));
            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(TutorialFocus.None, _view.Focus);

            _bus.Publish(InStep(new TutorialStep("Tap.", TutorialAdvance.Exited, TutorialFocus.Bat, 0)));
            _bus.Publish(new MenuOpened());
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void AFinishedPhase_HidesTheOverlay()
        {
            _bus.Publish(InStep(new TutorialStep("Tap.", TutorialAdvance.Exited)));

            _bus.Publish(new TutorialChanged(TutorialPhase.Finished, 2, 3, null));

            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void TheButtons_AreForwardedToTheGame()
        {
            _view.PressContinue();
            _view.PressRetry();
            _view.PressSkip();

            CollectionAssert.AreEqual(new[] { "continue", "retry", "skip" }, _commands.Calls);
        }

        [Test]
        public void AfterDispose_NothingIsListenedTo()
        {
            _presenter.Dispose();

            _bus.Publish(InStep(new TutorialStep("Tap.", TutorialAdvance.Exited)));
            _view.PressSkip();

            Assert.IsFalse(_view.Visible);
            Assert.IsEmpty(_commands.Calls);
        }

        [Test]
        public void MissingParts_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new TutorialPresenter(null, _bus, _commands));
            Assert.Throws<ArgumentNullException>(() => new TutorialPresenter(_view, null, _commands));
            Assert.Throws<ArgumentNullException>(() => new TutorialPresenter(_view, _bus, null));
        }
    }
}
