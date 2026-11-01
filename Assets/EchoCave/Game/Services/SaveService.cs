using System;
using EchoCave.Core;

namespace EchoCave.Game
{
    // Loads the save from a store when it is created and writes it back whenever something in it changes.
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveStore _store;

        public PlayerProgress Progress { get; private set; }
        public bool SoundOn { get; private set; } = true;
        public bool MusicOn { get; private set; } = true;

        // True when the store held something this version could not read, so the game started fresh.
        public bool RecoveredFromBadData { get; }

        public SaveService(ISaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));

            SaveContent content = SaveContent.Fresh();
            if (_store.TryRead(out string text))
                RecoveredFromBadData = !SaveSerializer.TryDeserialize(text, out content);

            Progress = content.Progress;
            SoundOn = content.SoundOn;
            MusicOn = content.MusicOn;
        }

        public void RecordWin(int levelId, int moves, int stars)
        {
            if (Progress.RecordWin(levelId, stars, moves)) Persist();
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
            Persist();
        }

        private void Persist()
        {
            _store.Write(SaveSerializer.Serialize(Progress, SoundOn, MusicOn));
        }
    }
}
