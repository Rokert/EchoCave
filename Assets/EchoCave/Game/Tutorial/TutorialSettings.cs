using UnityEngine;

namespace EchoCave.Game
{
    // Per-build choices about the tutorial. Data, so a playtest build can be made with or without the automatic tutorial
    // by changing an asset, not code.
    [CreateAssetMenu(fileName = "TutorialSettings", menuName = "EchoCave/Tutorial Settings")]
    public sealed class TutorialSettings : ScriptableObject
    {
        [Tooltip("The tutorial starts by itself the first time the player presses Play, until it has been finished or skipped. Turn it off for a playtest that asks whether the rules are understood without explanation. \"How to play\" in the menu and the pause popup works either way.")]
        [SerializeField] private bool autoStart = true;

        public bool AutoStart => autoStart;
    }
}
