using System;
using System.Globalization;

namespace EchoCave.UI
{
    // The words and numbers the challenge screens show, apart from the views so they can be tested.
    public static class ChallengeText
    {
        // "2:20.5": minutes, seconds and tenths. Never negative.
        public static string Time(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f) seconds = 0f;

            long tenths = (long)Math.Round(seconds * 10.0, MidpointRounding.AwayFromZero);
            long minutes = tenths / 600;
            long rest = tenths % 600;
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}.{2}", minutes, rest / 10, rest % 10);
        }

        public static string Taps(int count) => count == 1 ? "1 tap" : count + " taps";

        public static string Tries(int count) => count == 1 ? "1 try" : count + " tries";

        // "1 ping used", "No pings used".
        public static string PingsUsed(int count)
        {
            if (count <= 0) return "No pings used";
            return count == 1 ? "1 ping used" : count + " pings used";
        }

        // "52 taps in 2:20.5".
        public static string Result(int moves, float seconds) => Taps(moves) + " in " + Time(seconds);

        public static string Rank(int rank) => "Rank " + rank + " on this device";

        // What a win says when it opens challenges: "A challenge is open! Find it in the menu."
        public static string UnlockedNote(int opened)
        {
            return opened == 1
                ? "New challenge unlocked! Find it in the menu."
                : opened + " new challenges unlocked! Find them in the menu.";
        }

        public static string LockedMessage(int levelsLeft)
        {
            return levelsLeft == 1 ? "Win 1 more level to unlock" : "Win " + levelsLeft + " more levels to unlock";
        }
    }
}
