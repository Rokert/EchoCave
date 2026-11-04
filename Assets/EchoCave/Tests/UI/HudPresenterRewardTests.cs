using EchoCave.Core;
using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class HudPresenterRewardTests
    {
        private FakeHudView _view;
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeHudView();
            _bus = new EventBus();
        }

        private HudPresenter Presenter(int reserve = 0) =>
            new HudPresenter(_view, _bus, () => { }, () => { }, () => { }, 10, 3, false, reserve);

        [Test]
        public void TheReserveTheHudStartsWith_IsShown()
        {
            using (Presenter(reserve: 1))
            {
                Assert.AreEqual(1, _view.HintReserve);
            }
        }

        [Test]
        public void TheReserveChanging_UpdatesTheSpareTag()
        {
            using (Presenter())
            {
                _bus.Publish(new HintReserveChanged(1));
                Assert.AreEqual(1, _view.HintReserve);

                _bus.Publish(new HintReserveChanged(0));
                Assert.AreEqual(0, _view.HintReserve);
            }
        }

        [TestCase(HintRewardSource.Daily, 1, 0)]
        [TestCase(HintRewardSource.Streak, 0, 1)]
        public void AFreeHint_PopsTheButton_WhereverItWent(HintRewardSource source, int toStock, int toReserve)
        {
            using (Presenter())
            {
                _bus.Publish(new HintRewarded(source, toStock, toReserve));

                Assert.AreEqual(1, _view.HintPulseCount);
            }
        }

        [Test]
        public void TheCountOrReserveChanging_WithoutAReward_DoesNotPop()
        {
            using (Presenter())
            {
                _bus.Publish(new HintsChanged(2));
                _bus.Publish(new HintReserveChanged(1));

                Assert.AreEqual(0, _view.HintPulseCount, "spending a hint is not a reward");
            }
        }

        [Test]
        public void AfterDispose_RewardsAreNotListenedTo()
        {
            Presenter().Dispose();

            _bus.Publish(new HintRewarded(HintRewardSource.Streak, 1, 0));
            _bus.Publish(new HintReserveChanged(1));

            Assert.AreEqual(0, _view.HintPulseCount);
            Assert.AreEqual(0, _view.HintReserve, "the last value it was given at construction");
        }
    }
}
