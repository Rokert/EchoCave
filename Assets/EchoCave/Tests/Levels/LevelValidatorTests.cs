using System;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LevelValidatorTests
    {
        private static LevelDefinition Level(
            int width = 4,
            int height = 4,
            BatDefinition[] bats = null,
            int moveLimit = 0,
            int pingBudget = 0,
            int threeStar = 2,
            int twoStar = 4)
        {
            bats = bats ?? new[]
            {
                new BatDefinition(new Cell(0, 0), Direction.Up),
                new BatDefinition(new Cell(1, 1), Direction.Right, true)
            };
            return new LevelDefinition(width, height, bats, moveLimit, pingBudget, threeStar, twoStar);
        }

        [Test]
        public void ValidLevel_HasNoErrors()
        {
            Assert.IsEmpty(LevelValidator.Validate(Level()));
        }

        [Test]
        public void ValidLevelWithMoveLimit_HasNoErrors()
        {
            Assert.IsEmpty(LevelValidator.Validate(Level(moveLimit: 6, threeStar: 2, twoStar: 6)));
        }

        [TestCase(0, 4, "Width")]
        [TestCase(4, 0, "Height")]
        [TestCase(-2, 4, "Width")]
        public void NonPositiveSize_IsReported(int width, int height, string expected)
        {
            var errors = LevelValidator.Validate(Level(width, height, new BatDefinition[0]));

            Assert.IsTrue(errors.Any(e => e.Contains(expected)), string.Join(" | ", errors));
        }

        [Test]
        public void NoBats_IsReported()
        {
            var errors = LevelValidator.Validate(Level(bats: new BatDefinition[0]));

            Assert.IsTrue(errors.Any(e => e.Contains("no bats")));
        }

        [TestCase(4, 0)]
        [TestCase(0, 4)]
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        public void BatOutsideBoard_IsReported(int x, int y)
        {
            var bats = new[] { new BatDefinition(new Cell(x, y), Direction.Up) };

            var errors = LevelValidator.Validate(Level(bats: bats));

            Assert.IsTrue(errors.Any(e => e.Contains("outside")), string.Join(" | ", errors));
        }

        [Test]
        public void TwoBatsInOneCell_IsReported()
        {
            var bats = new[]
            {
                new BatDefinition(new Cell(1, 1), Direction.Up),
                new BatDefinition(new Cell(1, 1), Direction.Down)
            };

            var errors = LevelValidator.Validate(Level(bats: bats));

            Assert.IsTrue(errors.Any(e => e.Contains("shares cell")), string.Join(" | ", errors));
        }

        [Test]
        public void NegativeMoveLimitAndPings_AreReported()
        {
            var errors = LevelValidator.Validate(Level(moveLimit: -1, pingBudget: -3));

            Assert.IsTrue(errors.Any(e => e.Contains("Move limit")));
            Assert.IsTrue(errors.Any(e => e.Contains("Ping budget")));
        }

        [Test]
        public void StarThresholds_MustBeOrdered()
        {
            var errors = LevelValidator.Validate(Level(threeStar: 5, twoStar: 3));

            Assert.IsTrue(errors.Any(e => e.Contains("Two-star")));
        }

        [Test]
        public void ThreeStarThreshold_MustBePositive()
        {
            var errors = LevelValidator.Validate(Level(threeStar: 0, twoStar: 3));

            Assert.IsTrue(errors.Any(e => e.Contains("Three-star")));
        }

        [Test]
        public void TwoStarThreshold_CannotExceedMoveLimit()
        {
            var errors = LevelValidator.Validate(Level(moveLimit: 3, threeStar: 2, twoStar: 5));

            Assert.IsTrue(errors.Any(e => e.Contains("move limit")));
        }

        [Test]
        public void SeveralProblems_AreAllReportedTogether()
        {
            var bats = new[] { new BatDefinition(new Cell(9, 9), Direction.Up) };

            var errors = LevelValidator.Validate(Level(bats: bats, moveLimit: -1, threeStar: 0, twoStar: 0));

            Assert.GreaterOrEqual(errors.Count, 3);
        }

        [Test]
        public void NullLevel_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LevelValidator.Validate(null));
        }
    }
}
