using System;
using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeScoreTests
    {
        private static ChallengeScore Score(int moves, float seconds, long solvedAt) => new ChallengeScore(moves, seconds, solvedAt);

        [Test]
        public void FewerMoves_BeatsEverything_EvenAMuchSlowerTime()
        {
            Assert.IsTrue(Score(12, 900f, 5000).IsBetterThan(Score(13, 10f, 1)));
            Assert.IsFalse(Score(13, 10f, 1).IsBetterThan(Score(12, 900f, 5000)));
        }

        [Test]
        public void WithTheSameMoves_LessTimeWins()
        {
            Assert.IsTrue(Score(12, 40f, 5000).IsBetterThan(Score(12, 41f, 1)));
        }

        [Test]
        public void WithTheSameMovesAndTime_TheEarlierSolveWins()
        {
            Assert.IsTrue(Score(12, 40f, 1000).IsBetterThan(Score(12, 40f, 1001)));
            Assert.IsFalse(Score(12, 40f, 1001).IsBetterThan(Score(12, 40f, 1000)));
        }

        [Test]
        public void IdenticalScores_TieInEveryWay()
        {
            Assert.AreEqual(0, Score(12, 40f, 1000).CompareTo(Score(12, 40f, 1000)));
            Assert.IsFalse(Score(12, 40f, 1000).IsBetterThan(Score(12, 40f, 1000)));
        }

        [Test]
        public void Sorting_PutsTheBestFirst()
        {
            var scores = new List<ChallengeScore>
            {
                Score(13, 10f, 1), Score(12, 50f, 3), Score(12, 40f, 9), Score(12, 40f, 2)
            };

            scores.Sort();

            Assert.AreEqual(new[] { 2L, 9L, 3L, 1L }, scores.Select(s => s.SolvedAtUnixSeconds).ToArray());
        }

        [Test]
        public void RankOf_IsOneMoreThanTheResultsStrictlyBetter()
        {
            var existing = new[] { Score(12, 40f, 1), Score(12, 50f, 2), Score(14, 20f, 3) };

            Assert.AreEqual(1, ChallengeRanking.RankOf(existing, Score(11, 99f, 9)));
            Assert.AreEqual(2, ChallengeRanking.RankOf(existing, Score(12, 45f, 9)));
            Assert.AreEqual(4, ChallengeRanking.RankOf(existing, Score(20, 1f, 9)));
        }

        [Test]
        public void RankOf_AnEmptyBoard_IsFirst_AndATieSharesThePlace()
        {
            Assert.AreEqual(1, ChallengeRanking.RankOf(new ChallengeScore[0], Score(30, 999f, 9)));

            var existing = new[] { Score(12, 40f, 1000) };
            Assert.AreEqual(1, ChallengeRanking.RankOf(existing, Score(12, 40f, 1000)));
        }

        [Test]
        public void RankOf_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => ChallengeRanking.RankOf(null, Score(1, 1f, 1)));
        }
    }
}
