namespace EchoCave.Core
{
    // Immutable: moving or revealing a bat produces a new Bat, which keeps undo snapshots trivial.
    public sealed class Bat
    {
        public int Id { get; }
        public Cell Cell { get; }
        public Direction Direction { get; }
        public bool IsHidden { get; }

        public Bat(int id, Cell cell, Direction direction, bool isHidden = false)
        {
            Id = id;
            Cell = cell;
            Direction = direction;
            IsHidden = isHidden;
        }

        public Bat WithCell(Cell cell) => new Bat(Id, cell, Direction, IsHidden);

        public Bat WithHidden(bool isHidden) => new Bat(Id, Cell, Direction, isHidden);
    }
}
