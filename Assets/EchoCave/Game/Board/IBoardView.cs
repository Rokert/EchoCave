using System;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // What the presenter needs from the board view, so tests can drive it with a fake.
    public interface IBoardView
    {
        // Replaces whatever is on screen with the bats of this board.
        void Build(Board board);

        // Converts a world point to a board cell. False when the point is off the board.
        bool TryGetCell(Vector2 world, out Cell cell);

        // Animates one move the core already resolved. onComplete runs when the animation ends.
        void Play(MoveResult result, Action onComplete);
    }
}
