namespace EchoCave.Core
{
    public readonly struct BatDefinition
    {
        public Cell Cell { get; }
        public Direction Direction { get; }
        public bool IsHidden { get; }

        public BatDefinition(Cell cell, Direction direction, bool isHidden = false)
        {
            Cell = cell;
            Direction = direction;
            IsHidden = isHidden;
        }
    }
}
