using Rokert.Core;

namespace EchoCave.Game
{
    // What the game asks of the sound system.
    public interface IAudioService : IService
    {
        bool IsEnabled(AudioChannel channel);

        void SetEnabled(AudioChannel channel, bool enabled);

        // Plays the sound of a cue, unless the sound switch is off.
        void Play(SoundCue cue);
    }
}
