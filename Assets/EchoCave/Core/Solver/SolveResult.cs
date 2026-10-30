using System.Collections.Generic;

namespace EchoCave.Core
{
    public sealed class SolveResult
    {
        public bool IsSolvable { get; }

        // Bat ids in a valid order that clears the board. Partial when the level is not solvable.
        public IReadOnlyList<int> Solution { get; }

        // Bats that can leave at the same time, wave by wave. The number of waves is the depth of the
        // blocking chains: a flat level has one wave, a long chain has many.
        public IReadOnlyList<IReadOnlyList<int>> Waves { get; }

        // Bats that can never leave (they block each other). Empty when the level is solvable.
        public IReadOnlyList<int> DeadlockedBatIds { get; }

        // Minimum taps with perfect information, or -1 when not solvable.
        public int MinMoves { get; }

        // True when solvable and the move limit (if any) allows the minimum.
        public bool FitsMoveLimit { get; }

        public SolveResult(
            IReadOnlyList<int> solution,
            IReadOnlyList<IReadOnlyList<int>> waves,
            IReadOnlyList<int> deadlockedBatIds,
            bool fitsMoveLimit)
        {
            Solution = solution;
            Waves = waves;
            DeadlockedBatIds = deadlockedBatIds;
            IsSolvable = deadlockedBatIds.Count == 0;
            MinMoves = IsSolvable ? solution.Count : -1;
            FitsMoveLimit = IsSolvable && fitsMoveLimit;
        }
    }
}
