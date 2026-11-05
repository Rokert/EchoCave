using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // What the player has done on one challenge: how many tries, and the best results, best first.
    public sealed class ChallengeRecord
    {
        public string ChallengeId { get; }
        public int Attempts { get; }

        // Best first, at most ChallengeProgress.MaxResults.
        public IReadOnlyList<ChallengeResult> Results { get; }

        public bool HasResult => Results.Count > 0;

        // Only meaningful when HasResult.
        public ChallengeResult Best => Results[0];

        public ChallengeRecord(string challengeId, int attempts, IReadOnlyList<ChallengeResult> results)
        {
            ChallengeId = challengeId;
            Attempts = attempts;
            Results = results;
        }
    }

    // What recording a solve did.
    public readonly struct ChallengeSolveOutcome
    {
        // The place this result takes among the stored ones (1 is the best). A tie in everything shares the place.
        public int Rank { get; }

        // True when the result beats the best one so far, or is the first.
        public bool IsBest { get; }

        // False when the list was full of better results and this one was left out.
        public bool Stored { get; }

        public ChallengeSolveOutcome(int rank, bool isBest, bool stored)
        {
            Rank = rank;
            IsBest = isBest;
            Stored = stored;
        }
    }

    // The results and attempts of every challenge, by challenge id. Pure data and rules: where it is stored is the save
    // service's business. It keeps the best results of this device, which is also the local leaderboard.
    public sealed class ChallengeProgress
    {
        // The most results kept for one challenge.
        public const int MaxResults = 10;

        private sealed class Entry
        {
            public int Attempts;
            public readonly List<ChallengeResult> Results = new List<ChallengeResult>();
        }

        private readonly SortedDictionary<string, Entry> _entries = new SortedDictionary<string, Entry>(StringComparer.Ordinal);

        // Ordered by challenge id.
        public IReadOnlyList<ChallengeRecord> Records
        {
            get
            {
                var list = new List<ChallengeRecord>(_entries.Count);
                foreach (KeyValuePair<string, Entry> pair in _entries) list.Add(ToRecord(pair.Key, pair.Value));
                return list.AsReadOnly();
            }
        }

        // A challenge never tried has no attempts and no results.
        public ChallengeRecord Get(string challengeId)
        {
            RequireId(challengeId);
            return _entries.TryGetValue(challengeId, out Entry entry)
                ? ToRecord(challengeId, entry)
                : new ChallengeRecord(challengeId, 0, new List<ChallengeResult>().AsReadOnly());
        }

        // One more try, solved or not. Returns the new total.
        public int AddAttempt(string challengeId)
        {
            Entry entry = EntryOf(challengeId);
            entry.Attempts++;
            return entry.Attempts;
        }

        public ChallengeSolveOutcome RecordSolve(string challengeId, ChallengeResult result)
        {
            if (!result.IsValid) throw new ArgumentException("The result is not valid.", nameof(result));
            Entry entry = EntryOf(challengeId);

            var scores = new List<ChallengeScore>(entry.Results.Count);
            foreach (ChallengeResult existing in entry.Results) scores.Add(existing.Score);

            int rank = ChallengeRanking.RankOf(scores, result.Score);
            bool isBest = scores.Count == 0 || result.Score.IsBetterThan(scores[0]);
            bool stored = rank <= MaxResults;

            if (stored)
            {
                entry.Results.Add(result);
                SortAndTrim(entry);
            }

            return new ChallengeSolveOutcome(rank, isBest, stored);
        }

        // Puts back what was saved. Invalid results are dropped, the rest are sorted and capped, so a hand-edited save
        // cannot break the ranking.
        public void Restore(string challengeId, int attempts, IEnumerable<ChallengeResult> results)
        {
            Entry entry = EntryOf(challengeId);
            entry.Attempts = Math.Max(0, attempts);
            entry.Results.Clear();
            foreach (ChallengeResult result in results)
            {
                if (result.IsValid) entry.Results.Add(result);
            }

            SortAndTrim(entry);
        }

        private Entry EntryOf(string challengeId)
        {
            RequireId(challengeId);
            if (!_entries.TryGetValue(challengeId, out Entry entry))
            {
                entry = new Entry();
                _entries[challengeId] = entry;
            }

            return entry;
        }

        private static void RequireId(string challengeId)
        {
            if (!ChallengeCatalogRules.IsValidId(challengeId))
                throw new ArgumentException($"\"{challengeId}\" is not a valid challenge id.", nameof(challengeId));
        }

        private static void SortAndTrim(Entry entry)
        {
            entry.Results.Sort((a, b) => a.Score.CompareTo(b.Score));
            if (entry.Results.Count > MaxResults) entry.Results.RemoveRange(MaxResults, entry.Results.Count - MaxResults);
        }

        private static ChallengeRecord ToRecord(string id, Entry entry)
        {
            return new ChallengeRecord(id, entry.Attempts, new List<ChallengeResult>(entry.Results).AsReadOnly());
        }
    }
}
