using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // What one ping reveals: which bats, and for how long.
    public readonly struct EchoPulse
    {
        public IReadOnlyList<int> BatIds { get; }
        public float DurationSeconds { get; }

        public EchoPulse(IReadOnlyList<int> batIds, float durationSeconds)
        {
            BatIds = batIds ?? throw new ArgumentNullException(nameof(batIds));
            DurationSeconds = durationSeconds;
        }
    }
}
