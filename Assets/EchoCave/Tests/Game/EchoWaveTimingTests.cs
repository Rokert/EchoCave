using System;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class EchoWaveTimingTests
    {
        [Test]
        public void TheDelay_IsTheDistanceOverTheSpeed()
        {
            Assert.AreEqual(0.25f, EchoWaveTiming.DelayFor(3.5f, 14f), 1e-6f);
            Assert.AreEqual(1f, EchoWaveTiming.DelayFor(14f, 14f), 1e-6f);
        }

        [Test]
        public void ABatAtTheCentre_IsRevealedAtOnce()
        {
            Assert.AreEqual(0f, EchoWaveTiming.DelayFor(0f, 14f));
        }

        [Test]
        public void ADoubledSpeed_HalvesTheDelay()
        {
            float slow = EchoWaveTiming.DelayFor(4f, 8f);
            float fast = EchoWaveTiming.DelayFor(4f, 16f);

            Assert.AreEqual(slow * 0.5f, fast, 1e-6f);
        }

        [Test]
        public void ASpeedThatCannotMove_OrANegativeDistance_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoWaveTiming.DelayFor(1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoWaveTiming.DelayFor(1f, -2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoWaveTiming.DelayFor(-1f, 14f));
        }

        [Test]
        public void TheReach_CoversTheFarthestCornerAndHalfACellMore()
        {
            // 5x5: the corner cell centre is (2, 2) cells from the middle, so sqrt(8) + 0.5.
            Assert.AreEqual(Math.Sqrt(8) + 0.5, EchoWaveTiming.ReachCells(5, 5), 1e-5);

            // One cell: the ring only has to pass the bat standing on it.
            Assert.AreEqual(0.5f, EchoWaveTiming.ReachCells(1, 1), 1e-6f);
        }

        [Test]
        public void TheReach_ReachesEveryBatOfTheBoard()
        {
            // A ring that stops short would leave a corner bat hidden forever, because it is revealed on arrival.
            foreach (int size in new[] { 2, 3, 5, 7 })
            {
                float farthestCentre = (float)Math.Sqrt(2) * (size - 1) * 0.5f;
                Assert.Greater(EchoWaveTiming.ReachCells(size, size), farthestCentre, $"{size}x{size}");
            }
        }

        [Test]
        public void ABoardWithNoCells_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoWaveTiming.ReachCells(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoWaveTiming.ReachCells(5, 0));
        }
    }
}
