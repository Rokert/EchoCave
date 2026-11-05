using System;

namespace EchoCave.Game
{
    // The volume ramp of a crossfade, apart from the AudioSource so it can be tested.
    public static class MusicFade
    {
        // Moves a volume toward its target in a straight line that would take fadeSeconds to go from 0 to 1. A fade
        // of 0 (or less) jumps there at once.
        public static float Step(float current, float target, float deltaSeconds, float fadeSeconds)
        {
            if (fadeSeconds <= 0f) return target;

            float step = Math.Max(0f, deltaSeconds) / fadeSeconds;
            if (current < target) return Math.Min(target, current + step);
            return Math.Max(target, current - step);
        }
    }
}
