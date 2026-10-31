using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // Builds a level that matches a spec by trying random layouts from a seed until one is solvable and
    // meets the spec's structure. The same spec, seed and floor always give the same level (no System.Random,
    // no clock). Used by the editor tooling; nothing in a build calls it.
    public sealed class LevelGenerator
    {
        private readonly MoveResolver _resolver;

        public LevelGenerator(MoveResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        // minDifficulty is the previous level's difficulty: the result is at least that hard and at most
        // spec.MaxDifficultyStep harder. Returns null when no layout fit within maxAttempts.
        public LevelDefinition Generate(LevelSpec spec, int seed, int minDifficulty, int maxAttempts)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "Must be positive.");

            var random = new SeededRandom(seed);
            int maxDifficulty = minDifficulty + spec.MaxDifficultyStep;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                IReadOnlyList<BatDefinition> bats = RandomBats(spec, random);

                // Placeholder limit and stars: they only let the measurement run, the real ones come below.
                var draft = new LevelDefinition(spec.Width, spec.Height, bats, 0, spec.PingBudget, 1, 1);
                if (!LevelMetrics.TryMeasure(draft, _resolver, out LevelMetrics metrics)) continue;
                if (metrics.Waves < spec.MinWaves || metrics.BlockedAtStart < spec.MinBlocked) continue;
                if (metrics.Difficulty < minDifficulty || metrics.Difficulty > maxDifficulty) continue;

                int moveLimit = spec.Slack < 0 ? 0 : metrics.MinMoves + spec.Slack;
                StarThresholds stars = StarThresholds.Suggest(metrics.MinMoves, moveLimit, spec.TwoStarSlack);
                return new LevelDefinition(
                    spec.Width, spec.Height, bats, moveLimit, spec.PingBudget, stars.ThreeStarMoves, stars.TwoStarMoves);
            }

            return null;
        }

        private static IReadOnlyList<BatDefinition> RandomBats(LevelSpec spec, SeededRandom random)
        {
            int cellCount = spec.Width * spec.Height;
            var cells = new int[cellCount];
            for (int i = 0; i < cellCount; i++) cells[i] = i;

            // Partial Fisher-Yates: the first BatCount entries are distinct random cells.
            for (int i = 0; i < spec.BatCount; i++)
            {
                int j = i + random.Next(cellCount - i);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            var hidden = new bool[spec.BatCount];
            var order = new int[spec.BatCount];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = 0; i < spec.HiddenCount; i++)
            {
                int j = i + random.Next(order.Length - i);
                (order[i], order[j]) = (order[j], order[i]);
                hidden[order[i]] = true;
            }

            var bats = new List<BatDefinition>(spec.BatCount);
            for (int i = 0; i < spec.BatCount; i++)
            {
                var cell = new Cell(cells[i] % spec.Width, cells[i] / spec.Width);
                var direction = (Direction)random.Next(4);
                bats.Add(new BatDefinition(cell, direction, hidden[i]));
            }

            return bats;
        }
    }
}
