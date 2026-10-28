using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    public sealed class Board
    {
        private readonly List<Bat> _bats = new List<Bat>();
        private readonly Dictionary<int, Bat> _byId = new Dictionary<int, Bat>();
        private readonly Dictionary<Cell, Bat> _byCell = new Dictionary<Cell, Bat>();

        public int Width { get; }
        public int Height { get; }

        // Insertion order, so iteration is deterministic.
        public IReadOnlyList<Bat> Bats => _bats;

        public int Count => _bats.Count;

        public Board(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");

            Width = width;
            Height = height;
        }

        public bool IsInside(Cell cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        }

        public bool IsOccupied(Cell cell) => _byCell.ContainsKey(cell);

        public bool TryGetBat(Cell cell, out Bat bat) => _byCell.TryGetValue(cell, out bat);

        public bool TryGetBat(int id, out Bat bat) => _byId.TryGetValue(id, out bat);

        public void Add(Bat bat) => Insert(_bats.Count, bat);

        // Inserts at a position in Bats, so a removed bat can be restored exactly where it was.
        public void Insert(int index, Bat bat)
        {
            if (index < 0 || index > _bats.Count) throw new ArgumentOutOfRangeException(nameof(index), index, null);
            if (bat == null) throw new ArgumentNullException(nameof(bat));
            if (!IsInside(bat.Cell))
                throw new ArgumentException($"Bat {bat.Id} is outside the {Width}x{Height} board at {bat.Cell}.", nameof(bat));
            if (_byId.ContainsKey(bat.Id))
                throw new ArgumentException($"A bat with id {bat.Id} is already on the board.", nameof(bat));
            if (_byCell.ContainsKey(bat.Cell))
                throw new ArgumentException($"Cell {bat.Cell} is already occupied.", nameof(bat));

            _bats.Insert(index, bat);
            _byId.Add(bat.Id, bat);
            _byCell.Add(bat.Cell, bat);
        }

        public bool Remove(int id)
        {
            if (!_byId.TryGetValue(id, out Bat bat)) return false;

            _bats.Remove(bat);
            _byId.Remove(id);
            _byCell.Remove(bat.Cell);
            return true;
        }
    }
}
