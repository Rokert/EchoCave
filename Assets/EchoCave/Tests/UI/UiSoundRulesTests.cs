using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class UiSoundRulesTests
    {
        [Test]
        public void APillButton_Clicks()
        {
            Assert.AreEqual(SoundCue.Click, UiSoundRules.CueForButton(new[] { "pill-button", "pill-button--secondary" }));
            Assert.AreEqual(SoundCue.Click, UiSoundRules.CueForButton(new[] { "round-button" }));
        }

        [Test]
        public void ASwitch_UsesTheToggleSound_WhetherOnOrOff()
        {
            Assert.AreEqual(SoundCue.Toggle, UiSoundRules.CueForButton(new[] { "toggle", "toggle--on" }));
            Assert.AreEqual(SoundCue.Toggle, UiSoundRules.CueForButton(new[] { "toggle" }));
        }

        [Test]
        public void ThePingButton_MakesNoClick_BecauseTheGameEventsPlayTheirOwnSound()
        {
            Assert.IsNull(UiSoundRules.CueForButton(new[] { "ping-button", "ping-button--ready" }));
            Assert.IsNull(UiSoundRules.CueForButton(new[] { "ping-button--empty", "ping-button" }));
        }

        [Test]
        public void TheHintButton_MakesNoClick_ItsEventsPlayTheirOwnSound()
        {
            Assert.IsNull(UiSoundRules.CueForButton(new[] { "hint-button", "hint-button--ready" }));
            Assert.IsNull(UiSoundRules.CueForButton(new[] { "hint-button--empty", "hint-button" }));
        }

        [Test]
        public void AButtonWithNoClasses_StillClicks()
        {
            Assert.AreEqual(SoundCue.Click, UiSoundRules.CueForButton(new string[0]));
        }

        [Test]
        public void EachStar_HasItsOwnHigherSound()
        {
            Assert.AreEqual(SoundCue.Star1, UiSoundRules.CueForStar(0));
            Assert.AreEqual(SoundCue.Star2, UiSoundRules.CueForStar(1));
            Assert.AreEqual(SoundCue.Star3, UiSoundRules.CueForStar(2));
        }
    }
}
