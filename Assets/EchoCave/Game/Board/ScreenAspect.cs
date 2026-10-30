using System;

namespace EchoCave.Game
{
    // The one rule that decides portrait or landscape, shared by the backdrop art and the UI scaling so
    // they never disagree about which orientation a window is.
    public static class ScreenAspect
    {
        // A square window counts as landscape.
        public static bool IsLandscape(float aspect)
        {
            if (!(aspect > 0f)) throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "Aspect must be positive.");

            return aspect >= 1f;
        }

        public static bool IsLandscape(float width, float height)
        {
            if (!(width > 0f) || !(height > 0f))
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width and height must be positive.");

            return IsLandscape(width / height);
        }
    }
}
