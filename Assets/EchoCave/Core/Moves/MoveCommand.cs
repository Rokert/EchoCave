using System;
using Rokert.Core;

namespace EchoCave.Core
{
    // One tap. Every tap spends a move; only an exit changes the board.
    // The result is resolved on Execute, so redo after undo behaves identically.
    public sealed class MoveCommand : IUndoableCommand
    {
        private readonly LevelSession _session;
        private readonly MoveResolver _resolver;
        private readonly int _batId;

        private bool _executed;
        private Bat _removedBat;
        private int _removedIndex;

        public MoveResult Result { get; private set; }

        public MoveCommand(LevelSession session, MoveResolver resolver, int batId)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _batId = batId;
        }

        public void Execute()
        {
            if (_executed) throw new InvalidOperationException("Move already executed; undo it first.");

            Board board = _session.Board;
            Result = _resolver.Resolve(board, _batId);

            if (Result.Outcome == MoveOutcome.Exited)
            {
                _removedBat = Result.Bat;
                _removedIndex = IndexOf(board, _batId);
                board.Remove(_batId);
            }

            _session.SpendMove();
            _executed = true;
        }

        public void Undo()
        {
            if (!_executed) throw new InvalidOperationException("Move was not executed.");

            if (_removedBat != null)
            {
                _session.Board.Insert(_removedIndex, _removedBat);
                _removedBat = null;
            }

            _session.RefundMove();
            _executed = false;
        }

        private static int IndexOf(Board board, int batId)
        {
            for (int i = 0; i < board.Bats.Count; i++)
            {
                if (board.Bats[i].Id == batId) return i;
            }

            return -1;
        }
    }
}
