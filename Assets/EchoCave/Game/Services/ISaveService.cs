using EchoCave.Core;
using Rokert.Core;

namespace EchoCave.Game
{
    // The player's saved progress. Edge service: only the composition root and other edges ask for it.
    public interface ISaveService : IService
    {
        PlayerProgress Progress { get; }

        // Records a win and saves at once when it improved something.
        void RecordWin(int levelIndex, int moves, int stars);

        // Forgets all progress and deletes the save.
        void Reset();
    }
}
