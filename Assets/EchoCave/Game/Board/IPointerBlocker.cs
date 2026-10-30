using UnityEngine;

namespace EchoCave.Game
{
    // Lets the UI tell the board that a pointer is on a UI element, so the tap must not reach the bats.
    // Defined here, implemented in EchoCave.UI, because the UI assembly depends on this one and not the reverse.
    public interface IPointerBlocker
    {
        // The position is in screen pixels, origin at the bottom left.
        bool IsOverUi(Vector2 screenPosition);
    }
}
