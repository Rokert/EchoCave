using System;
using System.Collections.Generic;
using EchoCave.Ads;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class HintRefillTests
    {
        // Reports a fixed status and records what the refill asked of it, and how many hints were held at that moment.
        private sealed class FakeGate : IHintGate
        {
            public HintStatus Status = HintStatus.Ready;
            public readonly List<int> HintsHeldAtRequest = new List<int>();
            public Func<int> HeldNow;

            public HintStatus CheckHint() => Status;

            public void RequestHint() => HintsHeldAtRequest.Add(HeldNow());
        }

        // Shows an ad by keeping its callback until the test finishes it, or answers at once.
        private sealed class FakeAds : IAdService
        {
            public int RewardedShown;
            public Action<AdResult> Pending;
            public AdResult? AnswerAtOnce;

            public void ShowInterstitial(Action<AdResult> onComplete) => onComplete?.Invoke(AdResult.Unavailable);

            public void ShowRewarded(Action<AdResult> onComplete)
            {
                RewardedShown++;
                if (AnswerAtOnce.HasValue) onComplete(AnswerAtOnce.Value);
                else Pending = onComplete;
            }

            public void Finish(AdResult result)
            {
                Action<AdResult> callback = Pending;
                Pending = null;
                callback(result);
            }
        }

        private FakeGate _gate;
        private FakeAds _ads;
        private FakeSaveService _save;
        private HintStock _stock;

        [SetUp]
        public void SetUp()
        {
            _save = new FakeSaveService(0);
            _stock = new HintStock(_save, 5, new EventBus());
            _gate = new FakeGate { HeldNow = () => _stock.Count };
            _ads = new FakeAds();
        }

        private HintRefill Refill(int perAd = 1, bool enabled = true) => new HintRefill(_gate, _stock, _ads, perAd, enabled);

        [Test]
        public void WithHintsLeft_ThePressGoesStraightToTheGame_AndNoAdShows()
        {
            _save.SetHints(2);
            _gate.Status = HintStatus.Ready;

            Refill().Request();

            Assert.AreEqual(0, _ads.RewardedShown);
            Assert.AreEqual(1, _gate.HintsHeldAtRequest.Count);
        }

        [Test]
        public void WithNoHintsLeft_AnAdShows_AndTheGameIsNotAskedYet()
        {
            _gate.Status = HintStatus.NoHintsLeft;

            Refill().Request();

            Assert.AreEqual(1, _ads.RewardedShown);
            Assert.IsEmpty(_gate.HintsHeldAtRequest);
        }

        [Test]
        public void AFinishedAd_GrantsTheHints_BeforeAskingTheGameForOne()
        {
            _gate.Status = HintStatus.NoHintsLeft;
            Refill(perAd: 2).Request();

            _ads.Finish(AdResult.Granted);

            Assert.AreEqual(2, _stock.Count);
            CollectionAssert.AreEqual(new[] { 2 }, _gate.HintsHeldAtRequest, "the hint is asked for after the grant, so the game can spend one");
        }

        [TestCase(AdResult.Dismissed)]
        [TestCase(AdResult.Unavailable)]
        public void AnAdThatDoesNotFinish_GrantsNothing_AndTheGameRefusesInTheUsualWay(AdResult result)
        {
            _gate.Status = HintStatus.NoHintsLeft;
            Refill().Request();

            _ads.Finish(result);

            Assert.AreEqual(0, _stock.Count);
            CollectionAssert.AreEqual(new[] { 0 }, _gate.HintsHeldAtRequest, "the game is asked, finds none, and refuses visibly");
        }

        [Test]
        public void AnInstantAnswer_LikeTheNullProvider_StillGrantsOnceAndAsksOnce()
        {
            _ads.AnswerAtOnce = AdResult.Granted;
            _gate.Status = HintStatus.NoHintsLeft;

            Refill().Request();

            Assert.AreEqual(1, _ads.RewardedShown);
            Assert.AreEqual(1, _stock.Count);
            Assert.AreEqual(1, _gate.HintsHeldAtRequest.Count);
        }

        [Test]
        public void APressWhileTheAdIsOnScreen_DoesNotStackAnotherAd()
        {
            _gate.Status = HintStatus.NoHintsLeft;
            HintRefill refill = Refill();

            refill.Request();
            refill.Request();
            refill.Request();

            Assert.AreEqual(1, _ads.RewardedShown);
        }

        [Test]
        public void OnceAnAdIsOver_AnotherPressCanShowAnotherAd()
        {
            _gate.Status = HintStatus.NoHintsLeft;
            HintRefill refill = Refill();
            refill.Request();
            _ads.Finish(AdResult.Dismissed);

            refill.Request();

            Assert.AreEqual(2, _ads.RewardedShown);
        }

        [TestCase(HintStatus.Busy)]
        [TestCase(HintStatus.AlreadyHinted)]
        [TestCase(HintStatus.Unavailable)]
        public void AHintRefusedForAnyOtherReason_NeverOffersAnAd(HintStatus status)
        {
            _gate.Status = status;

            Refill().Request();

            Assert.AreEqual(0, _ads.RewardedShown, "an ad would not fix this");
            Assert.AreEqual(1, _gate.HintsHeldAtRequest.Count);
        }

        [Test]
        public void WithTheRefillOff_NoAdIsEverShown_AndTheGameRefusesAsUsual()
        {
            _gate.Status = HintStatus.NoHintsLeft;

            Refill(enabled: false).Request();

            Assert.AreEqual(0, _ads.RewardedShown);
            Assert.AreEqual(1, _gate.HintsHeldAtRequest.Count);
            Assert.AreEqual(0, _stock.Count);
        }

        [Test]
        public void TheGrant_RespectsTheCap()
        {
            _save.SetHints(0);
            var small = new HintStock(_save, 1, new EventBus());
            var refill = new HintRefill(_gate, small, _ads, 3, true);
            _gate.HeldNow = () => small.Count;
            _gate.Status = HintStatus.NoHintsLeft;
            refill.Request();

            _ads.Finish(AdResult.Granted);

            Assert.AreEqual(1, small.Count, "three hints per ad cannot go past a cap of one");
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new HintRefill(null, _stock, _ads, 1, true));
            Assert.Throws<ArgumentNullException>(() => new HintRefill(_gate, null, _ads, 1, true));
            Assert.Throws<ArgumentNullException>(() => new HintRefill(_gate, _stock, null, 1, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HintRefill(_gate, _stock, _ads, 0, true));
        }
    }
}
