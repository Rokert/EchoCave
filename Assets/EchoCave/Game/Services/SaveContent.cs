using System;
using System.Collections.Generic;
using EchoCave.Core;

namespace EchoCave.Game
{
    // Everything a save file holds: the progress, the two audio switches, the hint count, the reserve hints and the
    // state of each hint reward (a streak, the last day a gift was given), by the reward's key, and the challenge results.
    public sealed class SaveContent
    {
        private static readonly IReadOnlyDictionary<string, int> NoRewardState = new Dictionary<string, int>();

        public PlayerProgress Progress { get; }
        public bool SoundOn { get; }
        public bool MusicOn { get; }
        public int Hints { get; }
        public int ReserveHints { get; }
        public IReadOnlyDictionary<string, int> RewardState { get; }
        public bool TutorialSeen { get; }
        public ChallengeProgress Challenges { get; }

        public SaveContent(
            PlayerProgress progress, bool soundOn, bool musicOn, int hints, int reserveHints = 0,
            IReadOnlyDictionary<string, int> rewardState = null, bool tutorialSeen = false, ChallengeProgress challenges = null)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            SoundOn = soundOn;
            MusicOn = musicOn;
            Hints = hints;
            ReserveHints = reserveHints;
            RewardState = rewardState ?? NoRewardState;
            TutorialSeen = tutorialSeen;
            Challenges = challenges ?? new ChallengeProgress();
        }

        // What a player with no save has: no progress, both switches on, the starting hints, no reserve, no reward state, the tutorial unseen.
        public static SaveContent Fresh(int startingHints) => new SaveContent(new PlayerProgress(), true, true, startingHints);
    }
}
