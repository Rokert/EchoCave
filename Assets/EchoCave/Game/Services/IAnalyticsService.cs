using System.Collections.Generic;
using Rokert.Core;

namespace EchoCave.Game
{
    // One call for every event worth counting. The provider (and what is worth counting) is still open.
    public interface IAnalyticsService : IService
    {
        // parameters may be null.
        void Track(string eventName, IReadOnlyDictionary<string, string> parameters);
    }
}
