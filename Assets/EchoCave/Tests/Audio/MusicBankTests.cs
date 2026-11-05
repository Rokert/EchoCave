using System.Collections.Generic;
using System.Linq;
using EchoCave.Game;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class MusicBankTests
    {
        private MusicBank _bank;
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            _bank = ScriptableObject.CreateInstance<MusicBank>();
            _clip = AudioClip.Create("m", 100, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bank);
            Object.DestroyImmediate(_clip);
        }

        private static MusicBank.Entry Entry(MusicTrack track, AudioClip clip, float volume = 0.6f)
        {
            return new MusicBank.Entry { track = track, clip = clip, volume = volume };
        }

        private IEnumerable<MusicBank.Entry> AllTracks(AudioClip clip)
        {
            return System.Enum.GetValues(typeof(MusicTrack)).Cast<MusicTrack>().Select(t => Entry(t, clip));
        }

        [Test]
        public void ATrackWithAClip_IsFound_WithItsVolume()
        {
            _bank.SetEntries(new[] { Entry(MusicTrack.Cave, _clip, 0.4f) });

            Assert.IsTrue(_bank.TryGet(MusicTrack.Cave, out MusicBank.Music music));
            Assert.AreSame(_clip, music.Clip);
            Assert.AreEqual(0.4f, music.Volume, 1e-6f);
            Assert.IsFalse(_bank.TryGet(MusicTrack.Dream, out _));
        }

        [Test]
        public void ANewBank_HasTheDefaultFlow()
        {
            Assert.AreEqual(MusicTrack.Menu, _bank.MenuTrack);
            CollectionAssert.AreEqual(new[] { MusicTrack.Cave, MusicTrack.Bright, MusicTrack.Dream }, _bank.PlayTracks);
            Assert.AreEqual(180f, _bank.SecondsPerTrack);
            Assert.Greater(_bank.CrossfadeSeconds, 0f);
        }

        [Test]
        public void ACompleteBank_HasNoProblems()
        {
            _bank.SetEntries(AllTracks(_clip));

            Assert.IsEmpty(_bank.FindProblems());
        }

        [Test]
        public void MissingEntries_MissingClips_SilentVolumes_AndDuplicates_AreReported()
        {
            _bank.SetEntries(new[]
            {
                Entry(MusicTrack.Menu, null),
                Entry(MusicTrack.Cave, _clip, 0f),
                Entry(MusicTrack.Bright, _clip),
                Entry(MusicTrack.Bright, _clip)
            });

            string all = string.Join("\n", _bank.FindProblems());

            StringAssert.Contains("Menu has no clip", all);
            StringAssert.Contains("Cave has volume 0", all);
            StringAssert.Contains("Bright is listed more than once", all);
            StringAssert.Contains("Dream has no entry", all);
        }

        [Test]
        public void AFlowThatNamesATrackWithNoClip_IsReported()
        {
            _bank.SetEntries(new[] { Entry(MusicTrack.Menu, _clip) });

            string all = string.Join("\n", _bank.FindProblems());

            StringAssert.Contains("playlist names Cave", all);
        }
    }
}
