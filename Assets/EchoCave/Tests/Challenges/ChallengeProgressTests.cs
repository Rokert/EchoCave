using System;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeProgressTests
    {
        private const string Id = "challenge-001";

        private static ChallengeResult Result(int moves, float seconds, long solvedAt = 1000, int pings = 1)
        {
            return new ChallengeResult(moves, seconds, pings, solvedAt);
        }

        [Test]
        public void ANeverTriedChallenge_HasNoAttemptsAndNoResults()
        {
            ChallengeRecord record = new ChallengeProgress().Get(Id);

            Assert.AreEqual(0, record.Attempts);
            Assert.IsFalse(record.HasResult);
            Assert.AreEqual(Id, record.ChallengeId);
        }

        [Test]
        public void Attempts_CountUp_PerChallenge()
        {
            var progress = new ChallengeProgress();

            Assert.AreEqual(1, progress.AddAttempt(Id));
            Assert.AreEqual(2, progress.AddAttempt(Id));
            Assert.AreEqual(1, progress.AddAttempt("challenge-002"));

            Assert.AreEqual(2, progress.Get(Id).Attempts);
        }

        [Test]
        public void TheFirstSolve_IsRankOne_AndTheBest()
        {
            var progress = new ChallengeProgress();

            ChallengeSolveOutcome outcome = progress.RecordSolve(Id, Result(52, 140f));

            Assert.AreEqual(1, outcome.Rank);
            Assert.IsTrue(outcome.IsBest);
            Assert.IsTrue(outcome.Stored);
            Assert.AreEqual(52, progress.Get(Id).Best.Moves);
        }

        [Test]
        public void ABetterSolve_BecomesTheBest_AWorseOneRanksBelowIt()
        {
            var progress = new ChallengeProgress();
            progress.RecordSolve(Id, Result(52, 140f));

            ChallengeSolveOutcome better = progress.RecordSolve(Id, Result(45, 200f));
            ChallengeSolveOutcome worse = progress.RecordSolve(Id, Result(60, 90f));

            Assert.IsTrue(better.IsBest);
            Assert.AreEqual(1, better.Rank);
            Assert.IsFalse(worse.IsBest);
            Assert.AreEqual(3, worse.Rank);
            Assert.AreEqual(new[] { 45, 52, 60 }, progress.Get(Id).Results.Select(r => r.Moves).ToArray());
        }

        [Test]
        public void WithTheSameMoves_LessTimeRanksHigher_AndAnEarlierSolveBreaksTheRest()
        {
            var progress = new ChallengeProgress();
            progress.RecordSolve(Id, Result(40, 100f, solvedAt: 500));

            ChallengeSolveOutcome faster = progress.RecordSolve(Id, Result(40, 90f, solvedAt: 900));
            ChallengeSolveOutcome sameButLater = progress.RecordSolve(Id, Result(40, 90f, solvedAt: 950));

            Assert.AreEqual(1, faster.Rank);
            Assert.IsTrue(faster.IsBest);
            Assert.AreEqual(2, sameButLater.Rank, "equal in moves and seconds, but solved later");
            Assert.IsFalse(sameButLater.IsBest);
        }

        [Test]
        public void ARepeatOfTheBestResultInEveryWay_IsNotANewBest_AndSharesTheFirstPlace()
        {
            var progress = new ChallengeProgress();
            progress.RecordSolve(Id, Result(40, 90f, solvedAt: 900));

            ChallengeSolveOutcome again = progress.RecordSolve(Id, Result(40, 90f, solvedAt: 900));

            Assert.AreEqual(1, again.Rank);
            Assert.IsFalse(again.IsBest);
        }

        [Test]
        public void OnlyTheBestTenResultsAreKept_AndAWorseOneIsLeftOut()
        {
            var progress = new ChallengeProgress();
            for (int i = 0; i < ChallengeProgress.MaxResults; i++) progress.RecordSolve(Id, Result(40 + i, 100f));

            ChallengeSolveOutcome worst = progress.RecordSolve(Id, Result(99, 100f));
            Assert.IsFalse(worst.Stored);
            Assert.AreEqual(ChallengeProgress.MaxResults + 1, worst.Rank);
            Assert.AreEqual(ChallengeProgress.MaxResults, progress.Get(Id).Results.Count);

            ChallengeSolveOutcome best = progress.RecordSolve(Id, Result(30, 100f));
            Assert.IsTrue(best.Stored);
            Assert.AreEqual(ChallengeProgress.MaxResults, progress.Get(Id).Results.Count);
            Assert.AreEqual(30, progress.Get(Id).Best.Moves);
            Assert.AreEqual(48, progress.Get(Id).Results.Last().Moves, "the worst of the old ten dropped out");
        }

        [Test]
        public void AnInvalidResult_OrId_IsRejected()
        {
            var progress = new ChallengeProgress();

            Assert.Throws<ArgumentException>(() => progress.RecordSolve(Id, Result(0, 10f)));
            Assert.Throws<ArgumentException>(() => progress.RecordSolve(Id, Result(5, float.NaN)));
            Assert.Throws<ArgumentException>(() => progress.RecordSolve(Id, Result(5, -1f)));
            Assert.Throws<ArgumentException>(() => progress.RecordSolve(Id, Result(5, 10f, pings: -1)));
            Assert.Throws<ArgumentException>(() => progress.AddAttempt("Bad Id"));
            Assert.Throws<ArgumentException>(() => progress.Get(null));
        }

        [Test]
        public void Restore_SortsCapsAndDropsWhatMakesNoSense()
        {
            var progress = new ChallengeProgress();
            var saved = Enumerable.Range(0, 14).Select(i => Result(100 - i, 50f))
                .Concat(new[] { Result(0, 1f), Result(5, float.NaN) }).ToArray();

            progress.Restore(Id, -3, saved);

            ChallengeRecord record = progress.Get(Id);
            Assert.AreEqual(0, record.Attempts, "negative attempts become 0");
            Assert.AreEqual(ChallengeProgress.MaxResults, record.Results.Count);
            Assert.AreEqual(87, record.Best.Moves, "sorted best first: the lowest moves of the valid ones");
        }

        [Test]
        public void Records_AreOrderedById()
        {
            var progress = new ChallengeProgress();
            progress.AddAttempt("b-2");
            progress.AddAttempt("a-1");

            CollectionAssert.AreEqual(new[] { "a-1", "b-2" }, progress.Records.Select(r => r.ChallengeId).ToArray());
        }

        [Test]
        public void ARecordReadBefore_IsASnapshot()
        {
            var progress = new ChallengeProgress();
            progress.RecordSolve(Id, Result(50, 10f));
            ChallengeRecord before = progress.Get(Id);

            progress.RecordSolve(Id, Result(40, 10f));

            Assert.AreEqual(1, before.Results.Count);
        }
    }
}
