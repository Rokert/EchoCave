using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // Structural checks only. Whether a level can be solved is the solver's job.
    public static class LevelValidator
    {
        public static IReadOnlyList<string> Validate(LevelDefinition level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));

            var errors = new List<string>();
            bool sizeValid = true;

            if (level.Width <= 0)
            {
                errors.Add($"Width must be positive (was {level.Width}).");
                sizeValid = false;
            }

            if (level.Height <= 0)
            {
                errors.Add($"Height must be positive (was {level.Height}).");
                sizeValid = false;
            }

            if (level.Bats.Count == 0)
                errors.Add("Level has no bats.");

            var occupied = new HashSet<Cell>();
            for (int i = 0; i < level.Bats.Count; i++)
            {
                Cell cell = level.Bats[i].Cell;

                if (sizeValid && (cell.X < 0 || cell.X >= level.Width || cell.Y < 0 || cell.Y >= level.Height))
                    errors.Add($"Bat {i} is outside the {level.Width}x{level.Height} board at {cell}.");

                if (!occupied.Add(cell))
                    errors.Add($"Bat {i} shares cell {cell} with another bat.");
            }

            if (level.MoveLimit < 0)
                errors.Add($"Move limit cannot be negative (was {level.MoveLimit}); use 0 for no limit.");

            if (level.PingBudget < 0)
                errors.Add($"Ping budget cannot be negative (was {level.PingBudget}).");

            if (level.ThreeStarMoves < 1)
                errors.Add($"Three-star moves must be at least 1 (was {level.ThreeStarMoves}).");

            if (level.TwoStarMoves < level.ThreeStarMoves)
                errors.Add($"Two-star moves ({level.TwoStarMoves}) cannot be below three-star moves ({level.ThreeStarMoves}).");

            if (level.HasMoveLimit && level.TwoStarMoves > level.MoveLimit)
                errors.Add($"Two-star moves ({level.TwoStarMoves}) cannot exceed the move limit ({level.MoveLimit}).");

            return errors;
        }
    }
}
