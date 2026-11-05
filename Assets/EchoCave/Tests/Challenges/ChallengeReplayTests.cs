using System;
using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeReplayTests
    {
        private static readonly MoveResolver Resolver = new MoveResolver();

        private static BatDefinition Bat(int x, int y, Direction direction, bool hidden = false)
        {
            return new BatDefinition(new Cell(x, y), direction, hidden);
        }

        // A 3x1 board: bat 1 (middle) leaves to the right, bat 0 (left) points the same way and is blocked by bat 1.
        private static LevelDefinition Chain(int moveLimit = 0, int pings = 0, bool hidden = false)
        {
            return new LevelDefinition(3, 1, new[] { Bat(0, 0, Direction.Right, hidden), Bat(1, 0, Direction.Right) },
                moveLimit, pings, 1, 1);
        }

        private static ChallengeAttempt Attempt(float seconds = 10f, int attempts = 1, string id = "challenge-001", params ChallengeMove[] moves)
        {
            return new ChallengeAttempt(id, moves, seconds, attempts, 1000);
        }

        private static ChallengeMove Tap(int id) => ChallengeMove.Tap(id);

        private static ChallengeMove Ping() => ChallengeMove.Ping();

        private static ReplayResult Verify(LevelDefinition level, ChallengeAttempt attempt, float minSecondsPerTap = 0f)
        {
            return ChallengeReplay.Verify(level, attempt, Resolver, minSecondsPerTap);
        }

        [Test]
        public void ThePerfectOrder_IsValid_AndCountsEveryTap()
        {
            ReplayResult result = Verify(Chain(), Attempt(moves: new[] { Tap(1), Tap(0) }));

            Assert.IsTrue(result.IsValid, result.Message);
            Assert.AreEqual(2, result.Moves);
            Assert.AreEqual(0, result.Pings);
        }

        [Test]
        public void ABounce_CostsAMove_ButStillWins()
        {
            ReplayResult result = Verify(Chain(), Attempt(moves: new[] { Tap(0), Tap(1), Tap(0) }));

            Assert.IsTrue(result.IsValid, result.Message);
            Assert.AreEqual(3, result.Moves);
        }

        [Test]
        public void ClearingTheBoardOnTheLastAllowedMove_IsAWin()
        {
            Assert.IsTrue(Verify(Chain(moveLimit: 2), Attempt(moves: new[] { Tap(1), Tap(0) })).IsValid);
            Assert.IsTrue(Verify(Chain(moveLimit: 3), Attempt(moves: new[] { Tap(0), Tap(1), Tap(0) })).IsValid);
        }

        [Test]
        public void ATapAfterTheMoveLimitRanOut_IsRejected()
        {
            // Two bounces use the limit of 2 with bats left: the level was lost there, so nothing can follow.
            ReplayResult result = Verify(Chain(moveLimit: 2), Attempt(moves: new[] { Tap(0), Tap(0), Tap(1), Tap(0) }));

            Assert.AreEqual(ReplayError.MovesAfterLoss, result.Error);
        }

        [Test]
        public void ATapAfterTheBoardWasCleared_IsRejected()
        {
            ReplayResult result = Verify(Chain(), Attempt(moves: new[] { Tap(1), Tap(0), Tap(0) }));

            Assert.AreEqual(ReplayError.MovesAfterWin, result.Error);
        }

        [Test]
        public void ATapOnABatThatIsNotThere_IsRejected()
        {
            Assert.AreEqual(ReplayError.UnknownBat, Verify(Chain(), Attempt(moves: new[] { Tap(7) })).Error);
            Assert.AreEqual(ReplayError.UnknownBat, Verify(Chain(), Attempt(moves: new[] { Tap(-1) })).Error);
            Assert.AreEqual(ReplayError.UnknownBat, Verify(Chain(), Attempt(moves: new[] { Tap(1), Tap(1) })).Error, "bat 1 already left");
        }

        [Test]
        public void AnAttemptThatEndsWithBatsLeft_OrWithNoMoves_IsNotSolved()
        {
            Assert.AreEqual(ReplayError.NotSolved, Verify(Chain(), Attempt(moves: new[] { Tap(1) })).Error);
            Assert.AreEqual(ReplayError.NotSolved, Verify(Chain(), Attempt(moves: new ChallengeMove[0])).Error);
            Assert.AreEqual(ReplayError.NotSolved, Verify(Chain(), Attempt(moves: new[] { Tap(0), Tap(0) })).Error, "bounces only");
        }

        [Test]
        public void Pings_FitTheBudget_AndAreNotMoves()
        {
            LevelDefinition level = Chain(pings: 2, hidden: true);

            ReplayResult result = Verify(level, Attempt(moves: new[] { Ping(), Tap(1), Ping(), Tap(0) }));

            Assert.IsTrue(result.IsValid, result.Message);
            Assert.AreEqual(2, result.Moves, "pings are not taps");
            Assert.AreEqual(2, result.Pings);
        }

        [Test]
        public void AnExtraPing_IsRejected()
        {
            LevelDefinition level = Chain(pings: 1, hidden: true);

            ReplayResult result = Verify(level, Attempt(moves: new[] { Ping(), Ping(), Tap(1), Tap(0) }));

            Assert.AreEqual(ReplayError.TooManyPings, result.Error);
            Assert.AreEqual(ReplayError.TooManyPings, Verify(Chain(pings: 0, hidden: true), Attempt(moves: new[] { Ping(), Tap(1), Tap(0) })).Error);
        }

        [Test]
        public void APing_WithNoHiddenBatOnTheBoard_IsRejected()
        {
            Assert.AreEqual(ReplayError.PingWithNothingToReveal,
                Verify(Chain(pings: 1, hidden: false), Attempt(moves: new[] { Ping(), Tap(1), Tap(0) })).Error, "no hidden bat at all");

            // The hidden bat (id 0) is still there while bat 1 leaves; once it has left there is nothing to reveal, but
            // then the board is empty and any move is already rejected as coming after the win.
            LevelDefinition twoHidden = new LevelDefinition(3, 1, new[] { Bat(0, 0, Direction.Right, true), Bat(1, 0, Direction.Right, true) }, 0, 2, 1, 1);
            Assert.IsTrue(Verify(twoHidden, Attempt(moves: new[] { Ping(), Tap(1), Tap(0) })).IsValid);
            Assert.AreEqual(ReplayError.PingWithNothingToReveal,
                Verify(Chain(pings: 2, hidden: false), Attempt(moves: new[] { Tap(1), Ping(), Tap(0) })).Error);
        }

        [Test]
        public void ATimeShorterThanAPersonCanTap_IsRejected_WhenTheCheckIsOn()
        {
            ChallengeAttempt fast = Attempt(seconds: 0.4f, moves: new[] { Tap(1), Tap(0) });

            Assert.AreEqual(ReplayError.ImpossibleTime, Verify(Chain(), fast, minSecondsPerTap: 0.5f).Error);
            Assert.IsTrue(Verify(Chain(), fast, minSecondsPerTap: 0f).IsValid, "the check can be turned off");
            Assert.IsTrue(Verify(Chain(), Attempt(seconds: 1.0f, moves: new[] { Tap(1), Tap(0) }), minSecondsPerTap: 0.5f).IsValid, "exactly the minimum is allowed");
        }

        [Test]
        public void TheTimeCheck_CountsBouncesToo()
        {
            ChallengeAttempt attempt = Attempt(seconds: 1.0f, moves: new[] { Tap(0), Tap(1), Tap(0) });

            Assert.AreEqual(ReplayError.ImpossibleTime, Verify(Chain(), attempt, minSecondsPerTap: 0.5f).Error);
        }

        [Test]
        public void BadStats_AreRejected_WithoutThrowing()
        {
            var moves = new[] { Tap(1), Tap(0) };

            Assert.AreEqual(ReplayError.BadStats, Verify(Chain(), Attempt(seconds: float.NaN, moves: moves)).Error);
            Assert.AreEqual(ReplayError.BadStats, Verify(Chain(), Attempt(seconds: -1f, moves: moves)).Error);
            Assert.AreEqual(ReplayError.BadStats, Verify(Chain(), Attempt(seconds: float.PositiveInfinity, moves: moves)).Error);
            Assert.AreEqual(ReplayError.BadStats, Verify(Chain(), Attempt(attempts: 0, moves: moves)).Error);
            Assert.AreEqual(ReplayError.BadStats, Verify(Chain(), Attempt(id: "", moves: moves)).Error);
        }

        [Test]
        public void AnAbsurdlyLongList_IsRejectedBeforeItIsPlayed()
        {
            var moves = Enumerable.Repeat(Tap(0), ChallengeReplay.MaxMoves + 1).ToArray();

            Assert.AreEqual(ReplayError.TooManyMoves, Verify(Chain(), Attempt(moves: moves)).Error);
        }

        [Test]
        public void ARejectedAttempt_ReportsNoMoves_AndAReasonToLog()
        {
            ReplayResult result = Verify(Chain(), Attempt(moves: new[] { Tap(1) }));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(0, result.Moves);
            Assert.IsNotEmpty(result.Message);
        }

        [Test]
        public void ReplayingTwice_GivesTheSameAnswer_AndLeavesTheLevelAlone()
        {
            LevelDefinition level = Chain();
            ChallengeAttempt attempt = Attempt(moves: new[] { Tap(0), Tap(1), Tap(0) });

            ReplayResult first = Verify(level, attempt);
            ReplayResult second = Verify(level, attempt);

            Assert.AreEqual(first.Moves, second.Moves);
            Assert.AreEqual(2, level.Bats.Count);
        }

        [Test]
        public void AnInvalidLevel_ThrowsInsteadOfBlamingTheAttempt()
        {
            var empty = new LevelDefinition(3, 1, new BatDefinition[0], 0, 0, 1, 1);

            Assert.Throws<InvalidLevelException>(() => Verify(empty, Attempt(moves: new[] { Tap(0) })));
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ChallengeReplay.Verify(null, Attempt(), Resolver, 0f));
            Assert.Throws<ArgumentNullException>(() => ChallengeReplay.Verify(Chain(), null, Resolver, 0f));
            Assert.Throws<ArgumentNullException>(() => ChallengeReplay.Verify(Chain(), Attempt(), null, 0f));
            Assert.Throws<ArgumentNullException>(() => new ChallengeAttempt(null, new ChallengeMove[0], 1f, 1, 0));
            Assert.Throws<ArgumentNullException>(() => new ChallengeAttempt("x", null, 1f, 1, 0));
        }
    }
}
