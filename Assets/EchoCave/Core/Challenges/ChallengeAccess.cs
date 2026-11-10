using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // When each challenge opens: once the player has won a given number of the numbered levels (the challenge's own
    // threshold, kept in the catalog). A threshold higher than the number of levels is capped at it, so a challenge can
    // always be reached by winning every level.
    public static class ChallengeAccess
    {
        // How many of the levels (the stable ids in the catalog) the player has won, with any number of stars.
        public static int LevelsWon(PlayerProgress progress, IReadOnlyList<int> levelIds)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (levelIds == null) throw new ArgumentNullException(nameof(levelIds));

            int won = 0;
            foreach (int id in levelIds)
            {
                if (progress.IsCompleted(id)) won++;
            }

            return won;
        }

        // A game with no levels never opens a challenge.
        public static bool IsUnlocked(PlayerProgress progress, IReadOnlyList<int> levelIds, int levelsToUnlock)
        {
            if (levelsToUnlock < 0) throw new ArgumentOutOfRangeException(nameof(levelsToUnlock), levelsToUnlock, "Cannot be negative.");
            if (levelIds == null) throw new ArgumentNullException(nameof(levelIds));
            if (levelIds.Count == 0) return false;

            return LevelsLeft(progress, levelIds, levelsToUnlock) == 0;
        }

        // How many levels are still to win before the challenge opens, for a message like "Win 3 more levels".
        public static int LevelsLeft(PlayerProgress progress, IReadOnlyList<int> levelIds, int levelsToUnlock)
        {
            if (levelsToUnlock < 0) throw new ArgumentOutOfRangeException(nameof(levelsToUnlock), levelsToUnlock, "Cannot be negative.");

            int needed = levelIds != null ? Math.Min(levelsToUnlock, levelIds.Count) : 0;
            return Math.Max(0, needed - LevelsWon(progress, levelIds));
        }

        // How many of the challenges (one threshold each) are open.
        public static int UnlockedCount(PlayerProgress progress, IReadOnlyList<int> levelIds, IReadOnlyList<int> thresholds)
        {
            if (thresholds == null) throw new ArgumentNullException(nameof(thresholds));

            int open = 0;
            foreach (int threshold in thresholds)
            {
                if (IsUnlocked(progress, levelIds, threshold)) open++;
            }

            return open;
        }
    }
}
