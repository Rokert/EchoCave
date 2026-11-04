using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class HintRewardServiceTests
    {
        private sealed class FakeClock : IDayClock
        {
            public int Today { get; set; } = 1000;
        }

        private EventBus _bus;
        private FakeSaveService _save;
        private FakeClock _clock;
        private HintStock _stock;
        private DailyReward _daily;
        private StreakReward _streak;
        private HintRewardService _service;
        private readonly List<HintRewarded> _rewarded = new List<HintRewarded>();

        [SetUp]
        public void SetUp()
        {
            _rewarded.Clear();
            _bus = new EventBus();
            _bus.Subscribe<HintRewarded>(e => _rewarded.Add(e));
            _save = new FakeSaveService(3);
            _clock = new FakeClock();
            _stock = new HintStock(_save, 5, _bus, 1);
            _daily = new DailyReward(1, 0);
            _streak = new StreakReward(3, 1, 0);
            _service = new HintRewardService(new IHintRewardStrategy[] { _daily, _streak }, _stock, _save, _bus, _clock);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        private void WinNewThreeStar()
        {
            _service.OnLevelWon(3, 0);
        }

        [Test]
        public void ThreeNewThreeStarWins_PayOneHint_IntoTheStock_AndSayWhereItCameFrom()
        {
            WinNewThreeStar();
            WinNewThreeStar();
            Assert.AreEqual(3, _stock.Count);
            Assert.IsEmpty(_rewarded);

            WinNewThreeStar();

            Assert.AreEqual(4, _stock.Count);
            Assert.AreEqual(1, _rewarded.Count);
            Assert.AreEqual(HintRewardSource.Streak, _rewarded[0].Source);
            Assert.AreEqual(1, _rewarded[0].ToStock);
            Assert.AreEqual(0, _rewarded[0].ToReserve);
        }

        [Test]
        public void ALostLevelOnTheBus_BreaksTheStreak()
        {
            WinNewThreeStar();
            WinNewThreeStar();

            _bus.Publish(new LevelLost(0, 9));
            WinNewThreeStar();

            Assert.AreEqual(3, _stock.Count);
            Assert.AreEqual(1, _streak.Streak);
        }

        [Test]
        public void AWinWithFewerThanThreeStars_BreaksTheStreak()
        {
            WinNewThreeStar();
            WinNewThreeStar();

            _service.OnLevelWon(2, 0);
            WinNewThreeStar();

            Assert.AreEqual(3, _stock.Count);
        }

        [Test]
        public void AReplayOfAThreeStarLevel_DoesNotCount()
        {
            for (int i = 0; i < 6; i++) _service.OnLevelWon(3, 3);

            Assert.AreEqual(3, _stock.Count);
            Assert.IsEmpty(_rewarded);
        }

        [Test]
        public void ImprovingALevelFromTwoStarsToThree_CountsAsNew()
        {
            _service.OnLevelWon(3, 2);
            _service.OnLevelWon(3, 1);
            _service.OnLevelWon(3, 0);

            Assert.AreEqual(4, _stock.Count);
        }

        [Test]
        public void TheStreakWithAFullStock_GoesToTheReserve_ThenIsLost()
        {
            _save.SetHints(5);

            WinNewThreeStar();
            WinNewThreeStar();
            WinNewThreeStar();
            Assert.AreEqual(1, _save.ReserveHints);
            Assert.AreEqual(1, _rewarded[0].ToReserve);
            Assert.AreEqual(0, _rewarded[0].ToStock);

            WinNewThreeStar();
            WinNewThreeStar();
            WinNewThreeStar();

            Assert.AreEqual(1, _save.ReserveHints, "a second spare does not fit");
            Assert.AreEqual(1, _rewarded.Count, "a lost hint is not announced as a reward");
        }

        [Test]
        public void WithTheSpareInTheReserve_TheStreakWaits_AndNothingIsLost()
        {
            _save.SetHints(5);
            WinNewThreeStar();
            WinNewThreeStar();
            WinNewThreeStar();
            Assert.AreEqual(1, _save.ReserveHints);
            Assert.AreEqual(1, _rewarded.Count);

            for (int i = 0; i < 6; i++) WinNewThreeStar();

            Assert.AreEqual(0, _streak.Streak, "on standby the wins are not counted");
            Assert.AreEqual(1, _rewarded.Count);
            Assert.AreEqual(1, _save.ReserveHints);
        }

        [Test]
        public void SpendingTheSpare_StartsTheStreakAgainFromZero()
        {
            _save.SetHints(5);
            _save.SetReserveHints(1);
            WinNewThreeStar();
            WinNewThreeStar();

            _stock.TrySpend();
            WinNewThreeStar();
            WinNewThreeStar();
            Assert.AreEqual(0, _rewarded.Count, "two wins since the spare was used are not enough");

            WinNewThreeStar();

            Assert.AreEqual(1, _rewarded.Count);
            Assert.AreEqual(1, _save.ReserveHints);
        }

        [Test]
        public void TheDailyGift_IsNotOnStandby_WhenFull_ItsDayIsStillUsedUp()
        {
            _save.SetHints(5);
            _save.SetReserveHints(1);
            _service.CheckDay();
            _clock.Today = 1001;

            _service.CheckDay();

            Assert.AreEqual(1001, _save.GetRewardState("dailyDay"), "the daily gift does not wait: unchanged by the standby");
            Assert.IsEmpty(_rewarded);
        }

        [Test]
        public void TheFirstCheck_PaysNoGift_ButTheNextDayDoes()
        {
            _service.CheckDay();
            Assert.AreEqual(3, _stock.Count);
            Assert.IsEmpty(_rewarded);

            _clock.Today = 1001;
            _service.CheckDay();

            Assert.AreEqual(4, _stock.Count);
            Assert.AreEqual(1, _rewarded.Count);
            Assert.AreEqual(HintRewardSource.Daily, _rewarded[0].Source);
        }

        [Test]
        public void CheckingTheSameDayTwice_PaysOnce()
        {
            _service.CheckDay();
            _clock.Today = 1001;

            _service.CheckDay();
            _service.CheckDay();

            Assert.AreEqual(4, _stock.Count);
        }

        [Test]
        public void TheStateOfEveryStrategy_IsSavedUnderItsKey()
        {
            _service.CheckDay();
            WinNewThreeStar();

            Assert.AreEqual(1000, _save.GetRewardState("dailyDay"));
            Assert.AreEqual(1, _save.GetRewardState("streak"));
        }

        [Test]
        public void StateThatDidNotChange_IsNotWrittenAgain()
        {
            _service.CheckDay();
            int writes = _save.RewardStateWrites;

            _service.CheckDay();
            _service.OnLevelWon(3, 3);

            Assert.AreEqual(writes, _save.RewardStateWrites);
        }

        [Test]
        public void AfterDispose_ALostLevelIsNotListenedTo()
        {
            WinNewThreeStar();
            _service.Dispose();

            _bus.Publish(new LevelLost(0, 9));

            Assert.AreEqual(1, _streak.Streak);
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            var strategies = new IHintRewardStrategy[0];

            Assert.Throws<ArgumentNullException>(() => new HintRewardService(null, _stock, _save, _bus, _clock));
            Assert.Throws<ArgumentNullException>(() => new HintRewardService(strategies, null, _save, _bus, _clock));
            Assert.Throws<ArgumentNullException>(() => new HintRewardService(strategies, _stock, null, _bus, _clock));
            Assert.Throws<ArgumentNullException>(() => new HintRewardService(strategies, _stock, _save, null, _clock));
            Assert.Throws<ArgumentNullException>(() => new HintRewardService(strategies, _stock, _save, _bus, null));
        }
    }
}
