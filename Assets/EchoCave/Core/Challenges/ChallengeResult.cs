using System;

namespace EchoCave.Core
{
    // One solved attempt, as it is kept and shown: what it took (taps, seconds on the board, pings) and when. Pings and
    // the attempt count are kept for the player to read; only the score ranks.
    public readonly struct ChallengeResult
    {
        public int Moves { get; }
        public float Seconds { get; }
        public int Pings { get; }
        public long SolvedAtUnixSeconds { get; }

        public ChallengeResult(int moves, float seconds, int pings, long solvedAtUnixSeconds)
        {
            Moves = moves;
            Seconds = seconds;
            Pings = pings;
            SolvedAtUnixSeconds = solvedAtUnixSeconds;
        }

        public ChallengeScore Score => new ChallengeScore(Moves, Seconds, SolvedAtUnixSeconds);

        // A real result has at least one tap, a time that is a number and not negative, and no negative pings.
        public bool IsValid => Moves >= 1 && !float.IsNaN(Seconds) && !float.IsInfinity(Seconds) && Seconds >= 0f && Pings >= 0;
    }
}
