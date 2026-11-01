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

        [Header("Cell tiles")]
        [Tooltip("Tile size as a share of the cell: below 1 leaves a thin gap between tiles.")]
        [SerializeField, Range(0.5f, 1f)] private float tileScale = 0.94f;
        [Tooltip("Above the background layers, below the bats.")]
        [SerializeField] private int tileSortingOrder = -50;

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

        [Header("Refused action")]
        [Tooltip("How far the bats shake sideways when a ping is refused, in cells.")]
        [SerializeField, Min(0f)] private float rejectShakeCells = 0.12f;
        [SerializeField, Min(0.01f)] private float rejectShakeDuration = 0.3f;
        [SerializeField, Min(1)] private int rejectShakeVibrato = 8;

        [Header("Echo reveal")]
        [Tooltip("Seconds a hidden bat takes to fade from its silhouette into the bat.")]
        [SerializeField, Min(0f)] private float revealFadeSeconds = 0.25f;
        [Tooltip("Seconds it takes to fade back into the silhouette when the echo runs out.")]
        [SerializeField, Min(0f)] private float concealFadeSeconds = 0.35f;
        [Tooltip("The revealed bat starts this many times its size and settles to normal. 1 means no pop.")]
        [SerializeField, Min(1f)] private float revealPopScale = 1.25f;

        [Header("Ping wave")]
        [Tooltip("How fast the ring travels, in cells per second. Keep the board crossed in well under a second: a bat can be tapped before the ring reaches it.")]
        [SerializeField, Min(0.1f)] private float waveSpeedCells = 14f;
        [Tooltip("Where the ring band sits in its sprite, as a share of the sprite's half size (about 0.78 for Ping Ring).")]
        [SerializeField, Range(0.1f, 1f)] private float waveBandShare = 0.78f;
        [Tooltip("Above the bats.")]
        [SerializeField] private int waveSortingOrder = 3;

        public float CellSize => cellSize;
        public float CameraMarginCells => cameraMarginCells;
        public float TileScale => tileScale;
        public int TileSortingOrder => tileSortingOrder;
        public float FlySpeed => flySpeed;
        public float ExitOvershootCells => exitOvershootCells;
        public Ease ExitEase => exitEase;
        public float BumpCells => bumpCells;
        public float BumpDuration => bumpDuration;
        public float ReturnDuration => returnDuration;
        public Ease ReturnEase => returnEase;
        public float BlockerPunchCells => blockerPunchCells;
        public float BlockerPunchDuration => blockerPunchDuration;
        public float RejectShakeCells => rejectShakeCells;
        public float RejectShakeDuration => rejectShakeDuration;
        public int RejectShakeVibrato => rejectShakeVibrato;
        public float RevealFadeSeconds => revealFadeSeconds;
        public float ConcealFadeSeconds => concealFadeSeconds;
        public float RevealPopScale => revealPopScale;
        public float WaveSpeedCells => waveSpeedCells;
        public float WaveBandShare => waveBandShare;
        public int WaveSortingOrder => waveSortingOrder;
    }
}
