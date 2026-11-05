using System;
using System.Linq;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LocalChallengeLeaderboardTests
    {
        private const string Id = "challenge-001";

        private FakeSaveService _save;
        private LocalChallengeLeaderboard _board;

        [SetUp]
        public void SetUp()
        {
            _save = new FakeSaveService(3);
            _board = new LocalChallengeLeaderboard(_save);
        }

        private void Solve(int moves, float seconds) => _save.RecordChallengeSolve(Id, new ChallengeResult(moves, seconds, 1, 1000));

        [Test]
        public void ANeverSolvedChallenge_HasNoResults()
        {
            Assert.IsEmpty(_board.Top(Id, 3));
        }

        [Test]
        public void TheTop_IsTheBestFirst_AndNoLongerThanAsked()
        {
            Solve(60, 10f);
            Solve(45, 10f);
            Solve(52, 10f);
            Solve(70, 10f);

            Assert.AreEqual(new[] { 45, 52, 60 }, _board.Top(Id, 3).Select(r => r.Moves).ToArray());
            Assert.AreEqual(4, _board.Top(Id, 10).Count);
            Assert.AreEqual(0, _board.Top(Id, 0).Count);
        }

        [Test]
        public void TheTop_FollowsTheSave_NotACopy()
        {
            Solve(52, 10f);
            Assert.AreEqual(1, _board.Top(Id, 3).Count);

            Solve(40, 10f);

            Assert.AreEqual(40, _board.Top(Id, 3)[0].Moves);
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _board.Top(Id, -1));
            Assert.Throws<ArgumentNullException>(() => new LocalChallengeLeaderboard(null));
        }

        [Test]
        public void TheNullLeaderboard_HasNothing()
        {
            Assert.IsEmpty(new NullChallengeLeaderboard().Top(Id, 5));
        }
    }
}
