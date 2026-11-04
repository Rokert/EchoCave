using System;
using System.Collections.Generic;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Turns a save into JSON and back. Reading is forgiving: a save the game cannot understand is reported,
    // never thrown, because a broken save must not stop the game from starting.
    public static class SaveSerializer
    {
        // Version 4 adds the hint count. Version 3 added the sound and music switches, version 2 keyed levels by their
        // stable id (D-029) and version 1 by catalog position.
        public const int CurrentVersion = 4;

        // The ten levels of version 1 got ids 1 to 10 in catalog order, so position i was id i + 1.
        private const int LegacyIdOffset = 1;

        [Serializable]
        private sealed class Header
        {
            public int version;
        }

        // The switches are stored as "off" flags: JsonUtility reads a missing field as false, so a version 2 save,
        // which has neither, comes out with both switches on, which is what it always meant.
        [Serializable]
        private sealed class SaveFile
        {
            public int version;
            public bool soundOff;
            public bool musicOff;
            public int hints;
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

        public static string Serialize(PlayerProgress progress, bool soundOn, bool musicOn, int hints)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            var file = new SaveFile { version = CurrentVersion, soundOff = !soundOn, musicOff = !musicOn, hints = hints };
            foreach (LevelRecord record in progress.Records)
                file.levels.Add(new LevelEntry { id = record.LevelId, stars = record.Stars, moves = record.BestMoves });

            return JsonUtility.ToJson(file);
        }

        // False when the text is not a save this version understands; content is then a fresh save. Entries that
        // make no sense are skipped. Versions 1 to 3 are read and converted; a save from before the hints existed
        // gets startingHints.
        public static bool TryDeserialize(string json, int startingHints, out SaveContent content)
        {
            content = SaveContent.Fresh(startingHints);
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                Header header = JsonUtility.FromJson<Header>(json);
                if (header == null) return false;

                if (header.version >= 2 && header.version <= CurrentVersion) return ReadCurrent(json, header.version, startingHints, out content);
                if (header.version == 1) return ReadLegacy(json, startingHints, out content);
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool ReadCurrent(string json, int version, int startingHints, out SaveContent content)
        {
            content = SaveContent.Fresh(startingHints);
            SaveFile file = JsonUtility.FromJson<SaveFile>(json);
            if (file == null || file.levels == null) return false;

            var progress = new PlayerProgress();
            foreach (LevelEntry entry in file.levels)
            {
                if (entry == null) continue;
                Add(progress, new LevelRecord(entry.id, entry.stars, entry.moves));
            }

            int hints = version >= 4 ? Math.Max(0, file.hints) : startingHints;
            content = new SaveContent(progress, !file.soundOff, !file.musicOff, hints);
            return true;
        }

        private static bool ReadLegacy(string json, int startingHints, out SaveContent content)
        {
            content = SaveContent.Fresh(startingHints);
            LegacySaveFile file = JsonUtility.FromJson<LegacySaveFile>(json);
            if (file == null || file.levels == null) return false;

            var progress = new PlayerProgress();
            foreach (LegacyLevelEntry entry in file.levels)
            {
                if (entry == null) continue;
                Add(progress, new LevelRecord(entry.index + LegacyIdOffset, entry.stars, entry.moves));
            }

            content = new SaveContent(progress, true, true, startingHints);
            return true;
        }

        private static void Add(PlayerProgress progress, LevelRecord record)
        {
            if (record.IsValid) progress.RecordWin(record.LevelId, record.Stars, record.BestMoves);
        }
    }
}
