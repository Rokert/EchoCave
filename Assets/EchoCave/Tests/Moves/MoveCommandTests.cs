using System;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class MoveCommandTests
    {
        private LevelSession _session;
        private MoveResolver _resolver;
        private CommandHistory _history;

        [SetUp]
        public void SetUp()
        {
            var board = new Board(5, 5);
            board.Add(new Bat(1, new Cell(0, 0), Direction.Up));               // clear path up
            board.Add(new Bat(2, new Cell(2, 2), Direction.Right));            // blocked by 3
            board.Add(new Bat(3, new Cell(4, 2), Direction.Up, true));         // clear path up
            board.Add(new Bat(4, new Cell(1, 4), Direction.Left));             // clear path left
            _session = new LevelSession(board);
            _resolver = new MoveResolver();
            _history = new CommandHistory();
        }

        private MoveCommand Tap(int batId) => new MoveCommand(_session, _resolver, batId);

        // Order matters: it must be restored exactly, not just the set of bats.
        private string BoardSnapshot()
        {
            return string.Join(";", _session.Board.Bats.Select(b =>
                $"{b.Id}@{b.Cell}{b.Direction}{(b.IsHidden ? "h" : "")}"));
        }

        private string Snapshot() => $"{BoardSnapshot()}|moves={_session.MovesUsed}";

        [Test]
        public void Exit_RemovesTheBat_AndSpendsAMove()
        {
            var command = Tap(1);

            _history.Execute(command);

            Assert.AreEqual(MoveOutcome.Exited, command.Result.Outcome);
            Assert.IsFalse(_session.Board.TryGetBat(1, out _));
            Assert.AreEqual(1, _session.MovesUsed);
        }

        [Test]
        public void Bounce_KeepsTheBoard_AndSpendsAMove()
        {
            string boardBefore = BoardSnapshot();
            var command = Tap(2);

            _history.Execute(command);

            Assert.AreEqual(MoveOutcome.Bounced, command.Result.Outcome);
            Assert.AreEqual(boardBefore, BoardSnapshot());
            Assert.AreEqual(1, _session.MovesUsed);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ExecuteUndo_RestoresExactState(int batId)
        {
            string before = Snapshot();

            _history.Execute(Tap(batId));
            _history.Undo();

            Assert.AreEqual(before, Snapshot());
        }

        [TestCase(1)]
        [TestCase(2)]
        public void ExecuteUndoRedo_MatchesFirstExecution(int batId)
        {
            _history.Execute(Tap(batId));
            string afterExecute = Snapshot();

            _history.Undo();
            _history.Redo();

            Assert.AreEqual(afterExecute, Snapshot());
        }

        [Test]
        public void Undo_RestoresRemovedBatAtItsOriginalPosition()
        {
            _history.Execute(Tap(3));

            _history.Undo();

            Assert.AreEqual(new[] { 1, 2, 3, 4 }, _session.Board.Bats.Select(b => b.Id).ToArray());
        }

        [Test]
        public void Undo_RestoresHiddenFlagAndDirection()
        {
            _history.Execute(Tap(3));
            _history.Undo();

            Assert.IsTrue(_session.Board.TryGetBat(3, out Bat bat));
            Assert.IsTrue(bat.IsHidden);
            Assert.AreEqual(Direction.Up, bat.Direction);
        }

        [Test]
        public void SeveralMoves_UndoneInReverseOrder_ReachTheStart()
        {
            string start = Snapshot();

            _history.Execute(Tap(2));   // bounce
            _history.Execute(Tap(3));   // exit, frees the path of 2
            _history.Execute(Tap(2));   // now exits
            Assert.AreEqual(3, _session.MovesUsed);
            Assert.AreEqual(2, _session.Board.Count);

            while (_history.Undo()) { }

            Assert.AreEqual(start, Snapshot());
        }

        [Test]
        public void Redo_ReResolvesAgainstTheRestoredBoard()
        {
            _history.Execute(Tap(3));   // exit, frees the path of 2
            _history.Execute(Tap(2));   // exits too
            string afterBoth = Snapshot();
            _history.Undo();
            _history.Undo();

            _history.Redo();
            _history.Redo();

            Assert.AreEqual(afterBoth, Snapshot());
            Assert.IsFalse(_session.Board.TryGetBat(2, out _));
            Assert.AreEqual(2, _session.MovesUsed);
        }

        [Test]
        public void ExecuteTwiceWithoutUndo_Throws()
        {
            var command = Tap(1);
            command.Execute();

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.AreEqual(1, _session.MovesUsed);
        }

        [Test]
        public void UndoBeforeExecute_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Tap(1).Undo());
        }

        [Test]
        public void UnknownBat_ThrowsAndSpendsNothing()
        {
            Assert.Throws<ArgumentException>(() => Tap(99).Execute());
            Assert.AreEqual(0, _session.MovesUsed);
        }

        [Test]
        public void Constructor_RejectsNulls()
        {
            Assert.Throws<ArgumentNullException>(() => new MoveCommand(null, _resolver, 1));
            Assert.Throws<ArgumentNullException>(() => new MoveCommand(_session, null, 1));
        }
    }
}
