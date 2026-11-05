using System;
using System.Collections.Generic;
using EchoCave.Core;

namespace EchoCave.Game
{
    // Loads the save from a store when it is created and writes it back whenever something in it changes.
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveStore _store;
        private readonly int _startingHints;
        private readonly Dictionary<string, int> _rewardState = new Dictionary<string, int>();

        public PlayerProgress Progress { get; private set; }
        public bool SoundOn { get; private set; } = true;
        public bool MusicOn { get; private set; } = true;
        public int Hints { get; private set; }
        public int ReserveHints { get; private set; }
        public bool TutorialSeen { get; private set; }
        public ChallengeProgress Challenges { get; private set; }

        // True when the store held something this version could not read, so the game started fresh.
        public bool RecoveredFromBadData { get; }

        // startingHints is what a new player, and a save from before hints existed, begins with.
        public SaveService(ISaveStore store, int startingHints)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            if (startingHints < 0) throw new ArgumentOutOfRangeException(nameof(startingHints), startingHints, "Cannot be negative.");
            _startingHints = startingHints;

            SaveContent content = SaveContent.Fresh(startingHints);
            if (_store.TryRead(out string text))
                RecoveredFromBadData = !SaveSerializer.TryDeserialize(text, startingHints, out content);

            Progress = content.Progress;
            SoundOn = content.SoundOn;
            MusicOn = content.MusicOn;
            Hints = content.Hints;
            ReserveHints = Math.Max(0, content.ReserveHints);
            TutorialSeen = content.TutorialSeen;
            Challenges = content.Challenges;
            foreach (KeyValuePair<string, int> entry in content.RewardState) _rewardState[entry.Key] = entry.Value;
        }

        public void RecordWin(int levelId, int moves, int stars)
        {
            if (Progress.RecordWin(levelId, stars, moves)) Persist();
        }

        public int AddChallengeAttempt(string challengeId)
        {
            int attempts = Challenges.AddAttempt(challengeId);
            Persist();
            return attempts;
        }

        public ChallengeSolveOutcome RecordChallengeSolve(string challengeId, ChallengeResult result)
        {
            ChallengeSolveOutcome outcome = Challenges.RecordSolve(challengeId, result);
            if (outcome.Stored) Persist();
            return outcome;
        }

        public void SetHints(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Cannot be negative.");
            if (Hints == count) return;

            Hints = count;
            Persist();
        }

        public void SetTutorialSeen(bool seen)
        {
            if (TutorialSeen == seen) return;

            TutorialSeen = seen;
            Persist();
        }

        public void SetReserveHints(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Cannot be negative.");
            if (ReserveHints == count) return;

            ReserveHints = count;
            Persist();
        }

        public int GetRewardState(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("A reward state needs a key.", nameof(key));

            return _rewardState.TryGetValue(key, out int value) ? value : 0;
        }

        public void SetRewardState(string key, int value)
        {
            if (GetRewardState(key) == value) return;

            // 0 is what a missing key reads as, so it is not stored.
            if (value == 0) _rewardState.Remove(key);
            else _rewardState[key] = value;
            Persist();
        }

        public void SetAudio(AudioChannel channel, bool enabled)
        {
            if (channel == AudioChannel.Sound)
            {
                if (SoundOn == enabled) return;
                SoundOn = enabled;
            }
            else
            {
                if (MusicOn == enabled) return;
                MusicOn = enabled;
            }

            Persist();
        }

        public void Reset()
        {
            Progress = new PlayerProgress();
            Hints = _startingHints;
            ReserveHints = 0;
            _rewardState.Clear();
            TutorialSeen = false;
            Challenges = new ChallengeProgress();
            Persist();
        }

        private void Persist()
        {
            _store.Write(SaveSerializer.Serialize(Progress, SoundOn, MusicOn, Hints, ReserveHints, _rewardState, TutorialSeen, Challenges));
        }
    }
}
