using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class StreakRewardTests
    {
        private static RewardMoment NewThreeStar => RewardMoment.Won(3, false);

        private static StreakReward Streak(int length = 3, int perStreak = 1, int saved = 0) => new StreakReward(length, perStreak, saved);

        [Test]
        public void TheThirdNewThreeStarWinInARow_EarnsTheHint_AndTheCountStartsAgain()
        {
            StreakReward reward = Streak();

            Assert.AreEqual(0, reward.Evaluate(NewThreeStar));
            Assert.AreEqual(0, reward.Evaluate(NewThreeStar));
            Assert.AreEqual(1, reward.Evaluate(NewThreeStar));
            Assert.AreEqual(0, reward.Streak);
            Assert.AreEqual(0, reward.Evaluate(NewThreeStar), "the next streak starts from zero");
            Assert.AreEqual(1, reward.Streak);
        }

        [Test]
        public void AWinWithFewerThanThreeStars_BreaksTheStreak()
        {
            StreakReward reward = Streak();
            reward.Evaluate(NewThreeStar);
            reward.Evaluate(NewThreeStar);

            reward.Evaluate(RewardMoment.Won(2, false));

            Assert.AreEqual(0, reward.Streak);
            Assert.AreEqual(0, reward.Evaluate(NewThreeStar));
            Assert.AreEqual(0, reward.Evaluate(NewThreeStar));
            Assert.AreEqual(1, reward.Evaluate(NewThreeStar), "three more were needed after the break");
        }

        [Test]
        public void ALostLevel_BreaksTheStreak()
        {
            StreakReward reward = Streak();
            reward.Evaluate(NewThreeStar);
            reward.Evaluate(NewThreeStar);

            Assert.AreEqual(0, reward.Evaluate(RewardMoment.Lost()));

            Assert.AreEqual(0, reward.Streak);
        }

        [Test]
        public void ReplayingALevelThatAlreadyHasThreeStars_CountsForNothing_AndBreaksNothing()
        {
            StreakReward reward = Streak();
            reward.Evaluate(NewThreeStar);
            reward.Evaluate(NewThreeStar);

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(0, reward.Evaluate(RewardMoment.Won(3, true)), "a replay must never pay: that would be farming");

            Assert.AreEqual(2, reward.Streak);
            Assert.AreEqual(1, reward.Evaluate(NewThreeStar));
        }

        [Test]
        public void ALessThanThreeStarReplay_StillBreaksTheStreak()
        {
            StreakReward reward = Streak();
            reward.Evaluate(NewThreeStar);

            reward.Evaluate(RewardMoment.Won(1, true));

            Assert.AreEqual(0, reward.Streak);
        }

        [Test]
        public void TheDayStarting_ChangesNothing()
        {
            StreakReward reward = Streak();
            reward.Evaluate(NewThreeStar);

            Assert.AreEqual(0, reward.Evaluate(RewardMoment.DayStarted(5)));

            Assert.AreEqual(1, reward.Streak);
        }

        [Test]
        public void AStreakOfOne_PaysOnEveryNewThreeStarWin_AndAStreakCanPayMoreThanOneHint()
        {
            StreakReward reward = Streak(length: 1, perStreak: 2);

            Assert.AreEqual(2, reward.Evaluate(NewThreeStar));
            Assert.AreEqual(2, reward.Evaluate(NewThreeStar));
        }

        [Test]
        public void TheSavedStreak_IsPickedUp_AndCutToFitAShorterStreakLength()
        {
            Assert.AreEqual(2, Streak(length: 3, saved: 2).Streak);
            Assert.AreEqual(1, Streak(length: 2, saved: 9).Streak, "a saved 9 cannot exceed length - 1");
            Assert.AreEqual(0, Streak(saved: -4).Streak);
        }

        [Test]
        public void TheStateAndKey_AreWhatTheSaveStores()
        {
            StreakReward reward = Streak();
            reward.Evaluate(NewThreeStar);

            Assert.AreEqual("streak", reward.Key);
            Assert.AreEqual(1, reward.State);
            Assert.AreEqual(HintRewardSource.Streak, reward.Source);
        }

        [Test]
        public void WhileTheHintsAreFull_TheStreakIsOnStandby_ItIgnoresWinsAndStaysAtZero()
        {
            StreakReward reward = Streak();
            reward.Evaluate(NewThreeStar);
            reward.Evaluate(NewThreeStar);

            for (int i = 0; i < 6; i++)
                Assert.AreEqual(0, reward.Evaluate(RewardMoment.Won(3, false, hintsFull: true)), "nothing can be stored, so nothing is earned");

            Assert.AreEqual(0, reward.Streak, "standby also drops the count that was running");
        }

        [Test]
        public void WhenRoomOpensUp_TheCycleStartsAgainFromZero()
        {
            StreakReward reward = Streak();
            reward.Evaluate(RewardMoment.Won(3, false, hintsFull: true));
            reward.Evaluate(RewardMoment.Won(3, false, hintsFull: true));
            reward.Evaluate(RewardMoment.Won(3, false, hintsFull: true));

            Assert.AreEqual(0, reward.Evaluate(NewThreeStar));
            Assert.AreEqual(0, reward.Evaluate(NewThreeStar));
            Assert.AreEqual(1, reward.Evaluate(NewThreeStar), "three wins after the standby, not one");
        }

        [Test]
        public void ALossOrAWeakWin_DuringStandby_ChangesNothing()
        {
            StreakReward reward = Streak();

            Assert.AreEqual(0, reward.Evaluate(RewardMoment.Lost(hintsFull: true)));
            Assert.AreEqual(0, reward.Evaluate(RewardMoment.Won(1, false, hintsFull: true)));

            Assert.AreEqual(0, reward.Streak);
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Streak(length: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Streak(perStreak: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RewardMoment.Won(0, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => RewardMoment.Won(4, false));
        }
    }
}
