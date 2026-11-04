using System;
using Rokert.Core;

namespace EchoCave.Game
{
    // The hint stock, kept in the save so it survives a restart. Publishes HintsChanged whenever the count moves
    // and HintReserveChanged whenever the reserve does, so the HUD never has to poll.
    //
    // The reserve holds free hints that did not fit. It only fills while the stock is full, and spending a hint
    // pulls one back into the stock, so the count the player sees stays the same and the spare tag disappears.
    public sealed class HintStock : IHintStock
    {
        private readonly ISaveService _save;
        private readonly EventBus _events;

        public int Max { get; }

        public int ReserveMax { get; }

        public int Count => _save.Hints;

        public int ReserveCount => _save.ReserveHints;

        public bool IsFull => Count >= Max && ReserveCount >= ReserveMax;

        public HintStock(ISaveService save, int max, EventBus events, int reserveMax = 0)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), max, "The cap cannot be negative.");
            if (reserveMax < 0) throw new ArgumentOutOfRangeException(nameof(reserveMax), reserveMax, "The reserve cannot be negative.");

            Max = max;
            ReserveMax = reserveMax;
        }

        public bool TrySpend()
        {
            if (Count <= 0) return false;

            if (ReserveCount > 0)
            {
                // The reserve takes the place of the spent hint: the stock stays as it was.
                _save.SetReserveHints(ReserveCount - 1);
                _events.Publish(new HintReserveChanged(ReserveCount));
                return true;
            }

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

        public HintGrant GrantFree(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Grant at least one hint.");

            int toStock = Math.Max(0, Math.Min(count, Max - Count));
            int toReserve = Math.Max(0, Math.Min(count - toStock, ReserveMax - ReserveCount));

            if (toStock > 0)
            {
                _save.SetHints(Count + toStock);
                _events.Publish(new HintsChanged(Count));
            }

            if (toReserve > 0)
            {
                _save.SetReserveHints(ReserveCount + toReserve);
                _events.Publish(new HintReserveChanged(ReserveCount));
            }

            return new HintGrant(toStock, toReserve);
        }
    }
}
