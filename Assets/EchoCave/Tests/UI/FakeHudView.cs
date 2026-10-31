using System;
using EchoCave.UI;

namespace EchoCave.Tests
{
    // Stands in for HudView and records the last thing it was told.
    internal sealed class FakeHudView : IHudView
    {
        public event Action PingPressed;
        public event Action PausePressed;

        public bool Visible = true;
        public int MovesUsed = -1;
        public int MoveLimit = -1;
        public int Pings = -1;
        public int LevelNumber = -1;
        public int LevelCount = -1;
        public PingButtonState PingState = PingButtonState.Ready;
        public bool Paused;
        public int ShakeCount;

        public void SetVisible(bool visible) => Visible = visible;

        public void SetMoves(int movesUsed, int moveLimit)
        {
            MovesUsed = movesUsed;
            MoveLimit = moveLimit;
        }

        public void SetLevel(int levelNumber, int levelCount)
        {
            LevelNumber = levelNumber;
            LevelCount = levelCount;
        }

        public void SetPings(int pingsLeft) => Pings = pingsLeft;

        public void SetPingState(PingButtonState state) => PingState = state;

        public void SetPaused(bool paused) => Paused = paused;

        public void ShakePing() => ShakeCount++;

        public void PressPing() => PingPressed?.Invoke();

        public void PressPause() => PausePressed?.Invoke();
    }
}
