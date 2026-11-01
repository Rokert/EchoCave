using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoCave.Game
{
    // Which clip, how loud and how often each cue plays. Data, not code: tune it in the Inspector.
    [CreateAssetMenu(fileName = "SoundBank", menuName = "EchoCave/Sound Bank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SoundCue cue;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            [Tooltip("The cue plays at most once in this many seconds. 0 means no limit.")]
            [Min(0f)] public float minIntervalSeconds;
        }

        public readonly struct Sound
        {
            public AudioClip Clip { get; }
            public float Volume { get; }
            public float MinIntervalSeconds { get; }

            public Sound(AudioClip clip, float volume, float minIntervalSeconds)
            {
                Clip = clip;
                Volume = volume;
                MinIntervalSeconds = minIntervalSeconds;
            }
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<SoundCue, Sound> _lookup;

        // False when the cue has no entry or its entry has no clip. When a cue is listed twice the first one wins.
        public bool TryGet(SoundCue cue, out Sound sound)
        {
            _lookup ??= BuildLookup();
            return _lookup.TryGetValue(cue, out sound);
        }

        // One message per problem: a cue with no clip, a cue listed twice, a clip that would play silently.
        public IReadOnlyList<string> FindProblems()
        {
            var problems = new List<string>();
            var seen = new HashSet<SoundCue>();

            foreach (Entry entry in entries)
            {
                if (!seen.Add(entry.cue)) problems.Add($"{entry.cue} is listed more than once; only the first entry is used.");
                else if (entry.clip == null) problems.Add($"{entry.cue} has no clip.");
                else if (entry.volume <= 0f) problems.Add($"{entry.cue} has volume 0 and will never be heard.");
            }

            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)))
                if (!seen.Contains(cue)) problems.Add($"{cue} has no entry.");

            return problems;
        }

        internal void SetEntries(IEnumerable<Entry> newEntries)
        {
            entries = new List<Entry>(newEntries);
            _lookup = null;
        }

        private void OnEnable()
        {
            _lookup = null;
        }

        // Editing the list in the Inspector must show up without a restart.
        private void OnValidate()
        {
            _lookup = null;
        }

        private Dictionary<SoundCue, Sound> BuildLookup()
        {
            var lookup = new Dictionary<SoundCue, Sound>();
            foreach (Entry entry in entries)
            {
                if (entry.clip == null || lookup.ContainsKey(entry.cue)) continue;
                lookup.Add(entry.cue, new Sound(entry.clip, entry.volume, entry.minIntervalSeconds));
            }

            return lookup;
        }
    }
}
