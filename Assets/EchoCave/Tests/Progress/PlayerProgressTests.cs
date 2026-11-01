using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class PlayerProgressTests
    {
        [Test]
        public void ANewProgress_HasWonNothing()
        {
            var progress = new PlayerProgress();

            Assert.AreEqual(0, progress.CompletedCount);
            Assert.AreEqual(0, progress.TotalStars);
            Assert.IsFalse(progress.IsCompleted(0));
            Assert.AreEqual(0, progress.StarsOf(0));
            Assert.AreEqual(0, progress.BestMovesOf(0));
            Assert.IsEmpty(progress.Records);
        }

        [Test]
        public void TheFirstWin_IsRecorded()
        {
            var progress = new PlayerProgress();

            bool improved = progress.RecordWin(2, 2, 7);

            Assert.IsTrue(improved);
            Assert.IsTrue(progress.IsCompleted(2));
            Assert.AreEqual(2, progress.StarsOf(2));
            Assert.AreEqual(7, progress.BestMovesOf(2));
        }

        [Test]
        public void ABetterWin_RaisesTheStars_AndFewerMovesLowerTheBest()
        {
            var progress = new PlayerProgress();
            progress.RecordWin(0, 1, 9);

            Assert.IsTrue(progress.RecordWin(0, 3, 6));

            Assert.AreEqual(3, progress.StarsOf(0));
            Assert.AreEqual(6, progress.BestMovesOf(0));
        }

        [Test]
        public void AWorseWin_ChangesNothing_AndReportsNoImprovement()
        {
            var progress = new PlayerProgress();
            progress.RecordWin(0, 3, 6);

            Assert.IsFalse(progress.RecordWin(0, 1, 9));

            Assert.AreEqual(3, progress.StarsOf(0));
            Assert.AreEqual(6, progress.BestMovesOf(0));
        }

        [Test]
        public void StarsAndMoves_ImproveIndependently()
        {
            var progress = new PlayerProgress();
            progress.RecordWin(0, 2, 8);

            // Fewer moves but fewer stars is impossible in play, yet the rule must not let one drag the other down.
            Assert.IsTrue(progress.RecordWin(0, 1, 5));

            Assert.AreEqual(2, progress.StarsOf(0));
            Assert.AreEqual(5, progress.BestMovesOf(0));
        }

        [Test]
        public void TheTotals_CountEveryLevelWon()
        {
            var progress = new PlayerProgress();
            progress.RecordWin(0, 3, 6);
            progress.RecordWin(4, 1, 12);
            progress.RecordWin(2, 2, 8);

            Assert.AreEqual(3, progress.CompletedCount);
            Assert.AreEqual(6, progress.TotalStars);
        }

        [Test]
        public void TheRecords_AreOrderedByLevelIndex()
        {
            var progress = new PlayerProgress();
            progress.RecordWin(4, 1, 12);
            progress.RecordWin(0, 3, 6);
            progress.RecordWin(2, 2, 8);

            Assert.AreEqual(new[] { 0, 2, 4 }, new[] { progress.Records[0].Index, progress.Records[1].Index, progress.Records[2].Index });
        }

        [Test]
        public void ImpossibleResults_AreRejected()
        {
            var progress = new PlayerProgress();

            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordWin(-1, 1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordWin(0, 0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordWin(0, 4, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordWin(0, 1, -1));
            Assert.AreEqual(0, progress.CompletedCount);
        }

        [Test]
        public void ARecord_IsValidOnlyWithinTheRules()
        {
            Assert.IsTrue(new LevelRecord(0, 1, 0).IsValid);
            Assert.IsTrue(new LevelRecord(9, 3, 20).IsValid);
            Assert.IsFalse(new LevelRecord(-1, 2, 5).IsValid);
            Assert.IsFalse(new LevelRecord(0, 0, 5).IsValid);
            Assert.IsFalse(new LevelRecord(0, 4, 5).IsValid);
            Assert.IsFalse(new LevelRecord(0, 2, -3).IsValid);
        }
    }
}
