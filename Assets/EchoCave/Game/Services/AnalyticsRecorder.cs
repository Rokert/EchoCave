using System;
using System.Collections.Generic;
using System.Globalization;
using Rokert.Core;

namespace EchoCave.Game
{
    // Listens to the EventBus and counts what players do through IAnalyticsService. Plain C#: the composition root creates
    // it on enable and disposes it on disable. It only reports; it never changes the game, and a provider that throws
    // is caught so a broken analytics SDK can never break a level.
    public sealed class AnalyticsRecorder : IDisposable
    {
        private readonly EventBus _events;
        private readonly IAnalyticsService _analytics;
        private readonly IReadOnlyList<int> _levelIds;
        private readonly Action<Exception> _onError;

        // Where the player is, for events that do not carry it (a ping, a hint): a level id, a challenge id or "tutorial".
        private string _where = string.Empty;

        // levelIds[i] is the stable id of the level at catalog position i, like the ProgressRecorder's.
        // onError hears a provider that throws; without it the failure is dropped.
        public AnalyticsRecorder(EventBus events, IAnalyticsService analytics, IReadOnlyList<int> levelIds, Action<Exception> onError = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _levelIds = levelIds ?? throw new ArgumentNullException(nameof(levelIds));
            _onError = onError;

            _events.Subscribe<LevelStarted>(OnLevelStarted);
            _events.Subscribe<LevelWon>(OnLevelWon);
            _events.Subscribe<LevelLost>(OnLevelLost);
            _events.Subscribe<PingUsed>(OnPingUsed);
            _events.Subscribe<HintUsed>(OnHintUsed);
            _events.Subscribe<HintRefused>(OnHintRefused);
            _events.Subscribe<LessonStarted>(OnLessonStarted);
            _events.Subscribe<TutorialEnded>(OnTutorialEnded);
            _events.Subscribe<ChallengeStarted>(OnChallengeStarted);
            _events.Subscribe<ChallengeResultSaved>(OnChallengeResultSaved);
            _events.Subscribe<ChallengeVerified>(OnChallengeVerified);
            _events.Subscribe<ChallengeFailed>(OnChallengeFailed);
        }

        public void Dispose()
        {
            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
            _events.Unsubscribe<LevelWon>(OnLevelWon);
            _events.Unsubscribe<LevelLost>(OnLevelLost);
            _events.Unsubscribe<PingUsed>(OnPingUsed);
            _events.Unsubscribe<HintUsed>(OnHintUsed);
            _events.Unsubscribe<HintRefused>(OnHintRefused);
            _events.Unsubscribe<LessonStarted>(OnLessonStarted);
            _events.Unsubscribe<TutorialEnded>(OnTutorialEnded);
            _events.Unsubscribe<ChallengeStarted>(OnChallengeStarted);
            _events.Unsubscribe<ChallengeResultSaved>(OnChallengeResultSaved);
            _events.Unsubscribe<ChallengeVerified>(OnChallengeVerified);
            _events.Unsubscribe<ChallengeFailed>(OnChallengeFailed);
        }

        private void OnLevelStarted(LevelStarted started)
        {
            _where = Id(started.LevelIndex);
            Send(AnalyticsEvents.LevelStart,
                (AnalyticsEvents.Param.Level, _where),
                (AnalyticsEvents.Param.MoveLimit, Number(started.MoveLimit)),
                (AnalyticsEvents.Param.Pings, Number(started.PingBudget)));
        }

        private void OnLevelWon(LevelWon won)
        {
            Send(AnalyticsEvents.LevelWin,
                (AnalyticsEvents.Param.Level, Id(won.LevelIndex)),
                (AnalyticsEvents.Param.Moves, Number(won.MovesUsed)),
                (AnalyticsEvents.Param.Stars, Number(won.Stars)));
        }

        private void OnLevelLost(LevelLost lost)
        {
            Send(AnalyticsEvents.LevelLose,
                (AnalyticsEvents.Param.Level, Id(lost.LevelIndex)),
                (AnalyticsEvents.Param.Moves, Number(lost.MovesUsed)));
        }

        private void OnPingUsed(PingUsed used)
        {
            Send(AnalyticsEvents.PingUsed,
                (AnalyticsEvents.Param.Where, _where),
                (AnalyticsEvents.Param.PingsLeft, Number(used.PingsLeft)));
        }

        private void OnHintUsed(HintUsed used)
        {
            Send(AnalyticsEvents.HintUsed,
                (AnalyticsEvents.Param.Where, _where),
                (AnalyticsEvents.Param.HintsLeft, Number(used.HintsLeft)));
        }

        private void OnHintRefused(HintRefused refused)
        {
            Send(AnalyticsEvents.HintRefused,
                (AnalyticsEvents.Param.Where, _where),
                (AnalyticsEvents.Param.Reason, refused.Reason.ToString()));
        }

        // The pings and hints of a lesson are practice, not play: they are filed under "tutorial".
        private void OnLessonStarted(LessonStarted started)
        {
            _where = AnalyticsEvents.TutorialPlace;
        }

        private void OnTutorialEnded(TutorialEnded ended)
        {
            Send(AnalyticsEvents.TutorialEnd, (AnalyticsEvents.Param.Reason, ended.Reason.ToString()));
        }

        private void OnChallengeStarted(ChallengeStarted started)
        {
            _where = started.ChallengeId;
            Send(AnalyticsEvents.ChallengeStart,
                (AnalyticsEvents.Param.Challenge, started.ChallengeId),
                (AnalyticsEvents.Param.Attempt, Number(started.Attempt)));
        }

        // Sent for a result the replay accepted and the save kept, so every count is of a verified solve.
        private void OnChallengeResultSaved(ChallengeResultSaved saved)
        {
            Send(AnalyticsEvents.ChallengeSolved,
                (AnalyticsEvents.Param.Challenge, saved.ChallengeId),
                (AnalyticsEvents.Param.Moves, Number(saved.Result.Moves)),
                (AnalyticsEvents.Param.Seconds, Number(Math.Round(saved.Result.Seconds, 1))),
                (AnalyticsEvents.Param.Pings, Number(saved.Result.Pings)),
                (AnalyticsEvents.Param.Attempt, Number(saved.Attempts)),
                (AnalyticsEvents.Param.Rank, Number(saved.Rank)),
                (AnalyticsEvents.Param.Best, saved.IsBest ? "true" : "false"));
        }

        // A solved board the replay refused: worth knowing about, since it is either a bug or someone cheating.
        private void OnChallengeVerified(ChallengeVerified verified)
        {
            if (verified.Result.IsValid) return;

            Send(AnalyticsEvents.ChallengeRejected,
                (AnalyticsEvents.Param.Challenge, verified.ChallengeId),
                (AnalyticsEvents.Param.Reason, verified.Result.Error.ToString()));
        }

        private void OnChallengeFailed(ChallengeFailed failed)
        {
            Send(AnalyticsEvents.ChallengeFailed,
                (AnalyticsEvents.Param.Challenge, failed.ChallengeId),
                (AnalyticsEvents.Param.Moves, Number(failed.MovesUsed)));
        }

        // The stable id of the level at a catalog position; the position itself if the table has no such entry.
        private string Id(int levelIndex)
        {
            int id = levelIndex >= 0 && levelIndex < _levelIds.Count ? _levelIds[levelIndex] : levelIndex;
            return Number(id);
        }

        private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        private void Send(string name, params (string Key, string Value)[] parameters)
        {
            var map = new Dictionary<string, string>(parameters.Length);
            foreach ((string key, string value) in parameters) map[key] = value;

            try
            {
                _analytics.Track(name, map);
            }
            catch (Exception exception)
            {
                _onError?.Invoke(exception);
            }
        }
    }
}
