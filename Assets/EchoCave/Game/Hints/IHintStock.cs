namespace EchoCave.Game
{
    // Where the hints of a free grant went. Whatever fit nowhere is lost, by design.
    public readonly struct HintGrant
    {
        public int ToStock { get; }
        public int ToReserve { get; }

        public int Granted => ToStock + ToReserve;

        public HintGrant(int toStock, int toReserve)
        {
            ToStock = toStock;
            ToReserve = toReserve;
        }
    }

    // The player's hints. One stock for the whole game, saved between sessions.
    public interface IHintStock
    {
        int Count { get; }

        int Max { get; }

        // Hints kept aside when the stock is full. Spending a hint moves one from here into the stock.
        int ReserveCount { get; }

        int ReserveMax { get; }

        // True when neither the stock nor the reserve has room for one more hint.
        bool IsFull { get; }

        // Takes one hint. False, and nothing changes, when there are none.
        bool TrySpend();

        // Adds hints up to Max; returns how many were really added. Never uses the reserve: an ad is only offered
        // with an empty stock, so there is nothing to overflow.
        int Grant(int count);

        // A free hint (a daily gift, a streak): fills the stock, then the reserve, and loses the rest.
        HintGrant GrantFree(int count);
    }

    // Stands in where no stock was given: there are never any hints.
    public sealed class NoHintStock : IHintStock
    {
        public int Count => 0;
        public int Max => 0;
        public int ReserveCount => 0;
        public int ReserveMax => 0;
        public bool IsFull => true;
        public bool TrySpend() => false;
        public int Grant(int count) => 0;
        public HintGrant GrantFree(int count) => new HintGrant(0, 0);
    }
}
