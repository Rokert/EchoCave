using System;
using System.Collections.Generic;
using EchoCave.Core;
using Rokert.Core;

namespace EchoCave.Game
{
    // Replays every attempt the player solves through the same Core code the server will use, and reports the verdict.
    // A client that sends a result its own replay rejects has a bug, and this is where it shows. Plain C#: the
    // composition root creates it on enable and disposes it on disable.
    public sealed class ChallengeSelfCheck : IDisposable
    {
        private readonly EventBus _events;
        private readonly IReadOnlyList<ChallengeEntry> _entries;
        private readonly MoveResolver _resolver;
        private readonly float _minSecondsPerTap;
        private readonly Action<string> _info;
        private readonly Action<string> _error;

        public ChallengeSelfCheck(
            EventBus events, IReadOnlyList<ChallengeEntry> entries, MoveResolver resolver, float minSecondsPerTap,
            Action<string> info, Action<string> error)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _minSecondsPerTap = minSecondsPerTap;
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _error = error ?? throw new ArgumentNullException(nameof(error));

            _events.Subscribe<ChallengeSolved>(OnSolved);
        }

        public void Dispose()
        {
            _events.Unsubscribe<ChallengeSolved>(OnSolved);
        }

        private void OnSolved(ChallengeSolved solved)
        {
            if (solved.ChallengeIndex < 0 || solved.ChallengeIndex >= _entries.Count)
            {
                _error($"[Challenge] {solved.ChallengeId}: solved, but there is no challenge at position {solved.ChallengeIndex}.");
                return;
            }

            ReplayResult result = ChallengeReplay.Verify(_entries[solved.ChallengeIndex].Level, solved.Attempt, _resolver, _minSecondsPerTap);
            if (result.IsValid)
            {
                _info($"[Challenge] {solved.ChallengeId} solved in {result.Moves} taps, {result.Pings} ping(s), {solved.Attempt.ActiveSeconds:0.0} s, attempt {solved.Attempt.Attempts}. The replay agrees.");
            }
            else
            {
                _error($"[Challenge] {solved.ChallengeId}: the replay REJECTED this attempt ({result.Error}): {result.Message}");
            }
        }
    }
}
