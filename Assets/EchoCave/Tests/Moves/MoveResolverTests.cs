using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class MoveResolverTests
    {
        private MoveResolver _resolver;
        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _resolver = new MoveResolver();
            _board = new Board(5, 5);
        }

        private void Place(int id, int x, int y, Direction direction, bool hidden = false)
        {
            _board.Add(new Bat(id, new Cell(x, y), direction, hidden));
        }

        [TestCase(Direction.Up, 2, 4)]
        [TestCase(Direction.Down, 2, 0)]
        [TestCase(Direction.Left, 0, 2)]
        [TestCase(Direction.Right, 4, 2)]
        public void ClearPath_Exits_AndReportsLastCellInside(Direction direction, int lastX, int lastY)
        {
            Place(1, 2, 2, direction);

            MoveResult result = _resolver.Resolve(_board, 1);

            Assert.AreEqual(MoveOutcome.Exited, result.Outcome);
            Assert.AreEqual(new Cell(lastX, lastY), result.StopCell);
            Assert.IsNull(result.BlockingBat);
        }

        [Test]
        public void BatOnEdgeFacingOut_ExitsImmediately()
        {
            Place(1, 4, 2, Direction.Right);

            MoveResult result = _resolver.Resolve(_board, 1);

            Assert.AreEqual(MoveOutcome.Exited, result.Outcome);
            Assert.AreEqual(new Cell(4, 2), result.StopCell);
        }

        [Test]
        public void BatInCornerFacingOut_ExitsImmediately()
        {
            Place(1, 0, 0, Direction.Down);

            Assert.AreEqual(MoveOutcome.Exited, _resolver.Resolve(_board, 1).Outcome);
        }

        [Test]
        public void BlockedAdjacent_Bounces_AndStopCellIsOwnCell()
        {
            Place(1, 1, 2, Direction.Right);
            Place(2, 2, 2, Direction.Up);

            MoveResult result = _resolver.Resolve(_board, 1);

            Assert.AreEqual(MoveOutcome.Bounced, result.Outcome);
            Assert.AreEqual(new Cell(1, 2), result.StopCell);
            Assert.AreEqual(2, result.BlockingBat.Id);
        }

        [Test]
        public void BlockedFarAway_Bounces_AndStopsRightBeforeTheBlocker()
        {
            Place(1, 0, 2, Direction.Right);
            Place(2, 3, 2, Direction.Down);

            MoveResult result = _resolver.Resolve(_board, 1);

            Assert.AreEqual(MoveOutcome.Bounced, result.Outcome);
            Assert.AreEqual(new Cell(2, 2), result.StopCell);
            Assert.AreEqual(2, result.BlockingBat.Id);
        }

        [Test]
        public void FirstBlockerWins_WhenSeveralBatsAreInThePath()
        {
            Place(1, 0, 0, Direction.Up);
            Place(2, 0, 2, Direction.Right);
            Place(3, 0, 4, Direction.Right);

            MoveResult result = _resolver.Resolve(_board, 1);

            Assert.AreEqual(2, result.BlockingBat.Id);
            Assert.AreEqual(new Cell(0, 1), result.StopCell);
        }

        [Test]
        public void BatsOutsideTheLine_DoNotBlock()
        {
            Place(1, 2, 2, Direction.Right);
            Place(2, 3, 3, Direction.Left);
            Place(3, 1, 2, Direction.Left);

            Assert.AreEqual(MoveOutcome.Exited, _resolver.Resolve(_board, 1).Outcome);
        }

        [Test]
        public void HiddenBlocker_BlocksLikeAVisibleOne()
        {
            Place(1, 0, 0, Direction.Right);
            Place(2, 2, 0, Direction.Up, hidden: true);

            MoveResult result = _resolver.Resolve(_board, 1);

            Assert.AreEqual(MoveOutcome.Bounced, result.Outcome);
            Assert.AreEqual(2, result.BlockingBat.Id);
        }

        [Test]
        public void HiddenMover_FliesLikeAVisibleOne()
        {
            Place(1, 0, 0, Direction.Right, hidden: true);

            Assert.AreEqual(MoveOutcome.Exited, _resolver.Resolve(_board, 1).Outcome);
        }

        [Test]
        public void Resolve_DoesNotChangeTheBoard()
        {
            Place(1, 0, 2, Direction.Right);
            Place(2, 3, 2, Direction.Up);

            _resolver.Resolve(_board, 1);

            Assert.AreEqual(2, _board.Count);
            Assert.IsTrue(_board.TryGetBat(new Cell(0, 2), out Bat bat));
            Assert.AreEqual(1, bat.Id);
        }

        [Test]
        public void Resolve_IsDeterministic()
        {
            Place(1, 0, 2, Direction.Right);
            Place(2, 3, 2, Direction.Up);

            MoveResult a = _resolver.Resolve(_board, 1);
            MoveResult b = _resolver.Resolve(_board, 1);

            Assert.AreEqual((a.Outcome, a.StopCell, a.BlockingBat), (b.Outcome, b.StopCell, b.BlockingBat));
        }

        [Test]
        public void UnknownBat_Throws()
        {
            Assert.Throws<ArgumentException>(() => _resolver.Resolve(_board, 99));
        }

        [Test]
        public void NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _resolver.Resolve(null, 1));
        }
    }
}
