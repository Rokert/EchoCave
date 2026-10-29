using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LevelBuilderTests
    {
        private static LevelDefinition Level(params BatDefinition[] bats)
        {
            return new LevelDefinition(4, 3, bats, moveLimit: 5, pingBudget: 2, threeStarMoves: 2, twoStarMoves: 4);
        }

        [Test]
        public void Build_CreatesBoardWithTheDefinedBats()
        {
            var level = Level(
                new BatDefinition(new Cell(0, 0), Direction.Up),
                new BatDefinition(new Cell(3, 2), Direction.Left, true));

            LevelSession session = LevelBuilder.Build(level);

            Assert.AreEqual(4, session.Board.Width);
            Assert.AreEqual(3, session.Board.Height);
            Assert.AreEqual(2, session.Board.Count);
            Assert.AreEqual(0, session.MovesUsed);
        }

        [Test]
        public void Build_AssignsIdsByIndex_AndKeepsAllFields()
        {
            var level = Level(
                new BatDefinition(new Cell(0, 0), Direction.Up),
                new BatDefinition(new Cell(3, 2), Direction.Left, true));

            LevelSession session = LevelBuilder.Build(level);

            Assert.IsTrue(session.Board.TryGetBat(0, out Bat first));
            Assert.IsTrue(session.Board.TryGetBat(1, out Bat second));
            Assert.AreEqual((new Cell(0, 0), Direction.Up, false), (first.Cell, first.Direction, first.IsHidden));
            Assert.AreEqual((new Cell(3, 2), Direction.Left, true), (second.Cell, second.Direction, second.IsHidden));
        }

        [Test]
        public void Build_ProducesIndependentSessionsFromTheSameDefinition()
        {
            var level = Level(new BatDefinition(new Cell(0, 0), Direction.Up));

            LevelSession a = LevelBuilder.Build(level);
            LevelSession b = LevelBuilder.Build(level);
            a.Board.Remove(0);

            Assert.AreEqual(0, a.Board.Count);
            Assert.AreEqual(1, b.Board.Count);
        }

        [Test]
        public void Build_InvalidLevel_ThrowsWithEveryError()
        {
            var level = new LevelDefinition(
                2, 2,
                new[] { new BatDefinition(new Cell(5, 5), Direction.Up) },
                moveLimit: -1, pingBudget: 0, threeStarMoves: 1, twoStarMoves: 1);

            var exception = Assert.Throws<InvalidLevelException>(() => LevelBuilder.Build(level));

            Assert.GreaterOrEqual(exception.Errors.Count, 2);
            StringAssert.Contains("outside", exception.Message);
            StringAssert.Contains("Move limit", exception.Message);
        }

        [Test]
        public void Build_NullLevel_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LevelBuilder.Build(null));
        }

        [Test]
        public void Definition_CopiesTheBatList()
        {
            var bats = new[] { new BatDefinition(new Cell(0, 0), Direction.Up) };
            var level = Level(bats);

            bats[0] = new BatDefinition(new Cell(1, 1), Direction.Down);

            Assert.AreEqual(new Cell(0, 0), level.Bats[0].Cell);
        }

        [TestCase(1, 3)]
        [TestCase(2, 3)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 1)]
        [TestCase(40, 1)]
        public void StarsFor_FollowsThresholds(int movesUsed, int expectedStars)
        {
            var level = Level(new BatDefinition(new Cell(0, 0), Direction.Up));

            Assert.AreEqual(expectedStars, level.StarsFor(movesUsed));
        }
    }
}
