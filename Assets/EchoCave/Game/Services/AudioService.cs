using System;

namespace EchoCave.Game
{
    // The real audio service: plays cues through the sound output unless the sound switch is off. The music
    // switch is only remembered until there is music.
    public sealed class AudioService : IAudioService
    {
        private readonly ISoundOutput _output;
        private bool _sound = true;
        private bool _music = true;

        public AudioService(ISoundOutput output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public bool IsEnabled(AudioChannel channel) => channel == AudioChannel.Sound ? _sound : _music;

        public void SetEnabled(AudioChannel channel, bool enabled)
        {
            if (channel == AudioChannel.Sound) _sound = enabled;
            else _music = enabled;
        }

        public void Play(SoundCue cue)
        {
            if (_sound) _output.Play(cue);
        }
    }
}
