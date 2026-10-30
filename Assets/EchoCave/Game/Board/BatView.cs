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

        public int BatId { get; private set; }

        public void Bind(int batId, Direction direction, bool isHidden)
        {
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
