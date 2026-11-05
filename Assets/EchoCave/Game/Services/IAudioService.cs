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

        // Makes this track the music. It plays (with a crossfade from the one before) while the music switch is on;
        // with the switch off it is only remembered, and comes back when the switch is turned on.
        void PlayMusic(MusicTrack track);
    }
}
