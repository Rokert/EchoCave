using System;
using System.Collections.Generic;
using System.Text;

namespace EchoCave.Game
{
    // Writes every event to a log line instead of sending it, so the events can be checked in the editor or a development
    // build before any provider exists. The composition root passes Debug.Log.
    public sealed class LogAnalyticsService : IAnalyticsService
    {
        private readonly Action<string> _log;

        public LogAnalyticsService(Action<string> log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Track(string eventName, IReadOnlyDictionary<string, string> parameters)
        {
            var line = new StringBuilder("[Analytics] ").Append(eventName);
            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> pair in parameters) line.Append(' ').Append(pair.Key).Append('=').Append(pair.Value);
            }

            _log(line.ToString());
        }
    }
}
