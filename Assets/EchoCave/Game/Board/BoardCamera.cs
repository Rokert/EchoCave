using UnityEngine;

namespace EchoCave.Game
{
    // Frames the board with an orthographic camera and keeps a background sprite covering the view.
    // It refits when the screen aspect changes, which happens on rotation and on WebGL window resizes.
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCamera : MonoBehaviour
    {
        [SerializeField] private BoardViewSettings settings;
        [Tooltip("Optional. Scaled to cover the whole view and centered on the board.")]
        [SerializeField] private SpriteRenderer background;

        private Camera _camera;
        private BoardView _board;
        private float _framedAspect;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public void Frame(BoardView board)
        {
            _board = board;
            Refit();
        }

        private void Update()
        {
            if (_board != null && !Mathf.Approximately(_camera.aspect, _framedAspect)) Refit();
        }

        private void Refit()
        {
            GridLayout2D layout = _board.Layout;
            Vector3 center = _board.transform.position;
            float margin = settings.CameraMarginCells * layout.CellSize;

            _camera.orthographic = true;
            _camera.orthographicSize = CameraFraming.OrthographicSize(layout.Size, _camera.aspect, margin);
            transform.position = new Vector3(center.x, center.y, transform.position.z);
            _framedAspect = _camera.aspect;

            if (background == null || background.sprite == null) return;

            var view = new Vector2(_camera.orthographicSize * 2f * _camera.aspect, _camera.orthographicSize * 2f);
            background.transform.position = new Vector3(center.x, center.y, background.transform.position.z);
            background.transform.localScale = Vector3.one * CameraFraming.CoverScale(background.sprite.bounds.size, view);
        }
    }
}
