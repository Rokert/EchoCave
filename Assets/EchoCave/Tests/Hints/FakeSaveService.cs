using EchoCave.Core;
using EchoCave.Game;

namespace EchoCave.Tests
{
    // An in-memory save with no JSON in it, so the tests of what uses the save run without Unity.
    internal sealed class FakeSaveService : ISaveService
    {
        public PlayerProgress Progress { get; private set; } = new PlayerProgress();
        public bool SoundOn { get; private set; } = true;
        public bool MusicOn { get; private set; } = true;
        public int Hints { get; private set; }
        public int HintWrites;

        public FakeSaveService(int hints)
        {
            Hints = hints;
        }

        public void RecordWin(int levelId, int moves, int stars) => Progress.RecordWin(levelId, stars, moves);

        public void SetHints(int count)
        {
            Hints = count;
            HintWrites++;
        }

        public void SetAudio(AudioChannel channel, bool enabled)
        {
            if (channel == AudioChannel.Sound) SoundOn = enabled;
            else MusicOn = enabled;
        }

        public void Reset() => Progress = new PlayerProgress();
    }
}
