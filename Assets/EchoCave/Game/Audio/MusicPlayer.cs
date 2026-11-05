using UnityEngine;

namespace EchoCave.Game
{
    // Plays the music through two looping AudioSources and crossfades between them. Only plays: which track and when
    // are decided by the audio service and the music conductor.
    public sealed class MusicPlayer : MonoBehaviour, IMusicOutput
    {
        [SerializeField] private MusicBank bank;

        private AudioSource[] _sources;
        private float[] _targets;
        private int _active;
        private bool _hasTrack;
        private MusicTrack _current;
        private bool _reportedMissing;

        // The bank, so the composition root can read the playlist from the same asset the clips come from.
        public MusicBank Bank => bank;

        // Created on first use, not in Awake: the composition root may ask for music before this object has woken up.
        private void EnsureSources()
        {
            if (_sources != null) return;

            _sources = new AudioSource[2];
            _targets = new float[2];
            for (int i = 0; i < 2; i++)
            {
                var host = new GameObject("Music " + (i + 1));
                host.transform.SetParent(transform, false);
                AudioSource source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.volume = 0f;
                _sources[i] = source;
            }
        }

        public void Play(MusicTrack track)
        {
            if (bank == null) return;
            EnsureSources();

            if (_hasTrack && _current == track) return;

            if (!bank.TryGet(track, out MusicBank.Music music))
            {
                if (!_reportedMissing)
                {
                    _reportedMissing = true;
                    Debug.LogWarning($"MusicBank has no clip for {track}; the music will stay silent.", bank);
                }

                Stop();
                return;
            }

            // The old track fades out on its source; the new one fades in on the other.
            _targets[_active] = 0f;
            _active = 1 - _active;
            AudioSource source = _sources[_active];
            source.Stop();
            source.clip = music.Clip;
            source.volume = 0f;
            source.Play();
            _targets[_active] = music.Volume;

            _current = track;
            _hasTrack = true;
        }

        public void Stop()
        {
            if (_sources == null) return;

            _targets[0] = 0f;
            _targets[1] = 0f;
            _hasTrack = false;
        }

        private void Update()
        {
            if (_sources == null) return;

            float fade = bank != null ? bank.CrossfadeSeconds : 0f;
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = _sources[i];
                if (!source.isPlaying) continue;

                source.volume = MusicFade.Step(source.volume, _targets[i], Time.unscaledDeltaTime, fade);
                if (_targets[i] <= 0f && source.volume <= 0f) source.Stop();
            }
        }
    }
}
