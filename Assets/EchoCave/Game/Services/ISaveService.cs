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

        // True once the player has finished or skipped the tutorial. A save from before the tutorial reads as false, so
        // players who already have a save see it once too, and can skip it.
        bool TutorialSeen { get; }

        // Saves the tutorial flag at once when it changed.
        void SetTutorialSeen(bool seen);

        // The tries and the best results of every challenge, which is also the local leaderboard.
        ChallengeProgress Challenges { get; }

        // One more try at a challenge, saved at once. Returns the new total.
        int AddChallengeAttempt(string challengeId);

        // Keeps a solved challenge if it ranks among the best results, and saves at once when it did.
        ChallengeSolveOutcome RecordChallengeSolve(string challengeId, ChallengeResult result);

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

        // Forgets all progress, challenge results included, and gives back the starting hints, with no reserve, no reward state and the tutorial unseen. The audio
        // switches are kept: they are a preference, not an achievement.
        void Reset();
    }
}
