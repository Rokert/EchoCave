using System;
using EchoCave.Core;

namespace EchoCave.Game
{
    // Everything a save file holds: the progress and the two audio switches.
    public sealed class SaveContent
    {
        public PlayerProgress Progress { get; }
        public bool SoundOn { get; }
        public bool MusicOn { get; }

        public SaveContent(PlayerProgress progress, bool soundOn, bool musicOn)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            SoundOn = soundOn;
            MusicOn = musicOn;
        }

        public static SaveContent Fresh() => new SaveContent(new PlayerProgress(), true, true);
    }
}
