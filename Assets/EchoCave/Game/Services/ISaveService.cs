using EchoCave.Core;
using Rokert.Core;

namespace EchoCave.Game
{
    // The player's saved progress and settings. Edge service: only the composition root and other edges ask for it.
    public interface ISaveService : IService
    {
        PlayerProgress Progress { get; }

        // The saved sound and music switches. Both are on until the player turns one off.
        bool SoundOn { get; }
        bool MusicOn { get; }

        // Records a win and saves at once when it improved something.
        void RecordWin(int levelId, int moves, int stars);

        // Saves a switch at once when it changed.
        void SetAudio(AudioChannel channel, bool enabled);

        // Forgets all progress. The audio switches are kept: they are a preference, not an achievement.
        void Reset();
    }
}
