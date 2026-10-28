using System;

namespace EchoCave.Core
{
    // Mutable state of the level being played: the board and the moves spent so far.
    public sealed class LevelSession
    {
        public Board Board { get; }
        public int MovesUsed { get; private set; }

        public LevelSession(Board board)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
        }

        internal void SpendMove() => MovesUsed++;

        internal void RefundMove() => MovesUsed--;
    }
}
