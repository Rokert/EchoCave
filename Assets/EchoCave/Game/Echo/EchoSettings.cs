using UnityEngine;

namespace EchoCave.Game
{
    // Tunables for the echo ping.
    [CreateAssetMenu(fileName = "EchoSettings", menuName = "EchoCave/Echo Settings")]
    public sealed class EchoSettings : ScriptableObject
    {
        [Tooltip("How long a ping keeps the hidden bats' arrows visible, in seconds.")]
        [SerializeField, Min(0.1f)] private float revealSeconds = 3f;

        public float RevealSeconds => revealSeconds;
    }
}
