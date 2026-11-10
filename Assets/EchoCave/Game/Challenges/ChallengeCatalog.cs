using System;
using System.Collections.Generic;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // The challenges, in the order the menu lists them. A challenge is never removed or reordered under its id: the
    // id is what its results hang on, the position is only how it is shown.
    [CreateAssetMenu(fileName = "ChallengeCatalog", menuName = "EchoCave/Challenge Catalog")]
    public sealed class ChallengeCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("1 to 64 lowercase letters, digits, - or _. Never changed once players have results on it.")]
            public string challengeId;
            public LevelData level;

            [Tooltip("How many numbered levels the player must win before this challenge opens. 0 opens it from the start.")]
            [Min(0)] public int levelsToUnlock;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        [Tooltip("The shortest time a person needs to tap one bat. A result faster than this many seconds per tap is refused as impossible. 0 turns the check off.")]
        [SerializeField, Min(0f)] private float minSecondsPerTap = 0.15f;

        public float MinSecondsPerTap => minSecondsPerTap;
        public int Count => entries.Count;

        // One message per problem; empty when the catalog is usable.
        public IReadOnlyList<string> FindProblems()
        {
            var ids = new List<string>(entries.Count);
            var levels = new List<LevelDefinition>(entries.Count);
            foreach (Entry entry in entries)
            {
                ids.Add(entry.challengeId);
                levels.Add(entry.level != null ? entry.level.ToDefinition() : null);
            }

            var problems = new List<string>(ChallengeCatalogRules.Validate(ids, levels, new MoveResolver()));
            foreach (Entry entry in entries)
            {
                if (entry.levelsToUnlock < 0) problems.Add($"\"{entry.challengeId}\": levelsToUnlock is {entry.levelsToUnlock}; it cannot be negative.");
            }

            return problems;
        }

        // Throws, listing every problem, when the catalog is not usable: a bad challenge should stop the game at startup.
        public IReadOnlyList<ChallengeEntry> ToEntries()
        {
            IReadOnlyList<string> problems = FindProblems();
            if (problems.Count > 0)
                throw new InvalidOperationException("The challenge catalog has problems:\n" + string.Join("\n", problems));

            var result = new List<ChallengeEntry>(entries.Count);
            foreach (Entry entry in entries) result.Add(new ChallengeEntry(entry.challengeId, entry.level.ToDefinition(), entry.levelsToUnlock));
            return result;
        }

        internal bool Has(string id)
        {
            foreach (Entry entry in entries)
                if (entry.challengeId == id) return true;
            return false;
        }

        // Gives every entry that still has a threshold of 0 the default for its id (an entry saved before the threshold
        // existed reads 0). Returns how many it changed.
        internal int FillZeroThresholds(Func<string, int> defaultOf)
        {
            int changed = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.levelsToUnlock != 0) continue;

                int value = defaultOf(entry.challengeId);
                if (value == 0) continue;

                entry.levelsToUnlock = value;
                entries[i] = entry;
                changed++;
            }

            return changed;
        }

        // Appends at the end: an existing challenge is never moved.
        internal void Append(Entry entry)
        {
            entries.Add(entry);
        }

        internal void SetEntries(IEnumerable<Entry> newEntries)
        {
            entries = new List<Entry>(newEntries);
        }
    }
}
