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

        // The hints the player holds. A save from before hints existed starts with the starting count.
        int Hints { get; }

        // Records a win and saves at once when it improved something.
        void RecordWin(int levelId, int moves, int stars);

        // The spare hints kept aside while the stock is full.
        int ReserveHints { get; }

        // Saves the hint count at once when it changed.
        void SetHints(int count);

        // Saves the reserve at once when it changed.
        void SetReserveHints(int count);

        // The saved state of a hint reward, by its key; 0 when it was never saved.
        int GetRewardState(string key);

        // Saves the state of a hint reward at once when it changed.
        void SetRewardState(string key, int value);

        // Saves a switch at once when it changed.
        void SetAudio(AudioChannel channel, bool enabled);

        // Forgets all progress and gives back the starting hints, with no reserve and no reward state. The audio
        // switches are kept: they are a preference, not an achievement.
        void Reset();
    }
}
