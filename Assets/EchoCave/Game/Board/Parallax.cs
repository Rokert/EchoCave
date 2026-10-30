using System;
using UnityEngine;

namespace EchoCave.Game
{
    // The math behind BoardBackdrop, kept apart so it can be tested without a scene. Depth runs from 0
    // (the layer never moves) to 1 (it moves the most). Strengths are shares of the view's shorter side, so
    // the effect looks the same on any resolution.
    public static class Parallax
    {
        // Portrait art for tall windows, landscape art for wide ones. A square window counts as landscape.
        public static bool IsLandscape(float aspect)
        {
            if (!(aspect > 0f)) throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "Aspect must be positive.");

            return aspect >= 1f;
        }

        // Screen pixels to -1..1 on both axes, 0 at the centre of the screen. Positions outside are clamped.
        public static Vector2 NormalizePointer(Vector2 screenPosition, Vector2 screenSize)
        {
            if (!(screenSize.x > 0f) || !(screenSize.y > 0f))
                throw new ArgumentOutOfRangeException(nameof(screenSize), screenSize, "Screen size must be positive.");

            return new Vector2(
                Mathf.Clamp(screenPosition.x / screenSize.x * 2f - 1f, -1f, 1f),
                Mathf.Clamp(screenPosition.y / screenSize.y * 2f - 1f, -1f, 1f));
        }

        // A slow wander that needs no input, each axis on its own frequency. Always inside -1..1.
        public static Vector2 Ambient(float time, Vector2 speed)
        {
            return new Vector2(Mathf.Sin(time * speed.x), Mathf.Sin(time * speed.y));
        }

        // World-space offset of a layer. Near layers move against the pointer, as if the viewer leaned toward it.
        public static Vector2 Offset(
            Vector2 pointer,
            Vector2 ambient,
            Vector2 viewSize,
            float depth,
            float pointerStrength,
            float ambientStrength)
        {
            float side = Mathf.Min(viewSize.x, viewSize.y);
            return -depth * side * (pointer * pointerStrength + ambient * ambientStrength);
        }

        // The furthest a layer can ever move on one axis, with pointer and ambient both inside -1..1.
        public static float MaxOffset(Vector2 viewSize, float depth, float pointerStrength, float ambientStrength)
        {
            return depth * Mathf.Min(viewSize.x, viewSize.y) * (pointerStrength + ambientStrength);
        }

        // Uniform scale that makes the sprite cover the view plus the margin the layer needs to move
        // without ever showing an edge.
        public static float CoverScale(
            Vector2 spriteSize,
            Vector2 viewSize,
            float depth,
            float pointerStrength,
            float ambientStrength)
        {
            float margin = MaxOffset(viewSize, depth, pointerStrength, ambientStrength) * 2f;
            return CameraFraming.CoverScale(spriteSize, viewSize + new Vector2(margin, margin));
        }
    }
}
