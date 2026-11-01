using System;
using System.Collections.Generic;
using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class PopupsPresenterTests
    {
        private FakePopupsView _view;
        private FakePopupCommands _commands;
        private EventBus _bus;
        private PopupsPresenter _presenter;
        private readonly List<AudioToggled> _audio = new List<AudioToggled>();

        [SetUp]
        public void SetUp()
        {
            _audio.Clear();
            _view = new FakePopupsView();
            _commands = new FakePopupCommands();
            _bus = new EventBus();
            _bus.Subscribe<AudioToggled>(e => _audio.Add(e));
            _presenter = new PopupsPresenter(_view, _bus, _commands);
        }

        private void StartLevel(int moveLimit) => _bus.Publish(new LevelStarted(0, moveLimit, 0));

        [Test]
        public void ThePauseShowsTheSwitchesTheGameWasLeftWith()
        {
            _presenter.Dispose();
            _presenter = new PopupsPresenter(_view, _bus, _commands, soundOn: false, musicOn: true);
            StartLevel(9);

            _bus.Publish(new PauseChanged(true));

            Assert.AreEqual(ShownPopup.Pause, _view.Shown);
            Assert.IsFalse(_view.SoundOn, "the saved switch must show as off, not reset to on");
            Assert.IsTrue(_view.MusicOn);
        }

        [Test]
        public void AFlippedSwitch_StaysFlipped_FromTheSavedStart()
        {
            _presenter.Dispose();
            _presenter = new PopupsPresenter(_view, _bus, _commands, soundOn: false, musicOn: false);
            StartLevel(9);

            _view.Toggle(AudioChannel.Music, true);
            _bus.Publish(new PauseChanged(true));

            Assert.IsFalse(_view.SoundOn);
            Assert.IsTrue(_view.MusicOn);
        }

        [Test]
        public void NoPopupShows_UntilTheGameAsksForOne()
        {
            Assert.AreEqual(ShownPopup.None, _view.Shown);
            Assert.GreaterOrEqual(_view.HideCount, 1);
        }

        [Test]
        public void WinningShowsTheCompletePopup_WithTheStarsAndTheMoves()
        {
            StartLevel(9);

            _bus.Publish(new LevelWon(0, 7, 3));

            Assert.AreEqual(ShownPopup.Complete, _view.Shown);
            Assert.AreEqual(3, _view.Stars);
            Assert.AreEqual(7, _view.MovesUsed);
            Assert.AreEqual(9, _view.MoveLimit);
            Assert.IsTrue(_view.HasNext);
        }

        [Test]
        public void WinningALevelWithoutALimit_PassesZeroAsTheLimit()
        {
            StartLevel(0);

            _bus.Publish(new LevelWon(0, 4, 3));

            Assert.AreEqual(0, _view.MoveLimit);
        }

        [Test]
        public void OnTheLastLevel_TheCompletePopupHasNoNext()
        {
            StartLevel(9);
            _commands.HasNextLevel = false;

            _bus.Publish(new LevelWon(0, 7, 2));

            Assert.IsFalse(_view.HasNext);
        }

        [Test]
        public void LosingShowsTheLostPopup_WithTheMoves()
        {
            StartLevel(9);

            _bus.Publish(new LevelLost(0, 9));

            Assert.AreEqual(ShownPopup.Lost, _view.Shown);
            Assert.AreEqual(9, _view.MovesUsed);
            Assert.AreEqual(9, _view.MoveLimit);
        }

        [Test]
        public void PausingShowsThePausePopup_AndResumingHidesIt()
        {
            StartLevel(9);

            _bus.Publish(new PauseChanged(true));
            Assert.AreEqual(ShownPopup.Pause, _view.Shown);
            Assert.IsTrue(_view.SoundOn);
            Assert.IsTrue(_view.MusicOn);

            _bus.Publish(new PauseChanged(false));
            Assert.AreEqual(ShownPopup.None, _view.Shown);
        }

        [Test]
        public void ANewLevel_HidesWhateverWasShowing()
        {
            StartLevel(9);
            _bus.Publish(new LevelLost(0, 9));

            StartLevel(9);

            Assert.AreEqual(ShownPopup.None, _view.Shown);
        }

        [Test]
        public void TheMenu_HidesWhateverWasShowing()
        {
            StartLevel(9);
            _bus.Publish(new LevelWon(0, 5, 3));

            _bus.Publish(new MenuOpened());

            Assert.AreEqual(ShownPopup.None, _view.Shown);
        }

        [TestCase(PopupAction.Replay, 1, 0, 0, 0)]
        [TestCase(PopupAction.Retry, 1, 0, 0, 0)]
        [TestCase(PopupAction.Restart, 1, 0, 0, 0)]
        [TestCase(PopupAction.Next, 0, 1, 0, 0)]
        [TestCase(PopupAction.Menu, 0, 0, 1, 0)]
        [TestCase(PopupAction.Resume, 0, 0, 0, 1)]
        public void EachButton_HidesThePopup_AndCallsExactlyItsCommand(
            PopupAction action, int retries, int nexts, int menus, int resumes)
        {
            StartLevel(9);
            _bus.Publish(new LevelWon(0, 5, 3));

            _view.Press(action);

            Assert.AreEqual(ShownPopup.None, _view.Shown, "the popup must answer at once");
            Assert.AreEqual(retries, _commands.Retries);
            Assert.AreEqual(nexts, _commands.Nexts);
            Assert.AreEqual(menus, _commands.Menus);
            Assert.AreEqual(resumes, _commands.Resumes);
        }

        [Test]
        public void FlippingASwitch_PublishesAudioToggled()
        {
            _view.Toggle(AudioChannel.Sound, false);
            _view.Toggle(AudioChannel.Music, false);

            Assert.AreEqual(2, _audio.Count);
            Assert.AreEqual(AudioChannel.Sound, _audio[0].Channel);
            Assert.IsFalse(_audio[0].Enabled);
            Assert.AreEqual(AudioChannel.Music, _audio[1].Channel);
            Assert.IsFalse(_audio[1].Enabled);
        }

        [Test]
        public void TheSwitchesKeepTheirState_WhenThePausePopupOpensAgain()
        {
            _view.Toggle(AudioChannel.Sound, false);

            _bus.Publish(new PauseChanged(true));

            Assert.IsFalse(_view.SoundOn);
            Assert.IsTrue(_view.MusicOn, "only the sound switch was flipped");
        }

        [Test]
        public void TheSwitchesKeepTheirState_FromOneLevelToTheNext()
        {
            _view.Toggle(AudioChannel.Music, false);

            StartLevel(9);
            _bus.Publish(new PauseChanged(true));

            Assert.IsFalse(_view.MusicOn);
        }

        [Test]
        public void AfterDispose_NothingIsHeardAnymore()
        {
            StartLevel(9);
            _presenter.Dispose();

            _bus.Publish(new LevelWon(0, 5, 3));
            Assert.AreEqual(ShownPopup.None, _view.Shown, "the popup must not follow events after Dispose");

            _bus.Publish(new PauseChanged(true));
            Assert.AreEqual(ShownPopup.None, _view.Shown);

            _view.Press(PopupAction.Next);
            _view.Toggle(AudioChannel.Sound, false);

            Assert.AreEqual(0, _commands.Nexts);
            Assert.AreEqual(0, _audio.Count);
        }

        [Test]
        public void Constructor_RejectsNulls()
        {
            Assert.Throws<ArgumentNullException>(() => new PopupsPresenter(null, _bus, _commands));
            Assert.Throws<ArgumentNullException>(() => new PopupsPresenter(_view, null, _commands));
            Assert.Throws<ArgumentNullException>(() => new PopupsPresenter(_view, _bus, null));
        }
    }
}
