using EchoCave.Core;

namespace EchoCave.Game
{
    // Published on the EventBus by GameFlow, so the HUD and popups never have to poll it.

    public readonly struct LevelStarted
    {
        public int LevelIndex { get; }

        // 0 means no move limit.
        public int MoveLimit { get; }

        public int PingBudget { get; }

        public LevelStarted(int levelIndex, int moveLimit, int pingBudget)
        {
            LevelIndex = levelIndex;
            MoveLimit = moveLimit;
            PingBudget = pingBudget;
        }
    }

    // A tap was resolved and spent a move. MoveLimit is 0 when the level has none.
    public readonly struct MoveSpent
    {
        public int MovesUsed { get; }
        public int MoveLimit { get; }
        public MoveOutcome Outcome { get; }

        public MoveSpent(int movesUsed, int moveLimit, MoveOutcome outcome)
        {
            MovesUsed = movesUsed;
            MoveLimit = moveLimit;
            Outcome = outcome;
        }
    }

    // The game was paused or resumed.
    public readonly struct PauseChanged
    {
        public bool IsPaused { get; }

        public PauseChanged(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }

    // The flow went back to the menu, so there is no level on screen.
    public readonly struct MenuOpened { }

    public readonly struct LevelWon
    {
        public int LevelIndex { get; }
        public int MovesUsed { get; }
        public int Stars { get; }

        public LevelWon(int levelIndex, int movesUsed, int stars)
        {
            LevelIndex = levelIndex;
            MovesUsed = movesUsed;
            Stars = stars;
        }
    }

    public readonly struct LevelLost
    {
        public int LevelIndex { get; }
        public int MovesUsed { get; }

        public LevelLost(int levelIndex, int movesUsed)
        {
            LevelIndex = levelIndex;
            MovesUsed = movesUsed;
        }
    }

    // A ping started a reveal.
    public readonly struct PingUsed
    {
        public int PingsLeft { get; }
        public float DurationSeconds { get; }

        public PingUsed(int pingsLeft, float durationSeconds)
        {
            PingsLeft = pingsLeft;
            DurationSeconds = durationSeconds;
        }
    }

    // The reveal ran out and the hidden bats went dark again.
    public readonly struct EchoExpired { }

    // A ping did nothing; the reason says why, so the HUD can tell the player.
    public readonly struct PingRefused
    {
        public PingResult Reason { get; }

        public PingRefused(PingResult reason)
        {
            Reason = reason;
        }
    }
}
