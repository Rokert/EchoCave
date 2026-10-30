using System;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Maps board cells to world positions and back. The board is centered on the origin and Y grows
    // upwards, matching Direction (Up is +Y). Keeping this out of the views makes it EditMode-testable.
    public sealed class GridLayout2D
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }

        public Vector2 Size => new Vector2(Width * CellSize, Height * CellSize);

        public GridLayout2D(int width, int height, float cellSize)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "Cell size must be positive.");

            Width = width;
            Height = height;
            CellSize = cellSize;
        }

        public bool Contains(Cell cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        }

        public Vector2 CellToWorld(Cell cell)
        {
            return new Vector2(
                (cell.X - (Width - 1) * 0.5f) * CellSize,
                (cell.Y - (Height - 1) * 0.5f) * CellSize);
        }

        // Returns false when the point falls outside the board; the cell is then not meaningful.
        public bool TryWorldToCell(Vector2 world, out Cell cell)
        {
            int x = Mathf.FloorToInt(world.x / CellSize + (Width - 1) * 0.5f + 0.5f);
            int y = Mathf.FloorToInt(world.y / CellSize + (Height - 1) * 0.5f + 0.5f);

            cell = new Cell(x, y);
            return Contains(cell);
        }
    }
}
