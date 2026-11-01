using System.Collections.Generic;

namespace EchoCave.Game
{
    // Counts nothing. The web playtest runs on this (D-025).
    public sealed class NullAnalyticsService : IAnalyticsService
    {
        public void Track(string eventName, IReadOnlyDictionary<string, string> parameters)
        {
        }
    }
}
