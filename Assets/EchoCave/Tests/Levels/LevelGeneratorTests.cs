using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LevelGeneratorTests
    {
        private const int Attempts = 20000;
        private static readonly MoveResolver Resolver = new MoveResolver();

        private readonly LevelGenerator _generator = new LevelGenerator(Resolver);

        // 5x5, six bats of which one hidden, two pings, three taps of slack, chains two deep with a trap.
        private static LevelSpec Spec(int slack = 3, int step = 10)
        {
            return new LevelSpec(5, 5, 6, 1, 2, slack, 2, 1, step, 2);
        }

        private static bool HaveSameBats(LevelDefinition a, LevelDefinition b)
        {
            if (a.Bats.Count != b.Bats.Count) return false;
            for (int i = 0; i < a.Bats.Count; i++)
            {
                if (a.Bats[i].Cell != b.Bats[i].Cell
                    || a.Bats[i].Direction != b.Bats[i].Direction
                    || a.Bats[i].IsHidden != b.Bats[i].IsHidden)
                    return false;
            }

            return true;
        }

        private static void AssertSameBats(LevelDefinition a, LevelDefinition b)
        {
            Assert.AreEqual(a.Bats.Count, b.Bats.Count);
            for (int i = 0; i < a.Bats.Count; i++)
            {
                Assert.AreEqual(a.Bats[i].Cell, b.Bats[i].Cell, $"cell of bat {i}");
                Assert.AreEqual(a.Bats[i].Direction, b.Bats[i].Direction, $"direction of bat {i}");
                Assert.AreEqual(a.Bats[i].IsHidden, b.Bats[i].IsHidden, $"hidden flag of bat {i}");
            }
        }

        [Test]
        public void TheLevel_IsValid_AndSolvable_AndMatchesTheSpec()
        {
            LevelDefinition level = _generator.Generate(Spec(), 7, 0, Attempts);

            Assert.IsNotNull(level, "no layout fit the spec");
            Assert.IsEmpty(LevelValidator.Validate(level));
            Assert.IsTrue(LevelMetrics.TryMeasure(level, Resolver, out LevelMetrics metrics));
            Assert.AreEqual(5, level.Width);
            Assert.AreEqual(5, level.Height);
            Assert.AreEqual(6, level.Bats.Count);
            Assert.AreEqual(1, metrics.Hidden);
            Assert.AreEqual(2, level.PingBudget);
            Assert.GreaterOrEqual(metrics.Waves, 2);
            Assert.GreaterOrEqual(metrics.BlockedAtStart, 1);
        }

        [Test]
        public void TheMoveLimit_IsTheMinimumPlusTheSlack_AndTheStarsFollowTheMinimum()
        {
            LevelDefinition level = _generator.Generate(Spec(slack: 3), 7, 0, Attempts);

            Assert.AreEqual(6 + 3, level.MoveLimit);
            Assert.AreEqual(6, level.ThreeStarMoves);
            Assert.AreEqual(8, level.TwoStarMoves);
        }

        [Test]
        public void ANegativeSlack_MeansNoMoveLimit()
        {
            LevelDefinition level = _generator.Generate(Spec(slack: -1), 7, 0, Attempts);

            Assert.IsFalse(level.HasMoveLimit);
            Assert.AreEqual(6, level.ThreeStarMoves);
        }

        [Test]
        public void TheSameSpecSeedAndFloor_GiveTheSameLevel()
        {
            LevelDefinition a = _generator.Generate(Spec(), 11, 0, Attempts);
            LevelDefinition b = new LevelGenerator(new MoveResolver()).Generate(Spec(), 11, 0, Attempts);

            AssertSameBats(a, b);
        }

        [Test]
        public void DifferentSeeds_GiveDifferentLevels()
        {
            LevelDefinition a = _generator.Generate(Spec(), 1, 0, Attempts);
            LevelDefinition b = _generator.Generate(Spec(), 2, 0, Attempts);

            Assert.IsFalse(HaveSameBats(a, b));
        }

        [Test]
        public void TheDifficulty_StaysInsideTheWindowAboveTheFloor()
        {
            LevelDefinition level = _generator.Generate(Spec(step: 5), 3, 18, Attempts);

            Assert.IsNotNull(level, "no layout fit the window");
            LevelMetrics.TryMeasure(level, Resolver, out LevelMetrics metrics);
            Assert.That(metrics.Difficulty, Is.InRange(18, 23));
        }

        [Test]
        public void ReturnsNull_WhenNoLayoutCanReachTheFloor()
        {
            // Six bats can give at most 6 + 2 for the hidden bat + 3 * 6 waves + 5 traps = 31, far under a floor of 200.
            LevelDefinition level = _generator.Generate(Spec(), 1, 200, 500);

            Assert.IsNull(level);
        }

        [Test]
        public void ASpecAskingForMoreWavesThanBatsExist_ReturnsNull()
        {
            var spec = new LevelSpec(3, 3, 2, 0, 0, -1, 5, 0, 10, 2);

            Assert.IsNull(_generator.Generate(spec, 1, 0, 500));
        }

        [Test]
        public void InvalidArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new LevelGenerator(null));
            Assert.Throws<ArgumentNullException>(() => _generator.Generate(null, 1, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => _generator.Generate(Spec(), 1, 0, 0));
        }

        [Test]
        public void ASpecThatCannotExist_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelSpec(0, 5, 3, 0, 0, 1, 1, 0, 5, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelSpec(2, 2, 5, 0, 0, 1, 1, 0, 5, 1), "5 bats on 4 cells");
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelSpec(5, 5, 3, 4, 0, 1, 1, 0, 5, 1), "more hidden than bats");
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelSpec(5, 5, 3, 0, -1, 1, 1, 0, 5, 1), "negative pings");
        }
    }
}
