using DG.Tweening;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Draws one bat: the bat sprite plus an arrow overlay, or a dark "?" silhouette when it is hidden.
    // Only draws; the presenter decides what to show. The arrow sprite must point up at rotation zero.
    public sealed class BatView : MonoBehaviour
    {
        [SerializeField] private GameObject body;
        [SerializeField] private Transform arrow;
        [SerializeField] private GameObject silhouette;

        private bool _restCaptured;
        private Vector3 _restScale;
        private Quaternion _restRotation;

        public int BatId { get; private set; }

        public void Bind(int batId, Direction direction, bool isHidden)
        {
            CaptureRest();
            BatId = batId;
            arrow.localRotation = Quaternion.Euler(0f, 0f, AngleFor(direction));
            SetHidden(isHidden);
        }

        public void SetHidden(bool isHidden)
        {
            body.SetActive(!isHidden);
            arrow.gameObject.SetActive(!isHidden);
            silhouette.SetActive(isHidden);
        }

        // Stops animations and restores the prefab pose, because fly-out and bounce tweens change both.
        // Position is left alone: the board view sets it every time a bat is spawned.
        internal void ResetForPool()
        {
            CaptureRest();
            transform.DOKill();
            transform.localScale = _restScale;
            transform.localRotation = _restRotation;
        }

        // Lazy because the first call can come before Awake when the container is inactive.
        private void CaptureRest()
        {
            if (_restCaptured) return;

            _restScale = transform.localScale;
            _restRotation = transform.localRotation;
            _restCaptured = true;
        }

        // Counter-clockwise degrees around Z for an arrow that points up at zero.
        internal static float AngleFor(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return 0f;
                case Direction.Left: return 90f;
                case Direction.Down: return 180f;
                case Direction.Right: return 270f;
                default: throw new System.ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
