using System;
using EchoCave.Core;

namespace EchoCave.Game
{
    // Loads the progress from a store when it is created and writes it back whenever it improves.
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveStore _store;

        public PlayerProgress Progress { get; private set; }

        // True when the store held something this version could not read, so the game started fresh.
        public bool RecoveredFromBadData { get; }

        public SaveService(ISaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));

            if (_store.TryRead(out string content))
            {
                bool understood = SaveSerializer.TryDeserialize(content, out PlayerProgress loaded);
                Progress = loaded;
                RecoveredFromBadData = !understood;
            }
            else
            {
                Progress = new PlayerProgress();
            }
        }

        public void RecordWin(int levelId, int moves, int stars)
        {
            if (Progress.RecordWin(levelId, stars, moves)) _store.Write(SaveSerializer.Serialize(Progress));
        }

        public void Reset()
        {
            Progress = new PlayerProgress();
            _store.Delete();
        }
    }
}
