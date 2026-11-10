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

        // challenge-002: the hardest roll of a denser board, 48 bats on 9x9 with one ping and nothing to spare.
        public const string Challenge002Id = "challenge-002";
        public const int Challenge002Seed = 175;
        public static LevelSpec Challenge002Spec => new LevelSpec(9, 9, 48, 48, 1, -1, 0, 0, 100000, 2);

        // challenge-003: a smaller 8x8 board with 36 hidden bats and no ping at all, so the only way to learn an arrow
        // is to spend a tap on a bounce.
        public const string Challenge003Id = "challenge-003";
        public const int Challenge003Seed = 11;
        public static LevelSpec Challenge003Spec => new LevelSpec(8, 8, 36, 36, 0, -1, 0, 0, 100000, 2);

        // Every challenge there is, in the order the catalog lists them.
        public static readonly string[] AllIds = { Challenge001Id, Challenge002Id, Challenge003Id };

        // How many numbered levels a player wins before each challenge opens: the first early, the others as a reward for
        // getting further. Only what the editor menu writes into a new catalog entry; the catalog asset is what the game reads.
        public static int DefaultLevelsToUnlock(string challengeId)
        {
            switch (challengeId)
            {
                case Challenge001Id: return 1;
                case Challenge002Id: return 5;
                case Challenge003Id: return 10;
                default: throw new ArgumentException($"Unknown challenge id \"{challengeId}\".", nameof(challengeId));
            }
        }

        public static LevelSpec SpecOf(string challengeId)
        {
            switch (challengeId)
            {
                case Challenge001Id: return Challenge001Spec;
                case Challenge002Id: return Challenge002Spec;
                case Challenge003Id: return Challenge003Spec;
                default: throw new ArgumentException($"Unknown challenge id \"{challengeId}\".", nameof(challengeId));
            }
        }

        public static int SeedOf(string challengeId)
        {
            switch (challengeId)
            {
                case Challenge001Id: return Challenge001Seed;
                case Challenge002Id: return Challenge002Seed;
                case Challenge003Id: return Challenge003Seed;
                default: throw new ArgumentException($"Unknown challenge id \"{challengeId}\".", nameof(challengeId));
            }
        }

        public static LevelDefinition Build(string challengeId, MoveResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            int seed = SeedOf(challengeId);
            LevelDefinition level = new LevelGenerator(resolver).Generate(SpecOf(challengeId), seed, 0, GenerationAttempts);
            return level ?? throw new InvalidOperationException($"{challengeId}: the generator found no layout for seed {seed}.");
        }

        public static LevelDefinition BuildChallenge001(MoveResolver resolver) => Build(Challenge001Id, resolver);

        public static LevelDefinition BuildChallenge002(MoveResolver resolver) => Build(Challenge002Id, resolver);

        public static LevelDefinition BuildChallenge003(MoveResolver resolver) => Build(Challenge003Id, resolver);
    }
}
