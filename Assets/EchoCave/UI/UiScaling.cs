using EchoCave.Game;
using UnityEngine;

namespace EchoCave.UI
{
    // How the panel scales: the reference canvas and how to match it to the screen.
    public readonly struct UiScale
    {
        public Vector2Int ReferenceResolution { get; }

        // 0 matches the reference width, 1 matches the reference height.
        public float Match { get; }

        public UiScale(Vector2Int referenceResolution, float match)
        {
            ReferenceResolution = referenceResolution;
            Match = match;
        }

        // Screen pixels per panel unit, the way Unity's "match width or height" scale mode works out.
        public float PixelsPerUnit(Vector2 screenSize)
        {
            return Mathf.Pow(screenSize.x / ReferenceResolution.x, 1f - Match)
                * Mathf.Pow(screenSize.y / ReferenceResolution.y, Match);
        }
    }

    // The UI is laid out on a 1080x1920 canvas in portrait and a 1920x1080 canvas in landscape, and the
    // panel is matched to the side that stays fixed, so sizes read the same on any screen of that orientation.
    public static class UiScaling
    {
        private static readonly Vector2Int PortraitCanvas = new Vector2Int(1080, 1920);
        private static readonly Vector2Int LandscapeCanvas = new Vector2Int(1920, 1080);

        public static UiScale For(float screenWidth, float screenHeight)
        {
            return ScreenAspect.IsLandscape(screenWidth, screenHeight)
                ? new UiScale(LandscapeCanvas, 1f)
                : new UiScale(PortraitCanvas, 0f);
        }
    }
}
