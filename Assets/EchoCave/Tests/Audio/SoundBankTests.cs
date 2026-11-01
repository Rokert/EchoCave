using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class SoundBankTests
    {
        private SoundBank _bank;
        private AudioClip _clipA;
        private AudioClip _clipB;

        [SetUp]
        public void SetUp()
        {
            _bank = ScriptableObject.CreateInstance<SoundBank>();
            _clipA = AudioClip.Create("a", 100, 1, 44100, false);
            _clipB = AudioClip.Create("b", 100, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bank);
            Object.DestroyImmediate(_clipA);
            Object.DestroyImmediate(_clipB);
        }

        private static SoundBank.Entry Entry(SoundCue cue, AudioClip clip, float volume = 1f, float interval = 0f)
        {
            return new SoundBank.Entry { cue = cue, clip = clip, volume = volume, minIntervalSeconds = interval };
        }

        [Test]
        public void ACueWithAClip_IsFound_WithItsVolumeAndInterval()
        {
            _bank.SetEntries(new[] { Entry(SoundCue.Reveal, _clipA, 0.6f, 0.06f) });

            Assert.IsTrue(_bank.TryGet(SoundCue.Reveal, out SoundBank.Sound sound));
            Assert.AreSame(_clipA, sound.Clip);
            Assert.AreEqual(0.6f, sound.Volume, 1e-6f);
            Assert.AreEqual(0.06f, sound.MinIntervalSeconds, 1e-6f);
        }

        [Test]
        public void ACueWithNoEntry_OrNoClip_IsNotFound()
        {
            _bank.SetEntries(new[] { Entry(SoundCue.Ping, null) });

            Assert.IsFalse(_bank.TryGet(SoundCue.Ping, out _));
            Assert.IsFalse(_bank.TryGet(SoundCue.Fly, out _));
        }

        [Test]
        public void WhenACueIsListedTwice_TheFirstOneWins()
        {
            _bank.SetEntries(new[] { Entry(SoundCue.Ping, _clipA), Entry(SoundCue.Ping, _clipB) });

            _bank.TryGet(SoundCue.Ping, out SoundBank.Sound sound);

            Assert.AreSame(_clipA, sound.Clip);
        }

        [Test]
        public void ReplacingTheEntries_ReplacesWhatIsFound()
        {
            _bank.SetEntries(new[] { Entry(SoundCue.Ping, _clipA) });
            _bank.TryGet(SoundCue.Ping, out _);

            _bank.SetEntries(new[] { Entry(SoundCue.Ping, _clipB) });
            _bank.TryGet(SoundCue.Ping, out SoundBank.Sound sound);

            Assert.AreSame(_clipB, sound.Clip, "the lookup must not keep the old entries");
        }

        [Test]
        public void TheProblems_NameEveryMissingDuplicatedAndSilentCue()
        {
            _bank.SetEntries(new[]
            {
                Entry(SoundCue.Ping, _clipA),
                Entry(SoundCue.Ping, _clipB),
                Entry(SoundCue.Fly, null),
                Entry(SoundCue.Win, _clipA, 0f)
            });

            string all = string.Join("\n", _bank.FindProblems());

            StringAssert.Contains("Ping is listed more than once", all);
            StringAssert.Contains("Fly has no clip", all);
            StringAssert.Contains("Win has volume 0", all);
            StringAssert.Contains("Reveal has no entry", all);
        }

        [Test]
        public void ACompleteBank_HasNoProblems()
        {
            var entries = new List<SoundBank.Entry>();
            foreach (SoundCue cue in System.Enum.GetValues(typeof(SoundCue))) entries.Add(Entry(cue, _clipA));
            _bank.SetEntries(entries);

            Assert.IsEmpty(_bank.FindProblems());
        }
    }
}
