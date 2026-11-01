namespace EchoCave.Ads
{
    // How an ad request ended. Only Granted means the player earns the reward.
    public enum AdResult
    {
        // The ad was watched to the end (rewarded) or shown (interstitial).
        Granted,

        // The player closed the ad early.
        Dismissed,

        // No ad could be shown: nothing loaded, no network, or no provider on this platform.
        Unavailable
    }
}
