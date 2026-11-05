using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeDifficultyTests
    {
        private static readonly MoveResolver Resolver = new MoveResolver();

        private static BatDefinition Bat(int x, int y, Direction direction, bool hidden = false)
        {
            return new BatDefinition(new Cell(x, y), direction, hidden);
        }

        private static LevelDefinition Level(int width, int height, params BatDefinition[] bats)
        {
            return new LevelDefinition(width, height, bats, 0, 1, 1, 1);
        }

        // Two bats in a row, both pointing right: bat 1 (middle) is free, bat 0 is blocked by it.
        private static LevelDefinition TwoInARow(bool hidden0, bool hidden1)
        {
            return Level(3, 1, Bat(0, 0, Direction.Right, hidden0), Bat(1, 0, Direction.Right, hidden1));
        }

        private static ChallengeDifficulty Measure(LevelDefinition level, int trials = 4000, int seed = 7)
        {
            Assert.IsTrue(ChallengeDifficulty.TryMeasure(level, Resolver, trials, seed, out ChallengeDifficulty d));
            return d;
        }

        [Test]
        public void ALevelWithNoHiddenBats_WastesNothing()
        {
            ChallengeDifficulty d = Measure(TwoInARow(false, false));

            Assert.AreEqual(0.0, d.ExpectedBlindBounces);
            Assert.AreEqual(1.0, d.ChanceOfFlawlessBlindRun);
            Assert.AreEqual(0, d.Hidden);
        }

        [Test]
        public void AHiddenBatThatIsBlockedByAVisibleOne_IsNotATrap()
        {
            // The blind player taps the visible free bat first, so the hidden one is free by the time it is tapped.
            ChallengeDifficulty d = Measure(TwoInARow(hidden0: true, hidden1: false));

            Assert.AreEqual(0.0, d.ExpectedBlindBounces);
            Assert.AreEqual(1, d.Hidden);
        }

        [Test]
        public void TwoHiddenBatsInARow_AreACoinFlip()
        {
            // Both arrows unknown: a random pick is the blocked bat half of the time, and then it costs one bounce.
            ChallengeDifficulty d = Measure(TwoInARow(true, true));

            Assert.AreEqual(0.5, d.ExpectedBlindBounces, 0.05);
            Assert.AreEqual(0.5, d.ChanceOfFlawlessBlindRun, 0.05);
        }

        [Test]
        public void MoreHiddenBatsInAChain_MakeALuckyRunLessLikely()
        {
            // Four hidden bats in a row all pointing right: only the last one is free.
            var chain = Level(5, 1, Bat(0, 0, Direction.Right, true), Bat(1, 0, Direction.Right, true),
                Bat(2, 0, Direction.Right, true), Bat(3, 0, Direction.Right, true));
            ChallengeDifficulty two = Measure(TwoInARow(true, true));
            ChallengeDifficulty four = Measure(chain);

            Assert.Less(four.ChanceOfFlawlessBlindRun, two.ChanceOfFlawlessBlindRun);
            Assert.Greater(four.ExpectedBlindBounces, two.ExpectedBlindBounces);
            Assert.Greater(four.Score, two.Score);
            Assert.AreEqual(4, four.Waves);
        }

        [Test]
        public void TheSameLevelTrialsAndSeed_GiveTheSameNumbers()
        {
            LevelDefinition level = TwoInARow(true, true);

            ChallengeDifficulty a = Measure(level, 500, 42);
            ChallengeDifficulty b = Measure(level, 500, 42);

            Assert.AreEqual(a.ExpectedBlindBounces, b.ExpectedBlindBounces);
            Assert.AreEqual(a.ChanceOfFlawlessBlindRun, b.ChanceOfFlawlessBlindRun);
        }

        [Test]
        public void AFreeHiddenBat_IsNeverATrap()
        {
            // A lone hidden bat that points out of the board: the only bat there is, so the blind player taps it and leaves.
            ChallengeDifficulty d = Measure(Level(3, 1, Bat(2, 0, Direction.Right, true)));

            Assert.AreEqual(0.0, d.ExpectedBlindBounces);
        }

        [Test]
        public void TheReportCarriesTheLevelsOwnNumbers()
        {
            ChallengeDifficulty d = Measure(TwoInARow(true, false), trials: 10);

            Assert.AreEqual(2, d.Bats);
            Assert.AreEqual(1, d.Hidden);
            Assert.AreEqual(1, d.PingBudget);
            Assert.AreEqual(2, d.Waves);
            Assert.AreEqual(10, d.Trials);
        }

        [Test]
        public void ALevelThatCannotBeSolved_OrIsInvalid_IsNotMeasured()
        {
            var deadlock = Level(2, 1, Bat(0, 0, Direction.Right), Bat(1, 0, Direction.Left));
            var empty = Level(3, 1);

            Assert.IsFalse(ChallengeDifficulty.TryMeasure(deadlock, Resolver, 10, 1, out _));
            Assert.IsFalse(ChallengeDifficulty.TryMeasure(empty, Resolver, 10, 1, out _));
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            LevelDefinition level = TwoInARow(false, false);

            Assert.Throws<ArgumentOutOfRangeException>(() => ChallengeDifficulty.TryMeasure(level, Resolver, 0, 1, out _));
            Assert.Throws<ArgumentNullException>(() => ChallengeDifficulty.TryMeasure(null, Resolver, 1, 1, out _));
            Assert.Throws<ArgumentNullException>(() => ChallengeDifficulty.TryMeasure(level, null, 1, 1, out _));
        }
    }
}
