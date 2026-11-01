using System;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class VoicePickerTests
    {
        [Test]
        public void TakesTheVoiceAtTheCursor_WhenItIsIdle()
        {
            Assert.AreEqual(2, VoicePicker.Pick(new[] { false, false, false, false }, 2));
        }

        [Test]
        public void SkipsBusyVoices_AndWrapsRound()
        {
            Assert.AreEqual(0, VoicePicker.Pick(new[] { false, true, true, true }, 1));
            Assert.AreEqual(3, VoicePicker.Pick(new[] { true, true, true, false }, 1));
        }

        [Test]
        public void WhenEveryVoiceIsBusy_ItCutsTheOneAtTheCursor()
        {
            Assert.AreEqual(1, VoicePicker.Pick(new[] { true, true, true }, 1));
        }

        [Test]
        public void ABadCursorOrNoVoices_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VoicePicker.Pick(new[] { false, false }, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => VoicePicker.Pick(new[] { false, false }, -1));
            Assert.Throws<ArgumentException>(() => VoicePicker.Pick(new bool[0], 0));
            Assert.Throws<ArgumentNullException>(() => VoicePicker.Pick(null, 0));
        }
    }
}
