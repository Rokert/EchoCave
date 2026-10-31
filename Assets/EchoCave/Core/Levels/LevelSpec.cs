using System;

namespace EchoCave.Core
{
    // What a generated level must look like. The generator fills in the bats; the spec says how many, how
    // tangled, and how much room the player gets.
    public sealed class LevelSpec
    {
        public int Width { get; }
        public int Height { get; }
        public int BatCount { get; }
        public int HiddenCount { get; }
        public int PingBudget { get; }

        // Extra taps over the minimum that the move limit allows. Negative means no move limit.
        public int Slack { get; }

        // Floors for the structure: blocking chains at least this deep, at least this many traps.
        public int MinWaves { get; }
        public int MinBlocked { get; }

        // How far the difficulty may rise above the previous level, so the curve climbs in small steps.
        public int MaxDifficultyStep { get; }

        public int TwoStarSlack { get; }

        public LevelSpec(
            int width, int height, int batCount, int hiddenCount, int pingBudget, int slack,
            int minWaves, int minBlocked, int maxDifficultyStep, int twoStarSlack)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Must be positive.");
            if (batCount <= 0 || batCount > width * height)
                throw new ArgumentOutOfRangeException(nameof(batCount), batCount, $"Must fit the {width}x{height} board.");
            if (hiddenCount < 0 || hiddenCount > batCount)
                throw new ArgumentOutOfRangeException(nameof(hiddenCount), hiddenCount, "Cannot be negative or exceed the bat count.");
            if (pingBudget < 0) throw new ArgumentOutOfRangeException(nameof(pingBudget), pingBudget, "Cannot be negative.");
            if (minWaves < 0) throw new ArgumentOutOfRangeException(nameof(minWaves), minWaves, "Cannot be negative.");
            if (minBlocked < 0) throw new ArgumentOutOfRangeException(nameof(minBlocked), minBlocked, "Cannot be negative.");
            if (maxDifficultyStep < 0) throw new ArgumentOutOfRangeException(nameof(maxDifficultyStep), maxDifficultyStep, "Cannot be negative.");
            if (twoStarSlack < 0) throw new ArgumentOutOfRangeException(nameof(twoStarSlack), twoStarSlack, "Cannot be negative.");

            Width = width;
            Height = height;
            BatCount = batCount;
            HiddenCount = hiddenCount;
            PingBudget = pingBudget;
            Slack = slack;
            MinWaves = minWaves;
            MinBlocked = minBlocked;
            MaxDifficultyStep = maxDifficultyStep;
            TwoStarSlack = twoStarSlack;
        }
    }
}
