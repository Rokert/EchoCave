using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class HintPickerTests
    {
        private readonly HintPicker _picker = new HintPicker(new MoveResolver());

        private static Board BoardOf(int width, int height, params Bat[] bats)
        {
            var board = new Board(width, height);
            foreach (Bat bat in bats) board.Add(bat);
            return board;
        }

        private static Bat B(int id, int x, int y, Direction direction, bool hidden = false)
        {
            return new Bat(id, new Cell(x, y), direction, hidden);
        }

        [Test]
        public void ALoneFreeBat_IsTheHint()
        {
            Board board = BoardOf(5, 5, B(7, 2, 2, Direction.Right));

            HintPick? pick = _picker.Pick(board);

            Assert.IsTrue(pick.HasValue);
            Assert.AreEqual(7, pick.Value.BatId);
            Assert.AreEqual(0, pick.Value.FreesOthers);
        }

        [Test]
        public void OnlyABatThatCanLeaveNow_IsEverPicked()
        {
            // Bat 0 is blocked by bat 1; bat 1 is free.
            Board board = BoardOf(5, 1, B(0, 0, 0, Direction.Right), B(1, 1, 0, Direction.Right));

            Assert.AreEqual(1, _picker.Pick(board).Value.BatId);
        }

        [Test]
        public void TheBatThatFreesTheMost_BeatsALowerIdThatFreesNothing()
        {
            // Bat 0 (free, frees nothing) has the lowest id. Bat 1 is free and, once gone, frees bats 2 and 3.
            Board board = BoardOf(5, 5,
                B(0, 0, 0, Direction.Left),
                B(1, 2, 2, Direction.Up),
                B(2, 1, 2, Direction.Right),
                B(3, 2, 1, Direction.Up));

            HintPick pick = _picker.Pick(board).Value;

            Assert.AreEqual(1, pick.BatId);
            Assert.AreEqual(2, pick.FreesOthers);
        }

        [Test]
        public void ATie_GoesToAVisibleBat_EvenWithAHigherId()
        {
            Board board = BoardOf(5, 5,
                B(0, 0, 0, Direction.Left, hidden: true),
                B(1, 0, 4, Direction.Left));

            HintPick pick = _picker.Pick(board).Value;

            Assert.AreEqual(1, pick.BatId, "a visible bat needs no reveal, so it wins the tie");
            Assert.IsFalse(pick.IsHidden);
        }

        [Test]
        public void ATieBetweenVisibleBats_GoesToTheLowestId()
        {
            Board board = BoardOf(5, 5,
                B(4, 0, 0, Direction.Left),
                B(2, 0, 4, Direction.Left),
                B(9, 4, 4, Direction.Right));

            Assert.AreEqual(2, _picker.Pick(board).Value.BatId);
        }

        [Test]
        public void ATieBetweenHiddenBats_GoesToTheLowestId()
        {
            Board board = BoardOf(5, 5,
                B(5, 0, 0, Direction.Left, hidden: true),
                B(3, 0, 4, Direction.Left, hidden: true));

            HintPick pick = _picker.Pick(board).Value;

            Assert.AreEqual(3, pick.BatId);
            Assert.IsTrue(pick.IsHidden, "the caller must know it has to reveal this bat");
        }

        [Test]
        public void AHiddenBatThatFreesMore_StillBeatsAVisibleOneThatFreesLess()
        {
            // Visible bat 0 is free and frees nothing; hidden bat 1 is free and frees two.
            Board board = BoardOf(5, 5,
                B(0, 0, 0, Direction.Left),
                B(1, 2, 2, Direction.Up, hidden: true),
                B(2, 1, 2, Direction.Right),
                B(3, 2, 1, Direction.Up));

            Assert.AreEqual(1, _picker.Pick(board).Value.BatId);
        }

        [Test]
        public void ADeadlock_HasNoHint()
        {
            Board board = BoardOf(4, 1, B(0, 1, 0, Direction.Right), B(1, 2, 0, Direction.Left));

            Assert.IsNull(_picker.Pick(board));
        }

        [Test]
        public void AnEmptyBoard_HasNoHint()
        {
            Assert.IsNull(_picker.Pick(new Board(5, 5)));
        }

        [Test]
        public void Picking_NeverChangesTheBoard()
        {
            Board board = BoardOf(5, 5,
                B(0, 0, 0, Direction.Left),
                B(1, 2, 2, Direction.Up),
                B(2, 1, 2, Direction.Right),
                B(3, 2, 1, Direction.Up));

            _picker.Pick(board);

            Assert.AreEqual(4, board.Count);
            Assert.IsTrue(board.TryGetBat(1, out Bat bat));
            Assert.AreEqual(new Cell(2, 2), bat.Cell);
        }

        [Test]
        public void TheSameBoard_AlwaysGivesTheSameHint()
        {
            Board board = BoardOf(5, 5,
                B(0, 0, 0, Direction.Left),
                B(1, 2, 2, Direction.Up),
                B(2, 1, 2, Direction.Right),
                B(3, 2, 1, Direction.Up));

            HintPick first = _picker.Pick(board).Value;
            for (int i = 0; i < 20; i++) Assert.AreEqual(first.BatId, _picker.Pick(board).Value.BatId);
        }

        [Test]
        public void FollowingEveryHint_ClearsAnyLevelWithoutAWastedMove()
        {
            // The hint is a bat that leaves, so taking it every time solves the level in exactly one move per bat.
            Board board = BoardOf(5, 5,
                B(0, 0, 0, Direction.Left),
                B(1, 2, 2, Direction.Up),
                B(2, 1, 2, Direction.Right),
                B(3, 2, 1, Direction.Up),
                B(4, 4, 3, Direction.Down),
                B(5, 4, 1, Direction.Right));
            var resolver = new MoveResolver();

            int moves = 0;
            while (board.Count > 0)
            {
                HintPick? pick = _picker.Pick(board);
                Assert.IsTrue(pick.HasValue, "a level that can be solved must always have a hint");
                Assert.AreEqual(MoveOutcome.Exited, resolver.Resolve(board, pick.Value.BatId).Outcome);
                board.Remove(pick.Value.BatId);
                moves++;
            }

            Assert.AreEqual(6, moves);
        }

        [Test]
        public void ANullBoard_OrResolver_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => _picker.Pick(null));
            Assert.Throws<ArgumentNullException>(() => new HintPicker(null));
        }
    }
}
