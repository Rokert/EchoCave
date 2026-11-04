using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    // The recorder hands each win to the reward service together with the stars the level had BEFORE the win was
    // saved. Done after saving, every replay would look like a first win and the streak could be farmed.
    public class ProgressRecorderRewardTests
    {
        private sealed class FixedClock : IDayClock
        {
            public int Today => 1000;
        }

        private static readonly int[] LevelIds = { 10, 20, 30, 40 };

        private EventBus _bus;
        private SaveService _save;
        private HintStock _stock;
        private HintRewardService _rewards;
        private ProgressRecorder _recorder;
        private StreakReward _streak;
        private readonly List<HintRewarded> _rewarded = new List<HintRewarded>();

        [SetUp]
        public void SetUp()
        {
            _rewarded.Clear();
            _bus = new EventBus();
            _bus.Subscribe<HintRewarded>(e => _rewarded.Add(e));
            _save = new SaveService(new InMemorySaveStore(), 3);
            _stock = new HintStock(_save, 5, _bus, 1);
            _streak = new StreakReward(3, 1, 0);
            _rewards = new HintRewardService(new IHintRewardStrategy[] { _streak }, _stock, _save, _bus, new FixedClock());
            _recorder = new ProgressRecorder(_bus, _save, new NullAudioService(), LevelIds, _rewards);
        }

        [TearDown]
        public void TearDown()
        {
            _recorder.Dispose();
            _rewards.Dispose();
        }

        [Test]
        public void ThreeDifferentLevelsWonWithThreeStars_EarnAHint()
        {
            _bus.Publish(new LevelWon(0, 4, 3));
            _bus.Publish(new LevelWon(1, 4, 3));
            _bus.Publish(new LevelWon(2, 4, 3));

            Assert.AreEqual(4, _stock.Count);
            Assert.AreEqual(1, _rewarded.Count);
        }

        [Test]
        public void ReplayingTheSameThreeStarLevel_EarnsNothing_BecauseTheStarsAreReadBeforeTheyAreSaved()
        {
            for (int i = 0; i < 9; i++) _bus.Publish(new LevelWon(0, 4, 3));

            Assert.AreEqual(3, _stock.Count);
            Assert.IsEmpty(_rewarded);
            Assert.AreEqual(1, _streak.Streak, "only the first win of the level counted");
        }

        [Test]
        public void ALevelWonWithTwoStars_ThenImprovedToThree_CountsTheSecondTime()
        {
            _bus.Publish(new LevelWon(0, 6, 2));
            Assert.AreEqual(0, _streak.Streak);

            _bus.Publish(new LevelWon(0, 4, 3));

            Assert.AreEqual(1, _streak.Streak);
        }

        [Test]
        public void ThePreviousStarsOfAnotherLevel_AreNotMixedUp()
        {
            _bus.Publish(new LevelWon(0, 4, 3));

            _bus.Publish(new LevelWon(1, 4, 3));

            Assert.AreEqual(2, _streak.Streak, "level 20 had no stars, so it is new even though level 10 has three");
        }

        [Test]
        public void WithoutARewardService_AWinIsStillSaved()
        {
            _recorder.Dispose();
            var plain = new ProgressRecorder(_bus, _save, new NullAudioService(), LevelIds);

            _bus.Publish(new LevelWon(3, 5, 3));

            Assert.AreEqual(3, _save.Progress.StarsOf(40));
            plain.Dispose();
            _recorder = new ProgressRecorder(_bus, _save, new NullAudioService(), LevelIds, _rewards);
        }
    }
}
