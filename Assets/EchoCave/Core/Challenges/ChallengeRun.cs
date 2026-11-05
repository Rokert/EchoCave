using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // One attempt at a challenge while it is being played: every tap and ping in order, and the seconds spent on the
    // board. When the level is cleared it becomes the ChallengeAttempt that is replayed and ranked. Plain data with no
    // clock of its own: whoever owns it says how much time passed.
    public sealed class ChallengeRun
    {
        private readonly List<ChallengeMove> _moves = new List<ChallengeMove>();

        public string ChallengeId { get; }

        // Which try this is, counting from 1.
        public int Attempt { get; }

        public float ActiveSeconds { get; private set; }

        public int Taps { get; private set; }
        public int Pings { get; private set; }

        public IReadOnlyList<ChallengeMove> Moves => _moves;

        public ChallengeRun(string challengeId, int attempt)
        {
            ChallengeId = challengeId ?? throw new ArgumentNullException(nameof(challengeId));
            if (attempt < 1) throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempts count from 1.");

            Attempt = attempt;
        }

        public void RecordTap(int batId)
        {
            _moves.Add(ChallengeMove.Tap(batId));
            Taps++;
        }

        public void RecordPing()
        {
            _moves.Add(ChallengeMove.Ping());
            Pings++;
        }

        // Time on the board: not paused, not in the menu.
        public void AddSeconds(float seconds)
        {
            if (!(seconds >= 0f)) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Time cannot go backwards.");

            ActiveSeconds += seconds;
        }

        // The result to replay and rank. The solve time comes from the caller's clock.
        public ChallengeAttempt Build(long solvedAtUnixSeconds)
        {
            return new ChallengeAttempt(ChallengeId, _moves, ActiveSeconds, Attempt, solvedAtUnixSeconds);
        }
    }
}
