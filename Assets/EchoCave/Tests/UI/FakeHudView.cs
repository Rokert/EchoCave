using System;
using EchoCave.UI;

namespace EchoCave.Tests
{
    // Stands in for HudView and records the last thing it was told.
    internal sealed class FakeHudView : IHudView
    {
        public event Action PingPressed;
        public event Action HintPressed;
        public event Action PausePressed;

        public bool Visible = true;
        public int MovesUsed = -1;
        public int MoveLimit = -1;
        public int Pings = -1;
        public int LevelNumber = -1;
        public int LevelCount = -1;
        public PingButtonState PingState = PingButtonState.Ready;
        public int Hints = -1;
        public HintButtonState HintState = HintButtonState.Ready;
        public int HintShakeCount;
        public int HintReserve = -1;
        public int HintPulseCount;
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

        public void SetHints(int hintsLeft) => Hints = hintsLeft;

        public void SetHintState(HintButtonState state) => HintState = state;

        public void SetHintReserve(int reserveCount) => HintReserve = reserveCount;

        public void PulseHint() => HintPulseCount++;

        public void SetPaused(bool paused) => Paused = paused;

        public void ShakePing() => ShakeCount++;

        public void ShakeHint() => HintShakeCount++;

        public void PressPing() => PingPressed?.Invoke();

        public void PressHint() => HintPressed?.Invoke();

        public void PressPause() => PausePressed?.Invoke();
    }
}
