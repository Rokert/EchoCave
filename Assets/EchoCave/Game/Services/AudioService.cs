using System;

namespace EchoCave.Game
{
    // The real audio service: plays cues through the sound output unless the sound switch is off, and keeps the music
    // going through the music output unless the music switch is off. The music output is optional: without it the
    // game has no music.
    public sealed class AudioService : IAudioService
    {
        private readonly ISoundOutput _output;
        private readonly IMusicOutput _musicOutput;
        private bool _sound = true;
        private bool _music = true;
        private MusicTrack _wanted;
        private bool _hasWanted;

        public AudioService(ISoundOutput output, IMusicOutput musicOutput = null)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _musicOutput = musicOutput;
        }

        public bool IsEnabled(AudioChannel channel) => channel == AudioChannel.Sound ? _sound : _music;

        public void SetEnabled(AudioChannel channel, bool enabled)
        {
            if (channel == AudioChannel.Sound)
            {
                _sound = enabled;
                return;
            }

            if (_music == enabled) return;
            _music = enabled;

            if (_musicOutput == null) return;
            if (!enabled) _musicOutput.Stop();
            else if (_hasWanted) _musicOutput.Play(_wanted);
        }

        public void Play(SoundCue cue)
        {
            if (_sound) _output.Play(cue);
        }

        public void PlayMusic(MusicTrack track)
        {
            _wanted = track;
            _hasWanted = true;
            if (_music && _musicOutput != null) _musicOutput.Play(track);
        }
    }
}
