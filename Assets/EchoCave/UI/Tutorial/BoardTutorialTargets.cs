using System;
using EchoCave.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoCave.UI
{
    // Adapts the board and the camera to the panel: where a bat is, in the units the overlay draws in.
    public sealed class BoardTutorialTargets : ITutorialTargets
    {
        private readonly BoardView _board;
        private readonly Camera _camera;
        private readonly Func<IPanel> _panel;

        public BoardTutorialTargets(BoardView board, Camera camera, Func<IPanel> panel)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
        }

        public bool TryGetBatRect(int batId, out Rect rect)
        {
            rect = default;

            IPanel panel = _panel();
            if (panel == null || !_board.TryGetBatWorld(batId, out Vector3 world, out float cellWorldSize)) return false;

            Vector2 center = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, _camera);
            Vector3 edgeWorld = world + _camera.transform.right * (cellWorldSize * 0.5f);
            Vector2 edge = RuntimePanelUtils.CameraTransformWorldToPanel(panel, edgeWorld, _camera);

            float half = Vector2.Distance(center, edge);
            if (float.IsNaN(half) || half <= 0f) return false;

            rect = new Rect(center.x - half, center.y - half, half * 2f, half * 2f);
            return true;
        }
    }
}
