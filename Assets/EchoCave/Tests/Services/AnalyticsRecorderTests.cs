using System;
using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class AnalyticsRecorderTests
    {
        private sealed class Sent
        {
            public string Name;
            public Dictionary<string, string> Parameters;
        }

        private sealed class RecordingAnalytics : IAnalyticsService
        {
            public readonly List<Sent> Events = new List<Sent>();
            public bool Throw;

            public void Track(string eventName, IReadOnlyDictionary<string, string> parameters)
            {
                if (Throw) throw new InvalidOperationException("provider down");
                Events.Add(new Sent { Name = eventName, Parameters = new Dictionary<string, string>(parameters) });
            }
        }

        // Catalog positions 0..2 are the levels with the stable ids 10, 20 and 30.
        private static readonly int[] LevelIds = { 10, 20, 30 };

        private EventBus _bus;
        private RecordingAnalytics _analytics;
        private AnalyticsRecorder _recorder;
        private readonly List<Exception> _errors = new List<Exception>();

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _analytics = new RecordingAnalytics();
            _errors.Clear();
            _recorder = new AnalyticsRecorder(_bus, _analytics, LevelIds, _errors.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _recorder.Dispose();
        }

        private Sent Only() => _analytics.Events.Single();

        [Test]
        public void ALevelStart_ReportsTheStableLevelId_NotThePosition()
        {
            _bus.Publish(new LevelStarted(1, 9, 2));

            Sent sent = Only();
            Assert.AreEqual(AnalyticsEvents.LevelStart, sent.Name);
            Assert.AreEqual("20", sent.Parameters[AnalyticsEvents.Param.Level]);
            Assert.AreEqual("9", sent.Parameters[AnalyticsEvents.Param.MoveLimit]);
            Assert.AreEqual("2", sent.Parameters[AnalyticsEvents.Param.Pings]);
        }

        [Test]
        public void AWin_ReportsTheLevelTheMovesAndTheStars()
        {
            _bus.Publish(new LevelWon(2, 7, 3));

            Sent sent = Only();
            Assert.AreEqual(AnalyticsEvents.LevelWin, sent.Name);
            Assert.AreEqual("30", sent.Parameters[AnalyticsEvents.Param.Level]);
            Assert.AreEqual("7", sent.Parameters[AnalyticsEvents.Param.Moves]);
            Assert.AreEqual("3", sent.Parameters[AnalyticsEvents.Param.Stars]);
        }

        [Test]
        public void ALoss_ReportsTheLevelAndTheMoves()
        {
            _bus.Publish(new LevelLost(0, 9));

            Sent sent = Only();
            Assert.AreEqual(AnalyticsEvents.LevelLose, sent.Name);
            Assert.AreEqual("10", sent.Parameters[AnalyticsEvents.Param.Level]);
            Assert.AreEqual("9", sent.Parameters[AnalyticsEvents.Param.Moves]);
        }

        [Test]
        public void APositionOutsideTheTable_FallsBackToThePosition_AndNeverThrows()
        {
            _bus.Publish(new LevelWon(7, 3, 1));

            Assert.AreEqual("7", Only().Parameters[AnalyticsEvents.Param.Level]);
        }

        [Test]
        public void APingAndAHint_AreFiledUnderTheLevelThePlayerIsOn()
        {
            _bus.Publish(new LevelStarted(2, 9, 2));
            _analytics.Events.Clear();

            _bus.Publish(new PingUsed(1, 3f));
            _bus.Publish(new HintUsed(2, 0));
            _bus.Publish(new HintRefused(HintRefusal.NoHintsLeft));

            Assert.AreEqual(3, _analytics.Events.Count);
            Assert.AreEqual(AnalyticsEvents.PingUsed, _analytics.Events[0].Name);
            Assert.AreEqual("1", _analytics.Events[0].Parameters[AnalyticsEvents.Param.PingsLeft]);
            Assert.AreEqual(AnalyticsEvents.HintUsed, _analytics.Events[1].Name);
            Assert.AreEqual("2", _analytics.Events[1].Parameters[AnalyticsEvents.Param.HintsLeft]);
            Assert.AreEqual(AnalyticsEvents.HintRefused, _analytics.Events[2].Name);
            Assert.AreEqual("NoHintsLeft", _analytics.Events[2].Parameters[AnalyticsEvents.Param.Reason]);
            Assert.IsTrue(_analytics.Events.All(e => e.Parameters[AnalyticsEvents.Param.Where] == "30"));
        }

        [Test]
        public void APingInALesson_IsFiledUnderTheTutorial_AndOneInAChallengeUnderItsId()
        {
            _bus.Publish(new LessonStarted(0, 5, 3, 1));
            _bus.Publish(new PingUsed(0, 3f));
            _bus.Publish(new ChallengeStarted(0, 3, "challenge-002", -1, 1, 1));
            _bus.Publish(new PingUsed(0, 3f));

            string[] places = _analytics.Events
                .Where(e => e.Name == AnalyticsEvents.PingUsed)
                .Select(e => e.Parameters[AnalyticsEvents.Param.Where]).ToArray();
            CollectionAssert.AreEqual(new[] { AnalyticsEvents.TutorialPlace, "challenge-002" }, places);
        }

        [Test]
        public void TheTutorialEnding_ReportsHowItEnded()
        {
            _bus.Publish(new TutorialEnded(TutorialEnd.Skipped));

            Sent sent = Only();
            Assert.AreEqual(AnalyticsEvents.TutorialEnd, sent.Name);
            Assert.AreEqual("Skipped", sent.Parameters[AnalyticsEvents.Param.Reason]);
        }

        [Test]
        public void AChallengeStart_ReportsItsIdAndTheAttempt()
        {
            _bus.Publish(new ChallengeStarted(1, 3, "challenge-002", -1, 1, 4));

            Sent sent = Only();
            Assert.AreEqual(AnalyticsEvents.ChallengeStart, sent.Name);
            Assert.AreEqual("challenge-002", sent.Parameters[AnalyticsEvents.Param.Challenge]);
            Assert.AreEqual("4", sent.Parameters[AnalyticsEvents.Param.Attempt]);
        }

        [Test]
        public void AKeptChallengeResult_ReportsTheNumbersWithADotForDecimals()
        {
            var result = new ChallengeResult(48, 141.26f, 1, 1_700_000_000L);
            _bus.Publish(new ChallengeResultSaved("challenge-002", 3, result, 2, false));

            Sent sent = Only();
            Assert.AreEqual(AnalyticsEvents.ChallengeSolved, sent.Name);
            Assert.AreEqual("challenge-002", sent.Parameters[AnalyticsEvents.Param.Challenge]);
            Assert.AreEqual("48", sent.Parameters[AnalyticsEvents.Param.Moves]);
            Assert.AreEqual("141.3", sent.Parameters[AnalyticsEvents.Param.Seconds]);
            Assert.AreEqual("1", sent.Parameters[AnalyticsEvents.Param.Pings]);
            Assert.AreEqual("3", sent.Parameters[AnalyticsEvents.Param.Attempt]);
            Assert.AreEqual("2", sent.Parameters[AnalyticsEvents.Param.Rank]);
            Assert.AreEqual("false", sent.Parameters[AnalyticsEvents.Param.Best]);
        }

        [Test]
        public void ANewBestResult_IsFlaggedAsBest()
        {
            _bus.Publish(new ChallengeResultSaved("challenge-001", 1, new ChallengeResult(40, 60f, 1, 1_700_000_000L), 1, true));

            Assert.AreEqual("true", Only().Parameters[AnalyticsEvents.Param.Best]);
        }

        [Test]
        public void ARejectedChallengeRun_IsReportedWithTheReason_AndAnAcceptedOneIsNot()
        {
            var attempt = new ChallengeAttempt("challenge-001", new ChallengeMove[0], 1f, 1, 0);

            _bus.Publish(new ChallengeVerified(0, "challenge-001", attempt, new ReplayResult(ReplayError.ImpossibleTime, "too fast", 5, 0)));
            _bus.Publish(new ChallengeVerified(0, "challenge-001", attempt, new ReplayResult(ReplayError.None, string.Empty, 40, 1)));

            Sent sent = Only();
            Assert.AreEqual(AnalyticsEvents.ChallengeRejected, sent.Name);
            Assert.AreEqual("ImpossibleTime", sent.Parameters[AnalyticsEvents.Param.Reason]);
        }

        [Test]
        public void AFailedChallenge_ReportsItsMoves()
        {
            _bus.Publish(new ChallengeFailed(0, "challenge-001", 12));

            Sent sent = Only();
            Assert.AreEqual(AnalyticsEvents.ChallengeFailed, sent.Name);
            Assert.AreEqual("12", sent.Parameters[AnalyticsEvents.Param.Moves]);
        }

        [Test]
        public void EventsThatAreNotWorthCounting_SendNothing()
        {
            _bus.Publish(new PauseChanged(true));
            _bus.Publish(new MenuOpened());
            _bus.Publish(new MoveSpent(1, 9, MoveOutcome.Exited));
            _bus.Publish(new EchoExpired());

            Assert.IsEmpty(_analytics.Events);
        }

        [Test]
        public void AProviderThatThrows_NeverBreaksTheGame_AndTheErrorIsHeard()
        {
            _analytics.Throw = true;

            Assert.DoesNotThrow(() => _bus.Publish(new LevelWon(0, 3, 3)));
            Assert.AreEqual(1, _errors.Count);
            Assert.IsInstanceOf<InvalidOperationException>(_errors[0]);
        }

        [Test]
        public void AfterDispose_NothingIsReported()
        {
            _recorder.Dispose();

            _bus.Publish(new LevelStarted(0, 9, 2));
            _bus.Publish(new LevelWon(0, 3, 3));
            _bus.Publish(new ChallengeStarted(0, 3, "challenge-001", -1, 1, 1));

            Assert.IsEmpty(_analytics.Events);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new AnalyticsRecorder(null, _analytics, LevelIds));
            Assert.Throws<ArgumentNullException>(() => new AnalyticsRecorder(_bus, null, LevelIds));
            Assert.Throws<ArgumentNullException>(() => new AnalyticsRecorder(_bus, _analytics, null));
        }

        [Test]
        public void EveryEventName_IsUnique_AndLowercaseWithUnderscores()
        {
            string[] names =
            {
                AnalyticsEvents.LevelStart, AnalyticsEvents.LevelWin, AnalyticsEvents.LevelLose, AnalyticsEvents.PingUsed,
                AnalyticsEvents.HintUsed, AnalyticsEvents.HintRefused, AnalyticsEvents.TutorialEnd, AnalyticsEvents.ChallengeStart,
                AnalyticsEvents.ChallengeSolved, AnalyticsEvents.ChallengeFailed, AnalyticsEvents.ChallengeRejected
            };

            CollectionAssert.AllItemsAreUnique(names);
            Assert.IsTrue(names.All(n => n.All(c => (c >= 'a' && c <= 'z') || c == '_')), "provider-friendly names");
        }
    }
}
