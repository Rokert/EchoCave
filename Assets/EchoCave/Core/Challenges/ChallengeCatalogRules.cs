using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // What a list of challenges must satisfy before the game starts. A challenge id is what a leaderboard hangs on, so
    // it is restricted to characters that are safe in a file name, a URL and a database key.
    public static class ChallengeCatalogRules
    {
        public const int MaxIdLength = 64;

        // Lowercase letters, digits, '-' and '_', 1 to 64 characters, not starting with '-' or '_'.
        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaxIdLength) return false;
            if (id[0] == '-' || id[0] == '_') return false;

            foreach (char c in id)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!ok) return false;
            }

            return true;
        }

        // One message per problem, naming the challenge by its position; empty when the list is usable.
        public static IReadOnlyList<string> Validate(IReadOnlyList<string> ids, IReadOnlyList<LevelDefinition> levels, MoveResolver resolver)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            if (levels == null) throw new ArgumentNullException(nameof(levels));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            var errors = new List<string>();
            if (ids.Count != levels.Count)
            {
                errors.Add($"There are {ids.Count} ids and {levels.Count} levels.");
                return errors;
            }

            var seen = new HashSet<string>();
            var solver = new LevelSolver(resolver);
            for (int i = 0; i < ids.Count; i++)
            {
                string name = $"Challenge {i + 1} (\"{ids[i]}\")";

                if (!IsValidId(ids[i])) errors.Add($"{name} has an id that is not valid: use 1 to {MaxIdLength} lowercase letters, digits, - or _, not starting with - or _.");
                else if (!seen.Add(ids[i])) errors.Add($"{name} repeats an id.");

                if (levels[i] == null)
                {
                    errors.Add($"{name} has no level.");
                    continue;
                }

                IReadOnlyList<string> levelErrors = LevelValidator.Validate(levels[i]);
                if (levelErrors.Count > 0)
                {
                    errors.Add($"{name} has an invalid level: {string.Join(" ", levelErrors)}");
                    continue;
                }

                SolveResult solved = solver.Solve(levels[i]);
                if (!solved.IsSolvable) errors.Add($"{name} cannot be solved.");
                else if (!solved.FitsMoveLimit) errors.Add($"{name} has a move limit below its minimum of {solved.MinMoves}.");
            }

            return errors;
        }
    }
}
