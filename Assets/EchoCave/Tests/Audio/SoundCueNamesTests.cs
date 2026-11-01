using System;
using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class SoundCueNamesTests
    {
        [Test]
        public void EveryCue_HasAFileName_AndNoTwoShareOne()
        {
            var seen = new HashSet<string>();
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)))
            {
                string name = SoundCueNames.FileName(cue);

                Assert.IsFalse(string.IsNullOrWhiteSpace(name), cue.ToString());
                Assert.IsTrue(seen.Add(name), $"{cue} reuses the file name '{name}'");
            }
        }

        [Test]
        public void TheNames_AreLowercaseWithUnderscores_LikeTheFilesTheToolSaves()
        {
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)))
                StringAssert.IsMatch("^[a-z0-9_]+$", SoundCueNames.FileName(cue), cue.ToString());
        }

        [Test]
        public void ACueOutsideTheEnum_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SoundCueNames.FileName((SoundCue)999));
        }
    }
}
