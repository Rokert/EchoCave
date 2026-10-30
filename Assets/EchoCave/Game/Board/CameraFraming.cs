using System;
using UnityEngine;

namespace EchoCave.Game
{
    // The math behind BoardCamera, kept apart so it can be tested without a camera.
    public static class CameraFraming
    {
        // Orthographic size (half the visible height) that fits the whole board plus the margin on every side,
        // for either a tall or a wide screen.
        public static float OrthographicSize(Vector2 boardSize, float aspect, float margin)
        {
            if (aspect <= 0f) throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "Aspect must be positive.");

            float halfHeight = boardSize.y * 0.5f + margin;
            float halfWidth = boardSize.x * 0.5f + margin;
            return Mathf.Max(halfHeight, halfWidth / aspect);
        }

        // Uniform scale that makes a sprite cover the whole view, cropping the longer side.
        public static float CoverScale(Vector2 spriteSize, Vector2 viewSize)
        {
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(spriteSize), spriteSize, "Sprite size must be positive.");

            return Mathf.Max(viewSize.x / spriteSize.x, viewSize.y / spriteSize.y);
        }
    }
}
