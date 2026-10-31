using System;

namespace EchoCave.UI
{
    public enum PingButtonState
    {
        // Pings left and no reveal running.
        Ready,

        // A reveal is running.
        Active,

        // No pings left.
        Empty
    }

    // What the HUD presenter needs from the HUD view, so its tests can use a fake.
    public interface IHudView
    {
        event Action PingPressed;
        event Action PausePressed;

        void SetVisible(bool visible);

        // A moveLimit of 0 means the level has none.
        void SetMoves(int movesUsed, int moveLimit);

        // levelNumber counts from 1: the player reads "level 3 / 10".
        void SetLevel(int levelNumber, int levelCount);

        void SetPings(int pingsLeft);

        void SetPingState(PingButtonState state);

        // While paused the button shows "resume" instead of "pause".
        void SetPaused(bool paused);

        // A short shake of the ping button: "that did nothing".
        void ShakePing();
    }
}
