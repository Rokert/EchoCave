using System;
using EchoCave.Core;

namespace EchoCave.Game
{
    // Loads the save from a store when it is created and writes it back whenever something in it changes.
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveStore _store;
        private readonly int _startingHints;

        public PlayerProgress Progress { get; private set; }
        public bool SoundOn { get; private set; } = true;
        public bool MusicOn { get; private set; } = true;
        public int Hints { get; private set; }

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
        }

        public void RecordWin(int levelId, int moves, int stars)
        {
            if (Progress.RecordWin(levelId, stars, moves)) Persist();
        }

        public void SetHints(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Cannot be negative.");
            if (Hints == count) return;

            Hints = count;
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
            Persist();
        }

        private void Persist()
        {
            _store.Write(SaveSerializer.Serialize(Progress, SoundOn, MusicOn, Hints));
        }
    }
}
