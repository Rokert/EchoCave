using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class BoardTests
    {
        private static Bat BatAt(int id, int x, int y, Direction direction = Direction.Up, bool hidden = false)
        {
            return new Bat(id, new Cell(x, y), direction, hidden);
        }

        [Test]
        public void Constructor_RejectsNonPositiveSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(3, -1));
        }

        [TestCase(0, 0, true)]
        [TestCase(3, 2, true)]
        [TestCase(-1, 0, false)]
        [TestCase(0, -1, false)]
        [TestCase(4, 0, false)]
        [TestCase(0, 3, false)]
        public void IsInside_RespectsBounds(int x, int y, bool expected)
        {
            var board = new Board(4, 3);

            Assert.AreEqual(expected, board.IsInside(new Cell(x, y)));
        }

        [Test]
        public void Add_MakesBatQueryableByCellAndId()
        {
            var board = new Board(3, 3);
            Bat bat = BatAt(7, 1, 2, Direction.Left);

            board.Add(bat);

            Assert.IsTrue(board.IsOccupied(new Cell(1, 2)));
            Assert.IsTrue(board.TryGetBat(new Cell(1, 2), out Bat byCell));
            Assert.IsTrue(board.TryGetBat(7, out Bat byId));
            Assert.AreSame(bat, byCell);
            Assert.AreSame(bat, byId);
            Assert.AreEqual(1, board.Count);
        }

        [Test]
        public void Add_RejectsOutOfBounds()
        {
            var board = new Board(3, 3);

            Assert.Throws<ArgumentException>(() => board.Add(BatAt(1, 3, 0)));
            Assert.Throws<ArgumentException>(() => board.Add(BatAt(2, 0, -1)));
            Assert.AreEqual(0, board.Count);
        }

        [Test]
        public void Add_RejectsOccupiedCell()
        {
            var board = new Board(3, 3);
            board.Add(BatAt(1, 1, 1));

            Assert.Throws<ArgumentException>(() => board.Add(BatAt(2, 1, 1)));
            Assert.AreEqual(1, board.Count);
        }

        [Test]
        public void Add_RejectsDuplicateId()
        {
            var board = new Board(3, 3);
            board.Add(BatAt(1, 0, 0));

            Assert.Throws<ArgumentException>(() => board.Add(BatAt(1, 2, 2)));
            Assert.AreEqual(1, board.Count);
        }

        [Test]
        public void Add_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new Board(3, 3).Add(null));
        }

        [Test]
        public void Remove_FreesTheCellAndForgetsTheBat()
        {
            var board = new Board(3, 3);
            board.Add(BatAt(1, 1, 1));

            bool removed = board.Remove(1);

            Assert.IsTrue(removed);
            Assert.IsFalse(board.IsOccupied(new Cell(1, 1)));
            Assert.IsFalse(board.TryGetBat(1, out _));
            Assert.AreEqual(0, board.Count);
            Assert.DoesNotThrow(() => board.Add(BatAt(2, 1, 1)));
        }

        [Test]
        public void Remove_UnknownIdReturnsFalse()
        {
            Assert.IsFalse(new Board(3, 3).Remove(42));
        }

        [Test]
        public void Bats_KeepInsertionOrder()
        {
            var board = new Board(3, 3);
            board.Add(BatAt(5, 2, 2));
            board.Add(BatAt(1, 0, 0));
            board.Add(BatAt(3, 1, 1));
            board.Remove(1);

            Assert.AreEqual(new[] { 5, 3 }, new[] { board.Bats[0].Id, board.Bats[1].Id });
        }

        [Test]
        public void Insert_PlacesBatAtGivenPosition()
        {
            var board = new Board(3, 3);
            board.Add(BatAt(1, 0, 0));
            board.Add(BatAt(3, 2, 2));

            board.Insert(1, BatAt(2, 1, 1));

            Assert.AreEqual(new[] { 1, 2, 3 }, new[] { board.Bats[0].Id, board.Bats[1].Id, board.Bats[2].Id });
        }

        [Test]
        public void Insert_RejectsInvalidIndex()
        {
            var board = new Board(3, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => board.Insert(1, BatAt(1, 0, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.Insert(-1, BatAt(1, 0, 0)));
            Assert.AreEqual(0, board.Count);
        }

        [Test]
        public void TryGetBat_EmptyCellReturnsFalse()
        {
            Assert.IsFalse(new Board(3, 3).TryGetBat(new Cell(1, 1), out Bat bat));
            Assert.IsNull(bat);
        }
    }
}
