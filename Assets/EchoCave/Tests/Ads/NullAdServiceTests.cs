using EchoCave.Ads;
using NUnit.Framework;

namespace EchoCave.Tests.Ads
{
    public sealed class NullAdServiceTests
    {
        [Test]
        public void ARewardedRequest_IsGrantedExactlyOnce()
        {
            var service = new NullAdService();
            int calls = 0;
            AdResult result = AdResult.Dismissed;

            service.ShowRewarded(r => { calls++; result = r; });

            Assert.AreEqual(1, calls);
            Assert.AreEqual(AdResult.Granted, result);
        }

        [Test]
        public void AnInterstitialRequest_ReportsUnavailableExactlyOnce()
        {
            var service = new NullAdService();
            int calls = 0;
            AdResult result = AdResult.Granted;

            service.ShowInterstitial(r => { calls++; result = r; });

            Assert.AreEqual(1, calls);
            Assert.AreEqual(AdResult.Unavailable, result);
        }

        [Test]
        public void ANullCallback_DoesNotThrow()
        {
            var service = new NullAdService();

            Assert.DoesNotThrow(() => service.ShowRewarded(null));
            Assert.DoesNotThrow(() => service.ShowInterstitial(null));
        }
    }
}
