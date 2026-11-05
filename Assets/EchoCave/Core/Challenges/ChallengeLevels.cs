using System;
using System.Linq;

namespace EchoCave.Core
{
    // The challenge levels, as recipes: a spec and a seed. The generator is deterministic (D-010 is why the levels are
    // not searched for by a solver, see D-036), so a recipe always gives the same level on every machine, and the level
    // asset in the project is only a copy of it that tests keep honest. A new challenge is a new recipe, found with the
    // difficulty metric, never an edit to an old one: its id is what the leaderboard hangs on.
    public static class ChallengeLevels
    {
        public const string Challenge001Id = "challenge-001";

        // Rolled from many seeds on a 9x9 board with 40 hidden bats, keeping the layout where a blind player wastes the
        // most taps (docs/CHALLENGES.md). Do not change it: that is a different challenge.
        public const int Challenge001Seed = 259;

        // A very large cap: the first solvable layout of this seed is found long before it, and the layout does not
        // depend on the cap, only on whether it is reached.
        public const int GenerationAttempts = 100000;

        // 9x9, 40 bats and every one hidden, one ping, no move limit (the ranking is the number of taps, so nobody
        // is thrown out for a mistake), no floor on the structure.
        public static LevelSpec Challenge001Spec => new LevelSpec(9, 9, 40, 40, 1, -1, 0, 0, 100000, 2);

        // True when two levels are the same level: the same board, limits and every bat in the same cell, direction and
        // visibility, in the same order (a bat's id is its position in the list). The stars are not compared: they do not
        // matter in a challenge.
        public static bool SameLevel(LevelDefinition a, LevelDefinition b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));

            if (a.Width != b.Width || a.Height != b.Height) return false;
            if (a.MoveLimit != b.MoveLimit || a.PingBudget != b.PingBudget) return false;

            return a.Bats.Select(x => (x.Cell.X, x.Cell.Y, x.Direction, x.IsHidden))
                .SequenceEqual(b.Bats.Select(x => (x.Cell.X, x.Cell.Y, x.Direction, x.IsHidden)));
        }

        public static LevelDefinition BuildChallenge001(MoveResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            LevelDefinition level = new LevelGenerator(resolver).Generate(Challenge001Spec, Challenge001Seed, 0, GenerationAttempts);
            return level ?? throw new InvalidOperationException($"{Challenge001Id}: the generator found no layout for seed {Challenge001Seed}.");
        }
    }
}
