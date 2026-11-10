using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeAccessTests
    {
        private static readonly int[] Ids = { 1, 2, 3 };

        private static PlayerProgress Won(params int[] levelIds)
        {
            var progress = new PlayerProgress();
            foreach (int id in levelIds) progress.RecordWin(id, 1, 5);
            return progress;
        }

        [Test]
        public void AChallenge_OpensWhenEnoughLevelsAreWon_NotBefore()
        {
            Assert.IsFalse(ChallengeAccess.IsUnlocked(Won(), Ids, 1));
            Assert.IsTrue(ChallengeAccess.IsUnlocked(Won(1), Ids, 1));
            Assert.IsFalse(ChallengeAccess.IsUnlocked(Won(1), Ids, 2));
            Assert.IsTrue(ChallengeAccess.IsUnlocked(Won(1, 3), Ids, 2));
        }

        [Test]
        public void WhichLevelsWereWon_DoesNotMatter_OnlyHowMany()
        {
            Assert.IsTrue(ChallengeAccess.IsUnlocked(Won(3), Ids, 1));
            Assert.AreEqual(2, ChallengeAccess.LevelsWon(Won(2, 3), Ids));
        }

        [Test]
        public void AnyNumberOfStars_CountsAsWon()
        {
            var progress = new PlayerProgress();
            progress.RecordWin(1, 1, 9);
            progress.RecordWin(2, 2, 7);

            Assert.IsTrue(ChallengeAccess.IsUnlocked(progress, Ids, 2));
        }

        [Test]
        public void ALevelThatIsNotInTheCatalog_DoesNotCount()
        {
            Assert.IsFalse(ChallengeAccess.IsUnlocked(Won(99), Ids, 1));
            Assert.AreEqual(0, ChallengeAccess.LevelsWon(Won(99), Ids));
        }

        [Test]
        public void AThresholdAboveTheLevelCount_IsCappedAtIt_SoTheChallengeCanBeReached()
        {
            Assert.IsFalse(ChallengeAccess.IsUnlocked(Won(1, 2), Ids, 10));
            Assert.IsTrue(ChallengeAccess.IsUnlocked(Won(1, 2, 3), Ids, 10));
            Assert.AreEqual(1, ChallengeAccess.LevelsLeft(Won(1, 2), Ids, 10));
        }

        [Test]
        public void AThresholdOfZero_IsOpenFromTheStart_ButNotInAGameWithNoLevels()
        {
            Assert.IsTrue(ChallengeAccess.IsUnlocked(Won(), Ids, 0));
            Assert.IsFalse(ChallengeAccess.IsUnlocked(Won(1), new int[0], 0));
        }

        [Test]
        public void LevelsLeft_CountsWhatIsStillToWinForThatChallenge()
        {
            Assert.AreEqual(5, ChallengeAccess.LevelsLeft(Won(), new[] { 1, 2, 3, 4, 5, 6 }, 5));
            Assert.AreEqual(3, ChallengeAccess.LevelsLeft(Won(1, 3), new[] { 1, 2, 3, 4, 5, 6 }, 5));
            Assert.AreEqual(0, ChallengeAccess.LevelsLeft(Won(1, 2, 3, 4, 5), new[] { 1, 2, 3, 4, 5, 6 }, 5));
            Assert.AreEqual(0, ChallengeAccess.LevelsLeft(Won(), new int[0], 3));
        }

        [Test]
        public void UnlockedCount_CountsTheChallengesThatAreOpen()
        {
            var ids = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var thresholds = new[] { 1, 5, 10 };

            Assert.AreEqual(0, ChallengeAccess.UnlockedCount(Won(), ids, thresholds));
            Assert.AreEqual(1, ChallengeAccess.UnlockedCount(Won(1), ids, thresholds));
            Assert.AreEqual(2, ChallengeAccess.UnlockedCount(Won(1, 2, 3, 4, 5), ids, thresholds));
            Assert.AreEqual(3, ChallengeAccess.UnlockedCount(Won(1, 2, 3, 4, 5, 6, 7, 8, 9, 10), ids, thresholds));
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.IsUnlocked(null, Ids, 1));
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.IsUnlocked(Won(), null, 1));
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.LevelsLeft(null, Ids, 1));
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.LevelsWon(Won(), null));
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.UnlockedCount(Won(), Ids, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChallengeAccess.IsUnlocked(Won(), Ids, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChallengeAccess.LevelsLeft(Won(), Ids, -1));
        }
    }
}
