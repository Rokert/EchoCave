using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class DailyRewardTests
    {
        private static RewardMoment Day(int day) => RewardMoment.DayStarted(day);

        [Test]
        public void TheFirstDayEver_PaysNothing_ButIsRemembered()
        {
            var reward = new DailyReward(1, 0);

            Assert.AreEqual(0, reward.Evaluate(Day(1000)), "a new player already starts with hints");
            Assert.AreEqual(1000, reward.LastDay);
        }

        [Test]
        public void ANewDay_Pays_AndTheSameDayAgain_DoesNot()
        {
            var reward = new DailyReward(2, 1000);

            Assert.AreEqual(2, reward.Evaluate(Day(1001)));
            Assert.AreEqual(0, reward.Evaluate(Day(1001)), "opening the game twice in one day pays once");
            Assert.AreEqual(2, reward.Evaluate(Day(1002)));
        }

        [Test]
        public void SkippingDays_PaysOnlyOneGift_NotOnePerMissedDay()
        {
            var reward = new DailyReward(1, 1000);

            Assert.AreEqual(1, reward.Evaluate(Day(1010)));
            Assert.AreEqual(1010, reward.LastDay);
        }

        [Test]
        public void AnEarlierDay_Pays_SoAWrongClockCannotLockTheGift()
        {
            var reward = new DailyReward(1, 5000);

            Assert.AreEqual(1, reward.Evaluate(Day(1001)));
            Assert.AreEqual(1001, reward.LastDay);
        }

        [Test]
        public void MomentsThatAreNotADay_ChangeNothing()
        {
            var reward = new DailyReward(1, 1000);

            Assert.AreEqual(0, reward.Evaluate(RewardMoment.Won(3, false)));
            Assert.AreEqual(0, reward.Evaluate(RewardMoment.Lost()));
            Assert.AreEqual(1000, reward.LastDay);
        }

        [Test]
        public void TheStateAndKey_AreWhatTheSaveStores()
        {
            var reward = new DailyReward(1, 1000);

            Assert.AreEqual("dailyDay", reward.Key);
            Assert.AreEqual(1000, reward.State);
            Assert.AreEqual(HintRewardSource.Daily, reward.Source);
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DailyReward(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RewardMoment.DayStarted(0));
        }
    }
}
