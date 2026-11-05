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
        public void TheChallenges_AreClosed_UntilEveryNumberedLevelIsWon()
        {
            Assert.IsFalse(ChallengeAccess.IsUnlocked(Won(), Ids));
            Assert.IsFalse(ChallengeAccess.IsUnlocked(Won(1, 2), Ids));
            Assert.IsTrue(ChallengeAccess.IsUnlocked(Won(1, 2, 3), Ids));
        }

        [Test]
        public void AnyNumberOfStars_CountsAsWon()
        {
            var progress = new PlayerProgress();
            progress.RecordWin(1, 1, 9);
            progress.RecordWin(2, 2, 7);
            progress.RecordWin(3, 3, 5);

            Assert.IsTrue(ChallengeAccess.IsUnlocked(progress, Ids));
        }

        [Test]
        public void ANewLevelAtTheEnd_ClosesThemAgain_UntilItIsWon()
        {
            PlayerProgress progress = Won(1, 2, 3);

            Assert.IsFalse(ChallengeAccess.IsUnlocked(progress, new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void AGameWithNoLevels_NeverUnlocksThem()
        {
            Assert.IsFalse(ChallengeAccess.IsUnlocked(Won(1), new int[0]));
        }

        [Test]
        public void LevelsLeft_CountsWhatIsStillToWin()
        {
            Assert.AreEqual(3, ChallengeAccess.LevelsLeft(Won(), Ids));
            Assert.AreEqual(1, ChallengeAccess.LevelsLeft(Won(1, 3), Ids));
            Assert.AreEqual(0, ChallengeAccess.LevelsLeft(Won(1, 2, 3), Ids));
            Assert.AreEqual(0, ChallengeAccess.LevelsLeft(Won(), new int[0]));
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.IsUnlocked(null, Ids));
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.IsUnlocked(Won(), null));
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.LevelsLeft(null, Ids));
            Assert.Throws<ArgumentNullException>(() => ChallengeAccess.LevelsLeft(Won(), null));
        }
    }
}
