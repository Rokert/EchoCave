using Rokert.Core;

namespace EchoCave.Game
{
    // What the game asks of the sound system. Cues arrive with the first real sounds (ROK-173).
    public interface IAudioService : IService
    {
        bool IsEnabled(AudioChannel channel);

        void SetEnabled(AudioChannel channel, bool enabled);
    }
}
