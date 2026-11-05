using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class ChallengeRecorderTests
    {
        private const string Id = "challenge-001";

        private EventBus _bus;
        private FakeSaveService _save;
        private ChallengeRecorder _recorder;
        private readonly List<ChallengeResultSaved> _saved = new List<ChallengeResultSaved>();

        [SetUp]
        public void SetUp()
        {
            _saved.Clear();
            _bus = new EventBus();
            _bus.Subscribe<ChallengeResultSaved>(e => _saved.Add(e));
            _save = new FakeSaveService(3);
            _recorder = new ChallengeRecorder(_bus, _save);
        }

        [TearDown]
        public void TearDown()
        {
            _recorder.Dispose();
        }

        private static ChallengeVerified Verified(bool valid, int moves = 52, float seconds = 140f, int pings = 1, long solvedAt = 1700000000L)
        {
            var attempt = new ChallengeAttempt(Id, new ChallengeMove[0], seconds, 1, solvedAt);
            ReplayResult result = valid
                ? new ReplayResult(ReplayError.None, string.Empty, moves, pings)
                // Rejected, but with taps and a time that would look like a fine result if the verdict were ignored.
                : new ReplayResult(ReplayError.ImpossibleTime, "too fast", moves, pings);
            return new ChallengeVerified(0, Id, attempt, result);
        }

        [Test]
        public void EveryStartedChallenge_IsOneMoreSavedTry()
        {
            _bus.Publish(new ChallengeStarted(0, 1, Id, 0, 1, 1));
            _bus.Publish(new ChallengeStarted(0, 1, Id, 0, 1, 2));

            Assert.AreEqual(2, _save.Challenges.Get(Id).Attempts);
        }

        [Test]
        public void AVerifiedAttempt_IsSaved_AndPublishedWithItsRankAndTheTotalTries()
        {
            _bus.Publish(new ChallengeStarted(0, 1, Id, 0, 1, 1));
            _bus.Publish(new ChallengeStarted(0, 1, Id, 0, 1, 2));

            _bus.Publish(Verified(true));

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(Id, _saved[0].ChallengeId);
            Assert.AreEqual(2, _saved[0].Attempts);
            Assert.AreEqual(1, _saved[0].Rank);
            Assert.IsTrue(_saved[0].IsBest);
            Assert.AreEqual(52, _saved[0].Result.Moves);
            Assert.AreEqual(140f, _saved[0].Result.Seconds, 1e-4f);
            Assert.AreEqual(1, _saved[0].Result.Pings);
            Assert.AreEqual(1700000000L, _saved[0].Result.SolvedAtUnixSeconds);
            Assert.AreEqual(52, _save.Challenges.Get(Id).Best.Moves);
        }

        [Test]
        public void ARejectedAttempt_IsNeverKept()
        {
            _bus.Publish(Verified(false));

            Assert.IsEmpty(_saved);
            Assert.IsFalse(_save.Challenges.Get(Id).HasResult);
        }

        [Test]
        public void ASecondBetterResult_IsRankedFirst_AWorseOneIsNot()
        {
            _bus.Publish(Verified(true, moves: 52));
            _bus.Publish(Verified(true, moves: 45));
            _bus.Publish(Verified(true, moves: 60));

            Assert.AreEqual(3, _saved.Count);
            Assert.IsTrue(_saved[1].IsBest);
            Assert.AreEqual(1, _saved[1].Rank);
            Assert.IsFalse(_saved[2].IsBest);
            Assert.AreEqual(3, _saved[2].Rank);
        }

        [Test]
        public void AfterDispose_NothingIsRecorded()
        {
            _recorder.Dispose();

            _bus.Publish(new ChallengeStarted(0, 1, Id, 0, 1, 1));
            _bus.Publish(Verified(true));

            Assert.AreEqual(0, _save.Challenges.Get(Id).Attempts);
            Assert.IsEmpty(_saved);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new ChallengeRecorder(null, _save));
            Assert.Throws<ArgumentNullException>(() => new ChallengeRecorder(_bus, null));
        }
    }
}
