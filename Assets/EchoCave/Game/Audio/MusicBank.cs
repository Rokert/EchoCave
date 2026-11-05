using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoCave.Game
{
    // Which clip and how loud each track plays, which track the menu uses, the playlist of the play area and how
    // long each track plays before the next. Data, not code: tune it in the Inspector.
    [CreateAssetMenu(fileName = "MusicBank", menuName = "EchoCave/Music Bank")]
    public sealed class MusicBank : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public MusicTrack track;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        public readonly struct Music
        {
            public AudioClip Clip { get; }
            public float Volume { get; }

            public Music(AudioClip clip, float volume)
            {
                Clip = clip;
                Volume = volume;
            }
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        [Header("Flow")]
        [Tooltip("The track that plays in the menu.")]
        [SerializeField] private MusicTrack menuTrack = MusicTrack.Menu;
        [Tooltip("The tracks that play while playing, in order. After 'Seconds Per Track' the next one fades in; the last one is followed by the first.")]
        [SerializeField] private List<MusicTrack> playTracks = new List<MusicTrack> { MusicTrack.Cave, MusicTrack.Bright, MusicTrack.Dream };
        [Tooltip("How long a track plays before the next one of the playlist fades in. 0 means the first one plays for ever.")]
        [SerializeField, Min(0f)] private float secondsPerTrack = 180f;
        [Tooltip("How long the fade between two tracks takes, and the fade when the music is switched off or on.")]
        [SerializeField, Min(0f)] private float crossfadeSeconds = 2.5f;

        private Dictionary<MusicTrack, Music> _lookup;

        public MusicTrack MenuTrack => menuTrack;
        public IReadOnlyList<MusicTrack> PlayTracks => playTracks;
        public float SecondsPerTrack => secondsPerTrack;
        public float CrossfadeSeconds => crossfadeSeconds;

        // False when the track has no entry or its entry has no clip. When a track is listed twice the first one wins.
        public bool TryGet(MusicTrack track, out Music music)
        {
            _lookup ??= BuildLookup();
            return _lookup.TryGetValue(track, out music);
        }

        // One message per problem: a track with no clip, a track listed twice, a clip that would play silently, a
        // flow that names a track with no clip.
        public IReadOnlyList<string> FindProblems()
        {
            var problems = new List<string>();
            var seen = new HashSet<MusicTrack>();

            foreach (Entry entry in entries)
            {
                if (!seen.Add(entry.track)) problems.Add($"{entry.track} is listed more than once; only the first entry is used.");
                else if (entry.clip == null) problems.Add($"{entry.track} has no clip.");
                else if (entry.volume <= 0f) problems.Add($"{entry.track} has volume 0 and will never be heard.");
            }

            foreach (MusicTrack track in Enum.GetValues(typeof(MusicTrack)))
                if (!seen.Contains(track)) problems.Add($"{track} has no entry.");

            if (playTracks.Count == 0) problems.Add("The play playlist is empty: the menu track will play while playing.");
            foreach (MusicTrack track in playTracks)
                if (!TryGet(track, out _)) problems.Add($"The play playlist names {track}, which has no clip.");
            if (!TryGet(menuTrack, out _)) problems.Add($"The menu track {menuTrack} has no clip.");

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

        private Dictionary<MusicTrack, Music> BuildLookup()
        {
            var lookup = new Dictionary<MusicTrack, Music>();
            foreach (Entry entry in entries)
            {
                if (entry.clip == null || lookup.ContainsKey(entry.track)) continue;
                lookup.Add(entry.track, new Music(entry.clip, entry.volume));
            }

            return lookup;
        }
    }
}
