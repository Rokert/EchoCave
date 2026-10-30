using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoCave.Game
{
    // Turns a mouse click or a touch into a world-space point and raises Tapped. It forwards only:
    // what the tap hits is the presenter's business. <Pointer>/press covers mouse, pen and touch.
    public sealed class TapInput : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;

        private InputAction _press;
        private IPointerBlocker _blocker;

        public event Action<Vector2> Tapped;

        // Null removes it. A tap that starts on a UI element is dropped here, before anything hears of it.
        public void SetBlocker(IPointerBlocker blocker)
        {
            _blocker = blocker;
        }

        private void Awake()
        {
            _press = new InputAction("Tap", InputActionType.Button, "<Pointer>/press");
        }

        private void OnEnable()
        {
            _press.performed += OnPress;
            _press.Enable();
        }

        private void OnDisable()
        {
            _press.Disable();
            _press.performed -= OnPress;
        }

        private void OnDestroy()
        {
            _press.Dispose();
        }

        private void OnPress(InputAction.CallbackContext context)
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            Vector2 screen = pointer.position.ReadValue();
            if (_blocker != null && _blocker.IsOverUi(screen)) return;

            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            Tapped?.Invoke(world);
        }
    }
}
