using DG.Tweening;
using UnityEngine;

namespace EchoCave.Game
{
    // Tunables for drawing and animating the board. Distances are in cells so they survive a cell size change.
    [CreateAssetMenu(fileName = "BoardViewSettings", menuName = "EchoCave/Board View Settings")]
    public sealed class BoardViewSettings : ScriptableObject
    {
        [Header("Layout")]
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [Tooltip("Empty space around the board, in cells, when the camera frames it.")]
        [SerializeField, Min(0f)] private float cameraMarginCells = 1f;

        [Header("Fly-out")]
        [Tooltip("Cells per second.")]
        [SerializeField, Min(0.1f)] private float flySpeed = 12f;
        [Tooltip("How far past the board edge a bat flies before it disappears, in cells.")]
        [SerializeField, Min(0f)] private float exitOvershootCells = 1.5f;
        [SerializeField] private Ease exitEase = Ease.InSine;

        [Header("Bounce")]
        [Tooltip("How far a bat pushes into the blocker before coming back, in cells.")]
        [SerializeField, Min(0f)] private float bumpCells = 0.2f;
        [SerializeField, Min(0.01f)] private float bumpDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float returnDuration = 0.25f;
        [SerializeField] private Ease returnEase = Ease.OutBack;
        [Tooltip("How far the blocking bat is knocked, in cells.")]
        [SerializeField, Min(0f)] private float blockerPunchCells = 0.15f;
        [SerializeField, Min(0.01f)] private float blockerPunchDuration = 0.25f;

        public float CellSize => cellSize;
        public float CameraMarginCells => cameraMarginCells;
        public float FlySpeed => flySpeed;
        public float ExitOvershootCells => exitOvershootCells;
        public Ease ExitEase => exitEase;
        public float BumpCells => bumpCells;
        public float BumpDuration => bumpDuration;
        public float ReturnDuration => returnDuration;
        public Ease ReturnEase => returnEase;
        public float BlockerPunchCells => blockerPunchCells;
        public float BlockerPunchDuration => blockerPunchDuration;
    }
}
