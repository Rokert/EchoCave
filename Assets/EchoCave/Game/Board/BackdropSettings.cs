using UnityEngine;

namespace EchoCave.Game
{
    // Tunables for the layered background.
    [CreateAssetMenu(fileName = "BackdropSettings", menuName = "EchoCave/Backdrop Settings")]
    public sealed class BackdropSettings : ScriptableObject
    {
        [Header("Layers")]
        [Tooltip("Sorting order of the first layer; the rest follow in draw order. Keep it below the cell tiles and the bats.")]
        [SerializeField] private int baseSortingOrder = -100;
        [Tooltip("One value per layer, back to front: 0 never moves, 1 moves the most. Shared by both orientations.")]
        [SerializeField] private float[] layerDepths = { 0f, 0.3f, 0.55f, 0.8f, 1f };

        [Header("Motion")]
        [Tooltip("How far the pointer pushes the nearest layer, as a share of the view's shorter side.")]
        [SerializeField, Range(0f, 0.2f)] private float pointerStrength = 0.03f;
        [Tooltip("How far the idle drift moves the nearest layer, as a share of the view's shorter side.")]
        [SerializeField, Range(0f, 0.2f)] private float ambientStrength = 0.012f;
        [Tooltip("Radians per second of the idle drift, per axis.")]
        [SerializeField] private Vector2 ambientSpeed = new Vector2(0.31f, 0.23f);
        [Tooltip("Seconds the layers take to follow the pointer.")]
        [SerializeField, Min(0.01f)] private float pointerSmoothTime = 0.25f;
        [Tooltip("Turns all background motion off.")]
        [SerializeField] private bool reduceMotion;

        public int BaseSortingOrder => baseSortingOrder;
        public float PointerStrength => pointerStrength;
        public float AmbientStrength => ambientStrength;
        public Vector2 AmbientSpeed => ambientSpeed;
        public float PointerSmoothTime => pointerSmoothTime;
        public bool ReduceMotion => reduceMotion;

        // Layers past the end of the list reuse the last depth.
        public float DepthFor(int layerIndex)
        {
            if (layerDepths == null || layerDepths.Length == 0) return 0f;

            return Mathf.Clamp01(layerDepths[Mathf.Min(layerIndex, layerDepths.Length - 1)]);
        }
    }
}
