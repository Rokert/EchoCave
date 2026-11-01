using System.Collections.Generic;
using UnityEngine;

namespace EchoCave.Game
{
    // Plays the clip of a cue through a small pool of AudioSources, so sounds can overlap without cutting each
    // other off. Only plays: it decides nothing about when (the audio service and the cue player do).
    public sealed class SoundPlayer : MonoBehaviour, ISoundOutput
    {
        [SerializeField] private SoundBank bank;
        [Tooltip("How many sounds can play at once. Past that, the oldest is cut.")]
        [SerializeField, Min(1)] private int voices = 8;

        private readonly SoundThrottle _throttle = new SoundThrottle();
        private readonly HashSet<SoundCue> _reportedMissing = new HashSet<SoundCue>();
        private AudioSource[] _sources;
        private bool[] _busy;
        private int _cursor;

        private void Awake()
        {
            _sources = new AudioSource[voices];
            _busy = new bool[voices];
            for (int i = 0; i < voices; i++)
            {
                var host = new GameObject("Voice " + (i + 1));
                host.transform.SetParent(transform, false);
                AudioSource source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }
        }

        public void Play(SoundCue cue)
        {
            if (_sources == null) return;

            if (!bank.TryGet(cue, out SoundBank.Sound sound))
            {
                if (_reportedMissing.Add(cue)) Debug.LogWarning($"SoundBank has no clip for {cue}; it will stay silent.", bank);
                return;
            }

            if (!_throttle.TryAllow(cue, Time.unscaledTime, sound.MinIntervalSeconds)) return;

            for (int i = 0; i < _sources.Length; i++) _busy[i] = _sources[i].isPlaying;
            int index = VoicePicker.Pick(_busy, _cursor);
            _cursor = (index + 1) % _sources.Length;

            AudioSource source = _sources[index];
            source.Stop();
            source.clip = sound.Clip;
            source.volume = sound.Volume;
            source.Play();
        }
    }
}
