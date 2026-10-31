using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using UnityEngine;

namespace EchoCave.Editor
{
    // The difficulty curve as data: one entry per level, in play order. The Level Solver window turns each
    // entry into a LevelData asset. Change a seed to roll a different layout for that level.
    [CreateAssetMenu(fileName = "LevelGenerationPlan", menuName = "EchoCave/Level Generation Plan")]
    public sealed class LevelGenerationPlan : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Asset name, also the file name in the levels folder.")]
            public string levelName = "Level_000";

            [Tooltip("Hand-made level: never generated or overwritten, only listed in the catalog.")]
            public bool keepAsIs;

            [Min(1)] public int width = 5;
            [Min(1)] public int height = 5;
            [Min(1)] public int bats = 6;
            [Min(0)] public int hidden;
            [Min(0)] public int pings;

            [Tooltip("Extra taps over the minimum that the move limit allows. Negative means no move limit.")]
            public int slack = -1;

            [Tooltip("Blocking chains at least this deep.")]
            [Min(0)] public int minWaves = 2;

            [Tooltip("At least this many bats blocked on the first move (the traps).")]
            [Min(0)] public int minBlocked = 2;

            [Min(0)] public int twoStarSlack = 2;

            [Tooltip("Change it to roll a different layout for this level.")]
            public int seed = 1;
        }

        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private string folder = "Assets/EchoCave/Data/Levels";

        [Tooltip("How much harder than the previous level a generated level may be, in difficulty points.")]
        [SerializeField, Min(0)] private int maxDifficultyStep = 8;

        [SerializeField, Min(1)] private int maxAttempts = 100000;
        [SerializeField] private List<Entry> entries = DefaultEntries();

        public LevelCatalog Catalog => catalog;
        public string Folder => folder;
        public int MaxAttempts => maxAttempts;
        public IReadOnlyList<Entry> Entries => entries;

        public string PathOf(Entry entry) => $"{folder}/{entry.levelName}.asset";

        public LevelSpec ToSpec(Entry e)
        {
            return new LevelSpec(
                e.width, e.height, e.bats, e.hidden, e.pings, e.slack,
                e.minWaves, e.minBlocked, maxDifficultyStep, e.twoStarSlack);
        }

        // 1-3 no hidden bats and no pings, 4-6 the first hidden bats with generous pings, 7-10 more hidden
        // bats and fewer pings, with the peak at 10. Level_001 is the hand-made tutorial.
        private static List<Entry> DefaultEntries()
        {
            return new List<Entry>
            {
                new Entry { levelName = "Level_001", keepAsIs = true },
                Make("Level_002", 5, 5, 6, 0, 0, -1, 2, 2),
                Make("Level_003", 5, 5, 8, 0, 0, -1, 3, 3),
                Make("Level_004", 5, 5, 9, 1, 3, 4, 3, 4),
                Make("Level_005", 6, 6, 10, 1, 3, 3, 3, 4),
                Make("Level_006", 6, 6, 11, 2, 3, 3, 4, 5),
                Make("Level_007", 6, 6, 12, 3, 2, 3, 4, 5),
                Make("Level_008", 7, 7, 14, 4, 2, 2, 4, 6),
                Make("Level_009", 7, 7, 15, 5, 2, 2, 5, 7),
                Make("Level_010", 7, 7, 16, 6, 1, 1, 5, 8)
            };
        }

        private static Entry Make(string name, int width, int height, int bats, int hidden, int pings, int slack, int minWaves, int minBlocked)
        {
            return new Entry
            {
                levelName = name, width = width, height = height, bats = bats, hidden = hidden,
                pings = pings, slack = slack, minWaves = minWaves, minBlocked = minBlocked
            };
        }
    }
}
