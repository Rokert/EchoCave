using System;
using EchoCave.Ads;

namespace EchoCave.Game
{
    // The edge between the hint button and the ad service. A press with hints left goes straight to the game; a
    // press with none left shows a rewarded ad, and a completed ad grants hints and uses one at once. Any other way a
    // hint can be refused (a move animating, a hint already showing) never offers an ad: watching one would not fix it.
    // Plain C#: GameRoot builds it, because the root is where the ad service is known.
    public sealed class HintRefill
    {
        private readonly IHintGate _gate;
        private readonly IHintStock _stock;
        private readonly IAdService _ads;
        private readonly int _hintsPerAd;
        private readonly bool _enabled;

        private bool _adShowing;

        public HintRefill(IHintGate gate, IHintStock stock, IAdService ads, int hintsPerAd, bool enabled)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            if (hintsPerAd < 1) throw new ArgumentOutOfRangeException(nameof(hintsPerAd), hintsPerAd, "An ad must grant at least one hint.");

            _hintsPerAd = hintsPerAd;
            _enabled = enabled;
        }

        // What the hint button calls.
        public void Request()
        {
            if (!_enabled || _gate.CheckHint() != HintStatus.NoHintsLeft)
            {
                _gate.RequestHint();
                return;
            }

            // A second press while the ad is on screen must not stack another ad.
            if (_adShowing) return;

            _adShowing = true;
            _ads.ShowRewarded(OnAdFinished);
        }

        private void OnAdFinished(AdResult result)
        {
            _adShowing = false;

            // Granted pays out and then asks for the hint as usual. Anything else asks for it anyway: with no hints
            // the game refuses it in the usual visible way, so the player sees that the ad did not help.
            if (result == AdResult.Granted) _stock.Grant(_hintsPerAd);
            _gate.RequestHint();
        }
    }
}
