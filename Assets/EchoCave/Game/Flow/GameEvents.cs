namespace EchoCave.Game
{
    // Published on the EventBus by GameFlow, so the HUD and popups never have to poll it.

    public readonly struct LevelStarted
    {
        public int LevelIndex { get; }

        // 0 means no move limit.
        public int MoveLimit { get; }

        public LevelStarted(int levelIndex, int moveLimit)
        {
            LevelIndex = levelIndex;
            MoveLimit = moveLimit;
        }
    }

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
}
