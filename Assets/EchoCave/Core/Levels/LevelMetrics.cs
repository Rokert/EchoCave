using System;

namespace EchoCave.Core
{
    // How hard a level is, measured on its structure. The solver proves a level can be won; this says how
    // much it asks of the player. Difficulty is a heuristic to order levels, not a promise about feel.
    public readonly struct LevelMetrics
    {
        public int Bats { get; }
        public int Hidden { get; }

        // Bats that would bounce if tapped on the first move: the traps.
        public int BlockedAtStart { get; }

        // Depth of the blocking chains (solver waves).
        public int Waves { get; }

        // Taps with perfect information; always equal to Bats for a solvable level.
        public int MinMoves { get; }

        public int Difficulty => Bats + 2 * Hidden + 3 * Waves + BlockedAtStart;

        private LevelMetrics(int bats, int hidden, int blockedAtStart, int waves, int minMoves)
        {
            Bats = bats;
            Hidden = hidden;
            BlockedAtStart = blockedAtStart;
            Waves = waves;
            MinMoves = minMoves;
        }

        // False when the level is invalid or cannot be solved, because then there is nothing to measure.
        public static bool TryMeasure(LevelDefinition level, MoveResolver resolver, out LevelMetrics metrics)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            metrics = default;
            if (LevelValidator.Validate(level).Count > 0) return false;

            SolveResult result = new LevelSolver(resolver).Solve(level);
            if (!result.IsSolvable) return false;

            Board board = LevelBuilder.Build(level).Board;
            int blocked = 0;
            foreach (Bat bat in board.Bats)
            {
                if (resolver.Resolve(board, bat.Id).Outcome != MoveOutcome.Exited) blocked++;
            }

            int hidden = 0;
            foreach (BatDefinition bat in level.Bats)
            {
                if (bat.IsHidden) hidden++;
            }

            metrics = new LevelMetrics(level.Bats.Count, hidden, blocked, result.Waves.Count, result.MinMoves);
            return true;
        }
    }
}
