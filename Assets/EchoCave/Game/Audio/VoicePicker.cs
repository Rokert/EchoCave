using System;
using System.Collections.Generic;

namespace EchoCave.Game
{
    // Chooses which of a fixed set of audio voices plays the next sound: the first idle one at or after the
    // cursor, wrapping round. When every voice is busy it takes the one at the cursor, which is the one that
    // started longest ago, because the cursor moves on after every use.
    public static class VoicePicker
    {
        public static int Pick(IReadOnlyList<bool> isPlaying, int cursor)
        {
            if (isPlaying == null) throw new ArgumentNullException(nameof(isPlaying));
            if (isPlaying.Count == 0) throw new ArgumentException("There must be at least one voice.", nameof(isPlaying));
            if (cursor < 0 || cursor >= isPlaying.Count) throw new ArgumentOutOfRangeException(nameof(cursor), cursor, "Outside the voices.");

            for (int i = 0; i < isPlaying.Count; i++)
            {
                int index = (cursor + i) % isPlaying.Count;
                if (!isPlaying[index]) return index;
            }

            return cursor;
        }
    }
}
