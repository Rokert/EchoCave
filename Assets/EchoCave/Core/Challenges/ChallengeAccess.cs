using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // When the challenges open: once the player has won every numbered level. A level that is added later closes the
    // challenges again until it is won, which is what "finished the game" means.
    public static class ChallengeAccess
    {
        // levelIds are the stable ids of the levels in the catalog. A game with no levels never unlocks them.
        public static bool IsUnlocked(PlayerProgress progress, IReadOnlyList<int> levelIds)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (levelIds == null) throw new ArgumentNullException(nameof(levelIds));
            if (levelIds.Count == 0) return false;

            foreach (int id in levelIds)
            {
                if (!progress.IsCompleted(id)) return false;
            }

            return true;
        }

        // How many levels are still to win, for a message like "Win 3 more levels".
        public static int LevelsLeft(PlayerProgress progress, IReadOnlyList<int> levelIds)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (levelIds == null) throw new ArgumentNullException(nameof(levelIds));

            int left = 0;
            foreach (int id in levelIds)
            {
                if (!progress.IsCompleted(id)) left++;
            }

            return left;
        }
    }
}
