using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // How hard a level is to play perfectly, for picking challenges.
    //
    // The solver cannot say it: with every arrow known, any order that frees a bat each time works, so the minimum is
    // always the number of bats and finding it takes no search (D-010). What makes a level hard is what the player does
    // not know: the arrows of the hidden bats. So this measures the cost of not knowing, by playing the level many times
    // with a player who sees where every bat is but not where the hidden ones point, and who never pings. That player
    // taps a bat they know is free when there is one, otherwise a random bat whose arrow they do not know; a bat that
    // bounces shows which way it points (it flies that way before it comes back), so it is known from then on.
    //
    // Deterministic: the same level, trials and seed always give the same numbers.
    public readonly struct ChallengeDifficulty
    {
        public int Bats { get; }
        public int Hidden { get; }
        public int PingBudget { get; }

        // Depth of the blocking chains (solver waves): how long an arrow has to be remembered after a reveal.
        public int Waves { get; }

        // Average wasted taps of the player described above. Higher is harder to get through without pings.
        public double ExpectedBlindBounces { get; }

        // The share of those runs that wasted no tap at all: the chance of a perfect score by luck. Lower is better.
        public double ChanceOfFlawlessBlindRun { get; }

        public int Trials { get; }

        // One number to order candidates by, not a promise about how it feels: wasted taps weigh most, then the hidden
        // bats to remember at once, then the depth of the chains.
        public double Score => 2.0 * ExpectedBlindBounces + Hidden + Waves;

        private ChallengeDifficulty(int bats, int hidden, int pingBudget, int waves, double expectedBounces, double flawless, int trials)
        {
            Bats = bats;
            Hidden = hidden;
            PingBudget = pingBudget;
            Waves = waves;
            ExpectedBlindBounces = expectedBounces;
            ChanceOfFlawlessBlindRun = flawless;
            Trials = trials;
        }

        // False when the level is invalid or cannot be solved, because then there is nothing to measure.
        public static bool TryMeasure(LevelDefinition level, MoveResolver resolver, int trials, int seed, out ChallengeDifficulty difficulty)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (trials < 1) throw new ArgumentOutOfRangeException(nameof(trials), trials, "At least one trial.");

            difficulty = default;
            if (LevelValidator.Validate(level).Count > 0) return false;

            SolveResult solved = new LevelSolver(resolver).Solve(level);
            if (!solved.IsSolvable) return false;

            int hidden = 0;
            foreach (BatDefinition bat in level.Bats)
            {
                if (bat.IsHidden) hidden++;
            }

            var random = new SeededRandom(seed);
            long totalBounces = 0;
            int flawless = 0;
            for (int i = 0; i < trials; i++)
            {
                int bounces = PlayBlind(level, resolver, random);
                totalBounces += bounces;
                if (bounces == 0) flawless++;
            }

            difficulty = new ChallengeDifficulty(
                level.Bats.Count, hidden, level.PingBudget, solved.Waves.Count,
                (double)totalBounces / trials, (double)flawless / trials, trials);
            return true;
        }

        // Plays one run to the end and returns how many taps bounced.
        private static int PlayBlind(LevelDefinition level, MoveResolver resolver, SeededRandom random)
        {
            Board board = LevelBuilder.Build(level).Board;

            var known = new HashSet<int>();
            foreach (Bat bat in board.Bats)
            {
                if (!bat.IsHidden) known.Add(bat.Id);
            }

            int bounces = 0;
            var unknown = new List<int>();
            while (board.Count > 0)
            {
                int pick = -1;
                foreach (Bat bat in board.Bats)
                {
                    if (known.Contains(bat.Id) && resolver.Resolve(board, bat.Id).Outcome == MoveOutcome.Exited)
                    {
                        pick = bat.Id;
                        break;
                    }
                }

                if (pick < 0)
                {
                    unknown.Clear();
                    foreach (Bat bat in board.Bats)
                    {
                        if (!known.Contains(bat.Id)) unknown.Add(bat.Id);
                    }

                    // A solvable level always has a free bat, and every bat that is not known is on this list.
                    if (unknown.Count == 0) throw new InvalidOperationException("No bat can be tapped, but the level was solvable.");

                    pick = unknown[random.Next(unknown.Count)];
                }

                if (resolver.Resolve(board, pick).Outcome == MoveOutcome.Exited)
                {
                    board.Remove(pick);
                }
                else
                {
                    bounces++;
                    known.Add(pick);
                }
            }

            return bounces;
        }
    }
}
