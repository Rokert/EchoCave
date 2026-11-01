using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // What the player has achieved, level by level. Pure data and rules: where it is stored is the save
    // service's business. A level is identified by its stable id (see LevelIdRules), not by its place in the catalog.
    public sealed class PlayerProgress
    {
        public const int MaxStars = 3;

        private readonly SortedDictionary<int, LevelRecord> _records = new SortedDictionary<int, LevelRecord>();

        public int CompletedCount => _records.Count;

        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (LevelRecord record in _records.Values) total += record.Stars;
                return total;
            }
        }

        // Ordered by level id.
        public IReadOnlyList<LevelRecord> Records
        {
            get
            {
                var list = new List<LevelRecord>(_records.Values);
                return list.AsReadOnly();
            }
        }

        public bool IsCompleted(int levelId) => _records.ContainsKey(levelId);

        // 0 when the level has never been won.
        public int StarsOf(int levelId) => _records.TryGetValue(levelId, out LevelRecord r) ? r.Stars : 0;

        // 0 when the level has never been won.
        public int BestMovesOf(int levelId) => _records.TryGetValue(levelId, out LevelRecord r) ? r.BestMoves : 0;

        // Keeps the best stars and the fewest moves separately, so a win never lowers either.
        // Returns true when something improved.
        public bool RecordWin(int levelId, int stars, int moves)
        {
            if (levelId < 1) throw new ArgumentOutOfRangeException(nameof(levelId), levelId, "Level ids start at 1.");
            if (stars < 1 || stars > MaxStars) throw new ArgumentOutOfRangeException(nameof(stars), stars, "A win earns 1 to 3 stars.");
            if (moves < 0) throw new ArgumentOutOfRangeException(nameof(moves), moves, "Must not be negative.");

            if (!_records.TryGetValue(levelId, out LevelRecord old))
            {
                _records[levelId] = new LevelRecord(levelId, stars, moves);
                return true;
            }

            int bestStars = Math.Max(old.Stars, stars);
            int bestMoves = Math.Min(old.BestMoves, moves);
            if (bestStars == old.Stars && bestMoves == old.BestMoves) return false;

            _records[levelId] = new LevelRecord(levelId, bestStars, bestMoves);
            return true;
        }
    }
}
