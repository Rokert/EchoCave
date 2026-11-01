using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class SoundCuePlayerTests
    {
        private sealed class RecordingAudio : IAudioService
        {
            public readonly List<SoundCue> Played = new List<SoundCue>();

            public bool IsEnabled(AudioChannel channel) => true;

            public void SetEnabled(AudioChannel channel, bool enabled)
            {
            }

            public void Play(SoundCue cue) => Played.Add(cue);
        }

        private EventBus _bus;
        private RecordingAudio _audio;
        private SoundCuePlayer _player;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _audio = new RecordingAudio();
            _player = new SoundCuePlayer(_bus, _audio);
        }

        [TearDown]
        public void TearDown()
        {
            _player.Dispose();
        }

        [Test]
        public void EachEvent_PlaysItsCue()
        {
            _bus.Publish(new PingUsed(1, 3f));
            _bus.Publish(new EchoExpired());
            _bus.Publish(new PingRefused(PingResult.NoPingsLeft));
            _bus.Publish(new LevelWon(0, 5, 3));
            _bus.Publish(new LevelLost(0, 9));

            CollectionAssert.AreEqual(
                new[] { SoundCue.Ping, SoundCue.Expire, SoundCue.Refuse, SoundCue.Win, SoundCue.Lose },
                _audio.Played);
        }

        [Test]
        public void AMove_PlaysTheWhooshWhenItExits_AndTheThudWhenItBounces()
        {
            _bus.Publish(new MoveSpent(1, 9, MoveOutcome.Exited));
            _bus.Publish(new MoveSpent(2, 9, MoveOutcome.Bounced));

            CollectionAssert.AreEqual(new[] { SoundCue.Fly, SoundCue.Bounce }, _audio.Played);
        }

        [Test]
        public void EventsThatHaveNoSound_PlayNothing()
        {
            _bus.Publish(new LevelStarted(0, 9, 2));
            _bus.Publish(new PauseChanged(true));
            _bus.Publish(new MenuOpened());

            Assert.IsEmpty(_audio.Played);
        }

        [Test]
        public void AfterDispose_NothingIsListenedTo()
        {
            _player.Dispose();

            _bus.Publish(new PingUsed(1, 3f));
            _bus.Publish(new MoveSpent(1, 9, MoveOutcome.Exited));
            _bus.Publish(new LevelWon(0, 5, 3));

            Assert.IsEmpty(_audio.Played);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new SoundCuePlayer(null, _audio));
            Assert.Throws<ArgumentNullException>(() => new SoundCuePlayer(_bus, null));
        }
    }
}
