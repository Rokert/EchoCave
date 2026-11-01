namespace EchoCave.Game
{
    // The player's hints. One stock for the whole game, saved between sessions.
    public interface IHintStock
    {
        int Count { get; }

        int Max { get; }

        // Takes one hint. False, and nothing changes, when there are none.
        bool TrySpend();

        // Adds hints up to Max; returns how many were really added.
        int Grant(int count);
    }

    // Stands in where no stock was given: there are never any hints.
    public sealed class NoHintStock : IHintStock
    {
        public int Count => 0;
        public int Max => 0;
        public bool TrySpend() => false;
        public int Grant(int count) => 0;
    }
}
