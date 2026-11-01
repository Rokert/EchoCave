using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class SoundThrottleTests
    {
        [Test]
        public void TheFirstPlay_IsAlwaysAllowed()
        {
            Assert.IsTrue(new SoundThrottle().TryAllow(SoundCue.Reveal, 5f, 0.06f));
        }

        [Test]
        public void ASecondPlayInsideTheInterval_IsBlocked_AndOutsideItAllowed()
        {
            var throttle = new SoundThrottle();
            throttle.TryAllow(SoundCue.Reveal, 1.0f, 0.25f);

            // 1.25 and 0.25 are exact in binary, so the boundary itself is tested without rounding noise.
            Assert.IsFalse(throttle.TryAllow(SoundCue.Reveal, 1.125f, 0.25f));
            Assert.IsTrue(throttle.TryAllow(SoundCue.Reveal, 1.25f, 0.25f));
        }

        [Test]
        public void ABlockedPlay_DoesNotPushTheWindowForward()
        {
            var throttle = new SoundThrottle();
            throttle.TryAllow(SoundCue.Reveal, 1.00f, 0.06f);
            throttle.TryAllow(SoundCue.Reveal, 1.04f, 0.06f);

            Assert.IsTrue(throttle.TryAllow(SoundCue.Reveal, 1.07f, 0.06f), "the window runs from the last sound that played");
        }

        [Test]
        public void AnIntervalOfZero_NeverBlocks()
        {
            var throttle = new SoundThrottle();

            Assert.IsTrue(throttle.TryAllow(SoundCue.Fly, 1f, 0f));
            Assert.IsTrue(throttle.TryAllow(SoundCue.Fly, 1f, 0f));
        }

        [Test]
        public void EachCue_IsLimitedOnItsOwn()
        {
            var throttle = new SoundThrottle();
            throttle.TryAllow(SoundCue.Reveal, 1f, 0.5f);

            Assert.IsTrue(throttle.TryAllow(SoundCue.Ping, 1.01f, 0.5f));
        }

        [Test]
        public void AClockThatGoesBackwards_DoesNotSilenceTheCue()
        {
            var throttle = new SoundThrottle();
            throttle.TryAllow(SoundCue.Reveal, 100f, 0.5f);

            Assert.IsTrue(throttle.TryAllow(SoundCue.Reveal, 2f, 0.5f));
        }
    }
}
