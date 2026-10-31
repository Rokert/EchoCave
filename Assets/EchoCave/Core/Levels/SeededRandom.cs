using System;

namespace EchoCave.Core
{
    // A small deterministic generator (SplitMix64), so the same seed always gives the same levels on every
    // platform. The core cannot use System.Random: its sequence is not part of any contract.
    public sealed class SeededRandom
    {
        private ulong _state;

        public SeededRandom(int seed)
        {
            _state = unchecked((ulong)(uint)seed);
        }

        // A value in [0, maxExclusive).
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Must be positive.");

            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        private ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
