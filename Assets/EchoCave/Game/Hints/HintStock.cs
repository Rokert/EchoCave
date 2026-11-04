using System;
using Rokert.Core;

namespace EchoCave.Game
{
    // The hint stock, kept in the save so it survives a restart. Publishes HintsChanged whenever the count moves,
    // so the HUD never has to poll.
    public sealed class HintStock : IHintStock
    {
        private readonly ISaveService _save;
        private readonly EventBus _events;

        public int Max { get; }

        public int Count => _save.Hints;

        public HintStock(ISaveService save, int max, EventBus events)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), max, "The cap cannot be negative.");

            Max = max;
        }

        public bool TrySpend()
        {
            if (Count <= 0) return false;

            _save.SetHints(Count - 1);
            _events.Publish(new HintsChanged(Count));
            return true;
        }

        public int Grant(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Grant at least one hint.");

            int granted = Math.Max(0, Math.Min(count, Max - Count));
            if (granted == 0) return 0;

            _save.SetHints(Count + granted);
            _events.Publish(new HintsChanged(Count));
            return granted;
        }
    }
}
