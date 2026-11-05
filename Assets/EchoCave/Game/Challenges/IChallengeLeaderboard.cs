using System;
using System.Collections.Generic;
using EchoCave.Core;
using Rokert.Core;

namespace EchoCave.Game
{
    // The best results on a challenge, best first. An edge service: the local one reads this device's save, and the
    // online one (192-F) will ask a server and fall back to the local one when offline.
    public interface IChallengeLeaderboard : IService
    {
        // At most count results, best first. Empty when the challenge has none.
        IReadOnlyList<ChallengeResult> Top(string challengeId, int count);
    }

    // The results of this device, kept in the save.
    public sealed class LocalChallengeLeaderboard : IChallengeLeaderboard
    {
        private readonly ISaveService _save;

        public LocalChallengeLeaderboard(ISaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public IReadOnlyList<ChallengeResult> Top(string challengeId, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Cannot be negative.");

            IReadOnlyList<ChallengeResult> all = _save.Challenges.Get(challengeId).Results;
            var top = new List<ChallengeResult>(Math.Min(count, all.Count));
            for (int i = 0; i < all.Count && i < count; i++) top.Add(all[i]);
            return top;
        }
    }

    // No results at all: what the game falls back to when nothing else is wired.
    public sealed class NullChallengeLeaderboard : IChallengeLeaderboard
    {
        public IReadOnlyList<ChallengeResult> Top(string challengeId, int count) => new ChallengeResult[0];
    }
}
