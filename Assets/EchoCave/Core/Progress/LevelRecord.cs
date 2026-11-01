namespace EchoCave.Core
{
    // The best result a player has had on one level, identified by its stable level id.
    public readonly struct LevelRecord
    {
        public int LevelId { get; }
        public int Stars { get; }
        public int BestMoves { get; }

        public LevelRecord(int levelId, int stars, int bestMoves)
        {
            LevelId = levelId;
            Stars = stars;
            BestMoves = bestMoves;
        }

        public bool IsValid => LevelId >= 1 && Stars >= 1 && Stars <= PlayerProgress.MaxStars && BestMoves >= 0;
    }
}
