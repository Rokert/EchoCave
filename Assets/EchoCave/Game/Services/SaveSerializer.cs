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
        // Version 2 keys levels by their stable id (D-029). Version 1 keyed them by catalog position.
        public const int CurrentVersion = 2;

        // The ten levels of version 1 got ids 1 to 10 in catalog order, so position i was id i + 1.
        private const int LegacyIdOffset = 1;

        [Serializable]
        private sealed class Header
        {
            public int version;
        }

        [Serializable]
        private sealed class SaveFile
        {
            public int version;
            public List<LevelEntry> levels = new List<LevelEntry>();
        }

        [Serializable]
        private sealed class LevelEntry
        {
            public int id;
            public int stars;
            public int moves;
        }

        [Serializable]
        private sealed class LegacySaveFile
        {
            public int version;
            public List<LegacyLevelEntry> levels = new List<LegacyLevelEntry>();
        }

        [Serializable]
        private sealed class LegacyLevelEntry
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
                file.levels.Add(new LevelEntry { id = record.LevelId, stars = record.Stars, moves = record.BestMoves });

            return JsonUtility.ToJson(file);
        }

        // False when the text is not a save this version understands. Entries that make no sense are skipped.
        // A version 1 save is read and converted.
        public static bool TryDeserialize(string json, out PlayerProgress progress)
        {
            progress = new PlayerProgress();
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                Header header = JsonUtility.FromJson<Header>(json);
                if (header == null) return false;

                if (header.version == CurrentVersion) return ReadCurrent(json, progress);
                if (header.version == 1) return ReadLegacy(json, progress);
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool ReadCurrent(string json, PlayerProgress progress)
        {
            SaveFile file = JsonUtility.FromJson<SaveFile>(json);
            if (file == null || file.levels == null) return false;

            foreach (LevelEntry entry in file.levels)
            {
                if (entry == null) continue;
                Add(progress, new LevelRecord(entry.id, entry.stars, entry.moves));
            }

            return true;
        }

        private static bool ReadLegacy(string json, PlayerProgress progress)
        {
            LegacySaveFile file = JsonUtility.FromJson<LegacySaveFile>(json);
            if (file == null || file.levels == null) return false;

            foreach (LegacyLevelEntry entry in file.levels)
            {
                if (entry == null) continue;
                Add(progress, new LevelRecord(entry.index + LegacyIdOffset, entry.stars, entry.moves));
            }

            return true;
        }

        private static void Add(PlayerProgress progress, LevelRecord record)
        {
            if (record.IsValid) progress.RecordWin(record.LevelId, record.Stars, record.BestMoves);
        }
    }
}
