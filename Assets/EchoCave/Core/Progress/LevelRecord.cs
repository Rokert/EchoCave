namespace EchoCave.Core
{
    // The best result a player has had on one level.
    public readonly struct LevelRecord
    {
        public int Index { get; }
        public int Stars { get; }
        public int BestMoves { get; }

        public LevelRecord(int index, int stars, int bestMoves)
        {
            Index = index;
            Stars = stars;
            BestMoves = bestMoves;
        }

        public bool IsValid => Index >= 0 && Stars >= 1 && Stars <= PlayerProgress.MaxStars && BestMoves >= 0;
    }
}
