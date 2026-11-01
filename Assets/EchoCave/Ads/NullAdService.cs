using System;

namespace EchoCave.Ads
{
    // The provider for the editor and WebGL: no ads exist, so the flows that depend on them can still be played.
    // A rewarded request counts as watched; an interstitial reports Unavailable and the game carries on.
    public sealed class NullAdService : IAdService
    {
        public void ShowInterstitial(Action<AdResult> onComplete)
        {
            onComplete?.Invoke(AdResult.Unavailable);
        }

        public void ShowRewarded(Action<AdResult> onComplete)
        {
            onComplete?.Invoke(AdResult.Granted);
        }
    }
}
