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

    public enum HintButtonState
    {
        // At least one hint left.
        Ready,

        // No hints left.
        Empty,

        // No hints left, but a rewarded ad will give one: the button offers "AD" instead of a count.
        WatchAd
    }

    // What the HUD presenter needs from the HUD view, so its tests can use a fake.
    public interface IHudView
    {
        event Action PingPressed;
        event Action HintPressed;
        event Action PausePressed;

        void SetVisible(bool visible);

        // A moveLimit of 0 means the level has none.
        void SetMoves(int movesUsed, int moveLimit);

        // levelNumber counts from 1: the player reads "level 3 / 10".
        void SetLevel(int levelNumber, int levelCount);

        void SetPings(int pingsLeft);

        void SetPingState(PingButtonState state);

        void SetHints(int hintsLeft);

        void SetHintState(HintButtonState state);

        // The spare hints waiting in the reserve: any above 0 shows a small "+1" tag on the hint button.
        void SetHintReserve(int reserveCount);

        // A short pop of the hint button: "a free hint just arrived".
        void PulseHint();

        // While paused the button shows "resume" instead of "pause".
        void SetPaused(bool paused);

        // A short shake of the ping button: "that did nothing".
        void ShakePing();

        // The same for the hint button.
        void ShakeHint();
    }
}
