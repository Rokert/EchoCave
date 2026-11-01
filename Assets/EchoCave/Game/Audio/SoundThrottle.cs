using System.Collections.Generic;

namespace EchoCave.Game
{
    // Lets a cue play at most once per interval, so a burst of the same event (several bats revealed in the
    // same instant) sounds like one hit instead of a smear. The clock is passed in, so it is testable.
    public sealed class SoundThrottle
    {
        private readonly Dictionary<SoundCue, float> _lastPlayed = new Dictionary<SoundCue, float>();

        // True, and the moment is remembered, when the cue may play now. An interval of 0 never blocks.
        public bool TryAllow(SoundCue cue, float nowSeconds, float minIntervalSeconds)
        {
            // A clock that went backwards (a time reset) must not silence the cue until it catches up.
            if (minIntervalSeconds > 0f
                && _lastPlayed.TryGetValue(cue, out float last)
                && nowSeconds >= last
                && nowSeconds - last < minIntervalSeconds)
                return false;

            _lastPlayed[cue] = nowSeconds;
            return true;
        }
    }
}
