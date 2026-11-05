using System;
using EchoCave.Core;

namespace EchoCave.Game
{
    // A challenge the game can play: its id (what the leaderboard hangs on) and its level.
    public readonly struct ChallengeEntry
    {
        public string Id { get; }
        public LevelDefinition Level { get; }

        public ChallengeEntry(string id, LevelDefinition level)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Level = level ?? throw new ArgumentNullException(nameof(level));
        }
    }
}
