using System;
using Rokert.Core;

namespace EchoCave.Ads
{
    // The only way the game asks for an ad. Provider SDK types stay behind this interface (D-009).
    // Callbacks rather than Task: WebGL has no threads, and the SDKs report through callbacks anyway.
    public interface IAdService : IService
    {
        // Shows a full-screen ad between levels. onComplete always runs, exactly once.
        void ShowInterstitial(Action<AdResult> onComplete);

        // Shows an ad the player chose to watch for a reward. onComplete always runs, exactly once;
        // grant the reward only on Granted.
        void ShowRewarded(Action<AdResult> onComplete);
    }
}
