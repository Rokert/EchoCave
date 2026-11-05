using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // The result that ranks on a challenge: fewest taps, then least time on the board, then whoever solved it first.
    // Pings and attempts are kept and shown elsewhere; they never move a player up or down.
    public readonly struct ChallengeScore : IComparable<ChallengeScore>
    {
        public int Moves { get; }
        public float Seconds { get; }
        public long SolvedAtUnixSeconds { get; }

        public ChallengeScore(int moves, float seconds, long solvedAtUnixSeconds)
        {
            Moves = moves;
            Seconds = seconds;
            SolvedAtUnixSeconds = solvedAtUnixSeconds;
        }

        // Negative when this one ranks higher (better), positive when lower, 0 when they tie in everything.
        public int CompareTo(ChallengeScore other)
        {
            int byMoves = Moves.CompareTo(other.Moves);
            if (byMoves != 0) return byMoves;

            int bySeconds = Seconds.CompareTo(other.Seconds);
            if (bySeconds != 0) return bySeconds;

            return SolvedAtUnixSeconds.CompareTo(other.SolvedAtUnixSeconds);
        }

        public bool IsBetterThan(ChallengeScore other) => CompareTo(other) < 0;
    }

    public static class ChallengeRanking
    {
        // The place (1 is the best) a score would take among the results already there. A tie in everything shares the
        // place: only a strictly better result pushes it down.
        public static int RankOf(IReadOnlyList<ChallengeScore> existing, ChallengeScore candidate)
        {
            if (existing == null) throw new ArgumentNullException(nameof(existing));

            int better = 0;
            for (int i = 0; i < existing.Count; i++)
            {
                if (existing[i].IsBetterThan(candidate)) better++;
            }

            return better + 1;
        }
    }
}
