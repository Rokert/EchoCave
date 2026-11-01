namespace EchoCave.Game
{
    // Silent: it only remembers the two switches. The game falls back to it when no sound player is wired.
    public sealed class NullAudioService : IAudioService
    {
        private bool _sound = true;
        private bool _music = true;

        public bool IsEnabled(AudioChannel channel) => channel == AudioChannel.Sound ? _sound : _music;

        public void SetEnabled(AudioChannel channel, bool enabled)
        {
            if (channel == AudioChannel.Sound) _sound = enabled;
            else _music = enabled;
        }

        public void Play(SoundCue cue)
        {
        }
    }
}
