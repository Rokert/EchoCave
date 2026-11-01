namespace EchoCave.Game
{
    // No sound system yet: it only remembers the two switches.
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
    }
}
