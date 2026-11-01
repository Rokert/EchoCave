using System;
using System.Collections.Generic;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // What the presenter and the game flow need from the board view, so tests can drive them with a fake.
    public interface IBoardView
    {
        // Replaces whatever is on screen with the bats of this board. Every bat starts unrevealed.
        void Build(Board board);

        // Removes every bat from the screen.
        void Clear();

        // Converts a world point to a board cell. False when the point is off the board.
        bool TryGetCell(Vector2 world, out Cell cell);

        // Animates one move the core already resolved. onComplete runs when the animation ends.
        void Play(MoveResult result, Action onComplete);

        // Shows exactly these bats as revealed; every other hidden bat goes dark again.
        void SetRevealed(IReadOnlyCollection<int> batIds);

        // A ping was accepted: draws the wave and reveals these bats as it reaches them. Bats that are not listed
        // go dark again.
        void ShowPing(IReadOnlyCollection<int> batIds);

        // Tells the player that what they just asked for was refused.
        void ShowRejected();
    }
}
