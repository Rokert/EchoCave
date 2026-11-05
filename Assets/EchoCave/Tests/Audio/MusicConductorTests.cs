using System;
using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class MusicConductorTests
    {
        private sealed class RecordingAudio : IAudioService
        {
            public readonly List<MusicTrack> Music = new List<MusicTrack>();

            public bool IsEnabled(AudioChannel channel) => true;

            public void SetEnabled(AudioChannel channel, bool enabled)
            {
            }

            public void Play(SoundCue cue)
            {
            }

            public void PlayMusic(MusicTrack track) => Music.Add(track);
        }

        private static readonly MusicTrack[] Playlist = { MusicTrack.Cave, MusicTrack.Bright, MusicTrack.Dream };

        private EventBus _bus;
        private RecordingAudio _audio;
        private MusicConductor _conductor;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _audio = new RecordingAudio();
            _conductor = new MusicConductor(_bus, _audio, MusicTrack.Menu, Playlist, 180f);
        }

        [TearDown]
        public void TearDown()
        {
            _conductor.Dispose();
        }

        private void StartLevel() => _bus.Publish(new LevelStarted(0, 10, 2));

        [Test]
        public void TheMenuTrack_PlaysFromTheStart()
        {
            CollectionAssert.AreEqual(new[] { MusicTrack.Menu }, _audio.Music);
            Assert.AreEqual(MusicArea.Menu, _conductor.Area);
        }

        [Test]
        public void TheFirstLevel_SwitchesToTheFirstPlayTrack_AndLaterLevelsDoNotRestartIt()
        {
            StartLevel();
            StartLevel();
            StartLevel();

            CollectionAssert.AreEqual(new[] { MusicTrack.Menu, MusicTrack.Cave }, _audio.Music);
            Assert.AreEqual(MusicArea.Play, _conductor.Area);
        }

        [Test]
        public void ALesson_SwitchesToPlayMusicToo()
        {
            _bus.Publish(new LessonStarted(0, 5, 0, 0));

            CollectionAssert.AreEqual(new[] { MusicTrack.Menu, MusicTrack.Cave }, _audio.Music);
        }

        [Test]
        public void AfterTheTrackTime_TheNextTrackTakesOver_AndTheLastGoesBackToTheFirst()
        {
            StartLevel();

            _conductor.Tick(179f);
            Assert.AreEqual(MusicTrack.Cave, _audio.Music[_audio.Music.Count - 1]);

            _conductor.Tick(1f);
            _conductor.Tick(180f);
            _conductor.Tick(180f);

            CollectionAssert.AreEqual(
                new[] { MusicTrack.Menu, MusicTrack.Cave, MusicTrack.Bright, MusicTrack.Dream, MusicTrack.Cave },
                _audio.Music);
        }

        [Test]
        public void ALongFrame_MovesOneTrackOnly_NotSeveral()
        {
            StartLevel();

            _conductor.Tick(1000f);

            CollectionAssert.AreEqual(new[] { MusicTrack.Menu, MusicTrack.Cave, MusicTrack.Bright }, _audio.Music);
        }

        [Test]
        public void TimeInTheMenu_DoesNotCountTowardsTheNextTrack()
        {
            _conductor.Tick(1000f);
            StartLevel();
            _conductor.Tick(100f);

            CollectionAssert.AreEqual(new[] { MusicTrack.Menu, MusicTrack.Cave }, _audio.Music);
        }

        [Test]
        public void GoingBackToTheMenu_PlaysTheMenuTrack_AndTheNextPlayStartsTheListAgain()
        {
            StartLevel();
            _conductor.Tick(180f);
            _bus.Publish(new MenuOpened());
            StartLevel();

            CollectionAssert.AreEqual(
                new[] { MusicTrack.Menu, MusicTrack.Cave, MusicTrack.Bright, MusicTrack.Menu, MusicTrack.Cave },
                _audio.Music);
        }

        [Test]
        public void OpeningTheMenuTwice_PlaysTheMenuTrackOnlyOnce()
        {
            _bus.Publish(new MenuOpened());
            _bus.Publish(new MenuOpened());

            CollectionAssert.AreEqual(new[] { MusicTrack.Menu }, _audio.Music);
        }

        [Test]
        public void AOneTrackPlaylist_OrNoTrackTime_NeverChangesTrack()
        {
            _conductor.Dispose();
            _audio.Music.Clear();
            _conductor = new MusicConductor(_bus, _audio, MusicTrack.Menu, new[] { MusicTrack.Cave }, 180f);
            _bus.Publish(new LevelStarted(0, 10, 2));
            _conductor.Tick(1000f);
            Assert.AreEqual(MusicTrack.Cave, _audio.Music[_audio.Music.Count - 1]);
            Assert.AreEqual(2, _audio.Music.Count);

            _conductor.Dispose();
            _audio.Music.Clear();
            _bus.Publish(new MenuOpened());
            _conductor = new MusicConductor(_bus, _audio, MusicTrack.Menu, Playlist, 0f);
            _bus.Publish(new LevelStarted(0, 10, 2));
            _conductor.Tick(1000f);
            Assert.AreEqual(2, _audio.Music.Count);
        }

        [Test]
        public void AnEmptyPlaylist_PlaysTheMenuTrackWhilePlaying()
        {
            _conductor.Dispose();
            _audio.Music.Clear();
            _conductor = new MusicConductor(_bus, _audio, MusicTrack.Menu, new MusicTrack[0], 180f);

            _bus.Publish(new LevelStarted(0, 10, 2));

            CollectionAssert.AreEqual(new[] { MusicTrack.Menu, MusicTrack.Menu }, _audio.Music);
        }

        [Test]
        public void AfterDispose_TheConductorIgnoresTheFlow()
        {
            _conductor.Dispose();
            _audio.Music.Clear();

            StartLevel();

            Assert.IsEmpty(_audio.Music);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new MusicConductor(null, _audio, MusicTrack.Menu, Playlist, 180f));
            Assert.Throws<ArgumentNullException>(() => new MusicConductor(_bus, null, MusicTrack.Menu, Playlist, 180f));
        }
    }
}
