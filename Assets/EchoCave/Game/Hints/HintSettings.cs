using UnityEngine;

namespace EchoCave.Game
{
    // How many hints the player has and how they come back. Data, so it can be tuned without code.
    [CreateAssetMenu(fileName = "HintSettings", menuName = "EchoCave/Hint Settings")]
    public sealed class HintSettings : ScriptableObject
    {
        [Tooltip("Hints a new player starts with, and what an older save without a hint count is given.")]
        [SerializeField, Min(0)] private int startingHints = 3;
        [Tooltip("The most hints a player can hold. Hints that would go past it are lost.")]
        [SerializeField, Min(1)] private int maxHints = 5;
        [Tooltip("Hints granted for one rewarded ad.")]
        [SerializeField, Min(1)] private int hintsPerAd = 1;
        [Tooltip("With no hints left, pressing the button offers a rewarded ad. Turn it off for a playtest build: the null ad provider grants every request, so the hints would be unlimited.")]
        [SerializeField] private bool refillWithAds = true;

        public int StartingHints => startingHints;
        public int MaxHints => maxHints;
        public int HintsPerAd => hintsPerAd;
        public bool RefillWithAds => refillWithAds;

        private void OnValidate()
        {
            if (maxHints < startingHints) maxHints = startingHints;
        }
    }
}
