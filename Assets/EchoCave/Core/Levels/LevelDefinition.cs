using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // Plain description of a level. It is not validated on construction, so tools can hold work in
    // progress; run LevelValidator before using it. A bat's id is its index in Bats.
    public sealed class LevelDefinition
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<BatDefinition> Bats { get; }

        // 0 means no move limit.
        public int MoveLimit { get; }
        public int PingBudget { get; }

        // Three stars at or under ThreeStarMoves, two at or under TwoStarMoves, otherwise one.
        public int ThreeStarMoves { get; }
        public int TwoStarMoves { get; }

        public bool HasMoveLimit => MoveLimit > 0;

        public LevelDefinition(
            int width,
            int height,
            IEnumerable<BatDefinition> bats,
            int moveLimit,
            int pingBudget,
            int threeStarMoves,
            int twoStarMoves)
        {
            if (bats == null) throw new ArgumentNullException(nameof(bats));

            Width = width;
            Height = height;
            Bats = new List<BatDefinition>(bats).AsReadOnly();
            MoveLimit = moveLimit;
            PingBudget = pingBudget;
            ThreeStarMoves = threeStarMoves;
            TwoStarMoves = twoStarMoves;
        }

        public int StarsFor(int movesUsed)
        {
            if (movesUsed <= ThreeStarMoves) return 3;
            if (movesUsed <= TwoStarMoves) return 2;
            return 1;
        }
    }
}
