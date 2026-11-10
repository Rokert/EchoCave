using System;
using EchoCave.Core;

namespace EchoCave.Game
{
    // A challenge the game can play: its id (what the leaderboard hangs on), its level, and how many numbered levels the
    // player must have won before it opens (0: open from the start).
    public readonly struct ChallengeEntry
    {
        public string Id { get; }
        public LevelDefinition Level { get; }
        public int LevelsToUnlock { get; }

        public ChallengeEntry(string id, LevelDefinition level, int levelsToUnlock = 0)
        {
            if (levelsToUnlock < 0) throw new ArgumentOutOfRangeException(nameof(levelsToUnlock), levelsToUnlock, "Cannot be negative.");

            Id = id ?? throw new ArgumentNullException(nameof(id));
            Level = level ?? throw new ArgumentNullException(nameof(level));
            LevelsToUnlock = levelsToUnlock;
        }
    }
}
