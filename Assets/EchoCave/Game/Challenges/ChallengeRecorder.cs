using System;
using EchoCave.Core;
using Rokert.Core;

namespace EchoCave.Game
{
    // Keeps what the player does on a challenge: one more try each time a challenge starts, and the result of an attempt
    // the replay accepted. A result the replay rejected is never kept. Plain C#: the composition root creates it on
    // enable and disposes it on disable.
    public sealed class ChallengeRecorder : IDisposable
    {
        private readonly EventBus _events;
        private readonly ISaveService _save;

        public ChallengeRecorder(EventBus events, ISaveService save)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _save = save ?? throw new ArgumentNullException(nameof(save));

            _events.Subscribe<ChallengeStarted>(OnStarted);
            _events.Subscribe<ChallengeVerified>(OnVerified);
        }

        public void Dispose()
        {
            _events.Unsubscribe<ChallengeStarted>(OnStarted);
            _events.Unsubscribe<ChallengeVerified>(OnVerified);
        }

        private void OnStarted(ChallengeStarted started)
        {
            _save.AddChallengeAttempt(started.ChallengeId);
        }

        private void OnVerified(ChallengeVerified verified)
        {
            if (!verified.Result.IsValid) return;

            var result = new ChallengeResult(
                verified.Result.Moves, verified.Attempt.ActiveSeconds, verified.Result.Pings, verified.Attempt.SolvedAtUnixSeconds);
            ChallengeSolveOutcome outcome = _save.RecordChallengeSolve(verified.ChallengeId, result);

            int attempts = _save.Challenges.Get(verified.ChallengeId).Attempts;
            _events.Publish(new ChallengeResultSaved(verified.ChallengeId, attempts, result, outcome.Rank, outcome.IsBest));
        }
    }
}
