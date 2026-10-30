using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // Relies on two rules (D-010): a bounce never changes the board, and removing a bat can only free
    // paths, never block them. So clearing all bats that are free, wave after wave, finds a solution
    // whenever one exists; no search is needed. Hidden bats behave like visible ones here, because the
    // solver has perfect information.
    public sealed class LevelSolver
    {
        private readonly MoveResolver _resolver;

        public LevelSolver(MoveResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        // Throws InvalidLevelException when the level is structurally invalid.
        public SolveResult Solve(LevelDefinition level)
        {
            Board board = LevelBuilder.Build(level).Board;

            var solution = new List<int>();
            var waves = new List<IReadOnlyList<int>>();

            while (board.Count > 0)
            {
                var free = new List<int>();
                foreach (Bat bat in board.Bats)
                {
                    if (_resolver.Resolve(board, bat.Id).Outcome == MoveOutcome.Exited)
                        free.Add(bat.Id);
                }

                if (free.Count == 0) break;

                foreach (int id in free) board.Remove(id);
                solution.AddRange(free);
                waves.Add(free);
            }

            var deadlocked = new List<int>(board.Count);
            foreach (Bat bat in board.Bats) deadlocked.Add(bat.Id);

            bool fitsLimit = !level.HasMoveLimit || solution.Count <= level.MoveLimit;
            return new SolveResult(solution, waves, deadlocked, fitsLimit);
        }
    }
}
