using System;
using System.Collections.Generic;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Turns progress into JSON and back. Reading is forgiving: a save the game cannot understand is reported,
    // never thrown, because a broken save must not stop the game from starting.
    public static class SaveSerializer
    {
        public const int CurrentVersion = 1;

        [Serializable]
        private sealed class SaveFile
        {
            public int version;
            public List<LevelEntry> levels = new List<LevelEntry>();
        }

        [Serializable]
        private sealed class LevelEntry
        {
            public int index;
            public int stars;
            public int moves;
        }

        public static string Serialize(PlayerProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            var file = new SaveFile { version = CurrentVersion };
            foreach (LevelRecord record in progress.Records)
                file.levels.Add(new LevelEntry { index = record.Index, stars = record.Stars, moves = record.BestMoves });

            return JsonUtility.ToJson(file);
        }

        // False when the text is not a save this version understands. Entries that make no sense are skipped.
        public static bool TryDeserialize(string json, out PlayerProgress progress)
        {
            progress = new PlayerProgress();
            if (string.IsNullOrEmpty(json)) return false;

            SaveFile file;
            try
            {
                file = JsonUtility.FromJson<SaveFile>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (file == null || file.version != CurrentVersion || file.levels == null) return false;

            foreach (LevelEntry entry in file.levels)
            {
                if (entry == null) continue;

                var record = new LevelRecord(entry.index, entry.stars, entry.moves);
                if (record.IsValid) progress.RecordWin(record.Index, record.Stars, record.BestMoves);
            }

            return true;
        }
    }
}
