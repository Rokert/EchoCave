using System;
using EchoCave.Core;

namespace EchoCave.Game
{
    // Everything a save file holds: the progress, the two audio switches and the hint count.
    public sealed class SaveContent
    {
        public PlayerProgress Progress { get; }
        public bool SoundOn { get; }
        public bool MusicOn { get; }
        public int Hints { get; }

        public SaveContent(PlayerProgress progress, bool soundOn, bool musicOn, int hints)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            SoundOn = soundOn;
            MusicOn = musicOn;
            Hints = hints;
        }

        // What a player with no save has: no progress, both switches on, the starting hints.
        public static SaveContent Fresh(int startingHints) => new SaveContent(new PlayerProgress(), true, true, startingHints);
    }
}
