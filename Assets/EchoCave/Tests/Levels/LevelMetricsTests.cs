using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LevelMetricsTests
    {
        private static readonly MoveResolver Resolver = new MoveResolver();

        private static LevelDefinition Level(int width, int height, params BatDefinition[] bats)
        {
            return new LevelDefinition(width, height, bats, 0, 0, 1, 1);
        }

        // Bat 0 flies right into bat 1; bat 1 flies up and out. So bat 0 is a trap and the chain is two deep.
        private static LevelDefinition TwoBatChain(bool hiddenBlocker = false)
        {
            return Level(3, 3,
                new BatDefinition(new Cell(0, 0), Direction.Right),
                new BatDefinition(new Cell(1, 0), Direction.Up, hiddenBlocker));
        }

        [Test]
        public void MeasuresATwoBatChain()
        {
            Assert.IsTrue(LevelMetrics.TryMeasure(TwoBatChain(), Resolver, out LevelMetrics metrics));

            Assert.AreEqual(2, metrics.Bats);
            Assert.AreEqual(0, metrics.Hidden);
            Assert.AreEqual(1, metrics.BlockedAtStart);
            Assert.AreEqual(2, metrics.Waves);
            Assert.AreEqual(2, metrics.MinMoves);
            Assert.AreEqual(2 + 0 + 3 * 2 + 1, metrics.Difficulty);
        }

        [Test]
        public void AFreeBat_IsNotATrap_AndIsOneWave()
        {
            LevelDefinition level = Level(3, 3, new BatDefinition(new Cell(1, 1), Direction.Up));

            Assert.IsTrue(LevelMetrics.TryMeasure(level, Resolver, out LevelMetrics metrics));

            Assert.AreEqual(0, metrics.BlockedAtStart);
            Assert.AreEqual(1, metrics.Waves);
        }

        [Test]
        public void HiddenBats_AreCounted_AndMakeTheLevelHarder()
        {
            LevelMetrics.TryMeasure(TwoBatChain(), Resolver, out LevelMetrics plain);

            Assert.IsTrue(LevelMetrics.TryMeasure(TwoBatChain(hiddenBlocker: true), Resolver, out LevelMetrics hidden));

            Assert.AreEqual(1, hidden.Hidden);
            Assert.AreEqual(plain.Difficulty + 2, hidden.Difficulty);
        }

        [Test]
        public void ALevelWhereBatsBlockEachOther_CannotBeMeasured()
        {
            LevelDefinition deadlock = Level(2, 1,
                new BatDefinition(new Cell(0, 0), Direction.Right),
                new BatDefinition(new Cell(1, 0), Direction.Left));

            Assert.IsFalse(LevelMetrics.TryMeasure(deadlock, Resolver, out _));
        }

        [Test]
        public void AnInvalidLevel_CannotBeMeasured()
        {
            LevelDefinition outside = Level(2, 2, new BatDefinition(new Cell(5, 5), Direction.Up));

            Assert.IsFalse(LevelMetrics.TryMeasure(outside, Resolver, out _));
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => LevelMetrics.TryMeasure(null, Resolver, out _));
            Assert.Throws<ArgumentNullException>(() => LevelMetrics.TryMeasure(TwoBatChain(), null, out _));
        }
    }
}
