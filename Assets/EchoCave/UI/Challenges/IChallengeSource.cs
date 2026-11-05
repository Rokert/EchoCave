using System.Collections.Generic;
using EchoCave.Core;

namespace EchoCave.UI
{
    // What the challenge list needs from the game, so its tests can use a fake. UiRoot adapts GameRoot to it.
    public interface IChallengeSource
    {
        int Count { get; }

        string IdOf(int index);

        // Whether the player has won every numbered level.
        bool Unlocked { get; }

        // How many numbered levels are still to win.
        int LevelsLeft { get; }

        int AttemptsOf(int index);

        // The best results of the challenge, best first, at most count.
        IReadOnlyList<ChallengeResult> TopOf(int index, int count);

        void Play(int index);
    }
}
