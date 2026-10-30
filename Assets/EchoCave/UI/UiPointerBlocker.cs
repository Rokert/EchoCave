using System;
using EchoCave.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoCave.UI
{
    // Tells the board whether a pointer is over something the UI draws. Containers that only hold layout are
    // set to ignore the pointer in the UXML, so only real controls and panels answer.
    public sealed class UiPointerBlocker : IPointerBlocker
    {
        private readonly UIDocument _document;

        public UiPointerBlocker(UIDocument document)
        {
            _document = document != null ? document : throw new ArgumentNullException(nameof(document));
        }

        public bool IsOverUi(Vector2 screenPosition)
        {
            VisualElement root = _document.rootVisualElement;
            if (root == null || root.panel == null) return false;

            // Screen space starts at the bottom left, panel space at the top left.
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return root.panel.Pick(panelPosition) != null;
        }
    }
}
