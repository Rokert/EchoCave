using System;
using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class AudioServiceTests
    {
        private sealed class RecordingOutput : ISoundOutput
        {
            public readonly List<SoundCue> Played = new List<SoundCue>();

            public void Play(SoundCue cue) => Played.Add(cue);
        }

        private sealed class RecordingMusic : IMusicOutput
        {
            public readonly List<string> Calls = new List<string>();

            public void Play(MusicTrack track) => Calls.Add("play " + track);

            public void Stop() => Calls.Add("stop");
        }

        private RecordingOutput _output;
        private AudioService _audio;

        [SetUp]
        public void SetUp()
        {
            _output = new RecordingOutput();
            _audio = new AudioService(_output);
        }

        [Test]
        public void ACue_ReachesTheOutput_WhileSoundIsOn()
        {
            _audio.Play(SoundCue.Ping);
            _audio.Play(SoundCue.Fly);

            CollectionAssert.AreEqual(new[] { SoundCue.Ping, SoundCue.Fly }, _output.Played);
        }

        [Test]
        public void WithSoundOff_NothingReachesTheOutput_AndItComesBackWhenSwitchedOn()
        {
            _audio.SetEnabled(AudioChannel.Sound, false);
            _audio.Play(SoundCue.Ping);
            Assert.IsEmpty(_output.Played);

            _audio.SetEnabled(AudioChannel.Sound, true);
            _audio.Play(SoundCue.Ping);
            CollectionAssert.AreEqual(new[] { SoundCue.Ping }, _output.Played);
        }

        [Test]
        public void TheMusicSwitch_DoesNotMuteTheEffects()
        {
            _audio.SetEnabled(AudioChannel.Music, false);

            _audio.Play(SoundCue.Win);

            CollectionAssert.AreEqual(new[] { SoundCue.Win }, _output.Played);
            Assert.IsFalse(_audio.IsEnabled(AudioChannel.Music));
            Assert.IsTrue(_audio.IsEnabled(AudioChannel.Sound));
        }

        [Test]
        public void BothSwitches_StartOn()
        {
            Assert.IsTrue(_audio.IsEnabled(AudioChannel.Sound));
            Assert.IsTrue(_audio.IsEnabled(AudioChannel.Music));
        }

        [Test]
        public void Music_PlaysThroughTheMusicOutput_WhileTheMusicSwitchIsOn()
        {
            var music = new RecordingMusic();
            var audio = new AudioService(_output, music);

            audio.PlayMusic(MusicTrack.Menu);
            audio.PlayMusic(MusicTrack.Cave);

            CollectionAssert.AreEqual(new[] { "play Menu", "play Cave" }, music.Calls);
        }

        [Test]
        public void WithMusicOff_TheMusicStops_TheNewTrackIsOnlyRemembered_AndItComesBackWhenSwitchedOn()
        {
            var music = new RecordingMusic();
            var audio = new AudioService(_output, music);
            audio.PlayMusic(MusicTrack.Cave);

            audio.SetEnabled(AudioChannel.Music, false);
            audio.PlayMusic(MusicTrack.Bright);
            audio.SetEnabled(AudioChannel.Music, true);

            CollectionAssert.AreEqual(new[] { "play Cave", "stop", "play Bright" }, music.Calls);
        }

        [Test]
        public void SwitchingMusicOn_WithNothingChosenYet_PlaysNothing()
        {
            var music = new RecordingMusic();
            var audio = new AudioService(_output, music);

            audio.SetEnabled(AudioChannel.Music, false);
            audio.SetEnabled(AudioChannel.Music, true);

            CollectionAssert.AreEqual(new[] { "stop" }, music.Calls);
        }

        [Test]
        public void TheSameMusicSwitchValueTwice_DoesNothingTheSecondTime()
        {
            var music = new RecordingMusic();
            var audio = new AudioService(_output, music);
            audio.PlayMusic(MusicTrack.Cave);

            audio.SetEnabled(AudioChannel.Music, true);
            audio.SetEnabled(AudioChannel.Music, false);
            audio.SetEnabled(AudioChannel.Music, false);

            CollectionAssert.AreEqual(new[] { "play Cave", "stop" }, music.Calls);
        }

        [Test]
        public void TheSoundSwitch_DoesNotTouchTheMusic()
        {
            var music = new RecordingMusic();
            var audio = new AudioService(_output, music);
            audio.PlayMusic(MusicTrack.Cave);

            audio.SetEnabled(AudioChannel.Sound, false);

            CollectionAssert.AreEqual(new[] { "play Cave" }, music.Calls);
        }

        [Test]
        public void WithNoMusicOutput_MusicCallsAreHarmless()
        {
            Assert.DoesNotThrow(() =>
            {
                _audio.PlayMusic(MusicTrack.Cave);
                _audio.SetEnabled(AudioChannel.Music, false);
                _audio.SetEnabled(AudioChannel.Music, true);
            });
        }

        [Test]
        public void ANullOutput_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new AudioService(null));
        }

        [Test]
        public void TheNullAudio_PlaysNothing_AndNeverThrows()
        {
            var silent = new NullAudioService();

            Assert.DoesNotThrow(() => silent.Play(SoundCue.Ping));
            Assert.DoesNotThrow(() => silent.PlayMusic(MusicTrack.Menu));
        }
    }
}
