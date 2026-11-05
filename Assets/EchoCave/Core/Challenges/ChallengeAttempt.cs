using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // What the player sends when a challenge is solved: the list of everything they did, plus the numbers that cannot
    // be replayed. The move count is not here on purpose: it is counted from the list, so there is no number to forge.
    // Nothing is validated on construction, so data that came from outside can be held and then judged by ChallengeReplay.
    public sealed class ChallengeAttempt
    {
        public string ChallengeId { get; }
        public IReadOnlyList<ChallengeMove> Moves { get; }

        // Seconds spent on the board, not counting pauses or the menu. Reported by the device: it cannot be replayed,
        // only checked for what is humanly possible.
        public float ActiveSeconds { get; }

        // How many tries this one was, counting the solved one. At least 1.
        public int Attempts { get; }

        // When the device says it was solved (Unix seconds). A server must use its own clock for the "first to solve" rule.
        public long SolvedAtUnixSeconds { get; }

        public ChallengeAttempt(string challengeId, IEnumerable<ChallengeMove> moves, float activeSeconds, int attempts, long solvedAtUnixSeconds)
        {
            ChallengeId = challengeId ?? throw new ArgumentNullException(nameof(challengeId));
            if (moves == null) throw new ArgumentNullException(nameof(moves));

            Moves = new List<ChallengeMove>(moves).AsReadOnly();
            ActiveSeconds = activeSeconds;
            Attempts = attempts;
            SolvedAtUnixSeconds = solvedAtUnixSeconds;
        }
    }
}
