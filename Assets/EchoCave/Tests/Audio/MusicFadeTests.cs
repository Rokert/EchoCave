using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class MusicFadeTests
    {
        [Test]
        public void AFade_RisesInAStraightLine_AndStopsAtTheTarget()
        {
            float v = 0f;
            for (int i = 0; i < 5; i++) v = MusicFade.Step(v, 0.6f, 0.5f, 2f);

            Assert.AreEqual(0.6f, v, 1e-6f);
            Assert.AreEqual(0.25f, MusicFade.Step(0f, 0.6f, 0.5f, 2f), 1e-6f);
        }

        [Test]
        public void AFade_FallsToZero_AndNeverGoesBelowIt()
        {
            Assert.AreEqual(0.35f, MusicFade.Step(0.6f, 0f, 0.5f, 2f), 1e-6f);
            Assert.AreEqual(0f, MusicFade.Step(0.1f, 0f, 5f, 2f));
        }

        [Test]
        public void AZeroLengthFade_JumpsStraightToTheTarget()
        {
            Assert.AreEqual(0.6f, MusicFade.Step(0f, 0.6f, 0.016f, 0f));
            Assert.AreEqual(0f, MusicFade.Step(0.6f, 0f, 0.016f, 0f));
        }

        [Test]
        public void ANegativeDelta_NeverMovesTheVolumeTheWrongWay()
        {
            Assert.AreEqual(0.3f, MusicFade.Step(0.3f, 0.6f, -1f, 2f), 1e-6f);
        }
    }
}
