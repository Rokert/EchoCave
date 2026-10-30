using System;
using UnityEngine;

namespace EchoCave.UI
{
    // The border of the screen the UI must stay clear of (notches, rounded corners, system bars).
    public readonly struct ScreenInsets
    {
        public float Left { get; }
        public float Top { get; }
        public float Right { get; }
        public float Bottom { get; }

        public ScreenInsets(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        // Screen.safeArea is in pixels with the origin at the bottom left. Whatever lies outside it becomes an inset.
        public static ScreenInsets FromSafeArea(Rect safeArea, Vector2 screenSize)
        {
            if (!(screenSize.x > 0f) || !(screenSize.y > 0f))
                throw new ArgumentOutOfRangeException(nameof(screenSize), screenSize, "Screen size must be positive.");

            return new ScreenInsets(
                Mathf.Max(0f, safeArea.xMin),
                Mathf.Max(0f, screenSize.y - safeArea.yMax),
                Mathf.Max(0f, screenSize.x - safeArea.xMax),
                Mathf.Max(0f, safeArea.yMin));
        }

        // Pixels to panel units: divide by the pixels each unit takes.
        public ScreenInsets InPanelUnits(float pixelsPerUnit)
        {
            if (!(pixelsPerUnit > 0f))
                throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit), pixelsPerUnit, "Pixels per unit must be positive.");

            return new ScreenInsets(Left / pixelsPerUnit, Top / pixelsPerUnit, Right / pixelsPerUnit, Bottom / pixelsPerUnit);
        }
    }
}
