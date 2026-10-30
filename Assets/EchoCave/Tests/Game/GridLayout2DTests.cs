using System;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class GridLayout2DTests
    {
        private const float Tolerance = 1e-5f;

        private static void AssertPosition(float x, float y, Vector2 actual)
        {
            Assert.AreEqual(x, actual.x, Tolerance, "x");
            Assert.AreEqual(y, actual.y, Tolerance, "y");
        }

        [Test]
        public void OddBoard_CenterCellSitsAtTheOrigin()
        {
            var layout = new GridLayout2D(5, 5, 1f);

            AssertPosition(0f, 0f, layout.CellToWorld(new Cell(2, 2)));
        }

        [Test]
        public void EvenBoard_IsCenteredBetweenCells()
        {
            var layout = new GridLayout2D(4, 4, 2f);

            AssertPosition(-3f, -3f, layout.CellToWorld(new Cell(0, 0)));
            AssertPosition(3f, 3f, layout.CellToWorld(new Cell(3, 3)));
        }

        [Test]
        public void YGrowsUpwards_AndXGrowsToTheRight()
        {
            var layout = new GridLayout2D(3, 3, 1f);

            Vector2 origin = layout.CellToWorld(new Cell(1, 1));
            Vector2 up = layout.CellToWorld(new Cell(1, 1).Step(Direction.Up));
            Vector2 right = layout.CellToWorld(new Cell(1, 1).Step(Direction.Right));

            Assert.Greater(up.y, origin.y);
            Assert.Greater(right.x, origin.x);
        }

        [Test]
        public void NonSquareBoard_UsesEachAxisSize()
        {
            var layout = new GridLayout2D(6, 2, 1f);

            AssertPosition(-2.5f, -0.5f, layout.CellToWorld(new Cell(0, 0)));
            AssertPosition(2.5f, 0.5f, layout.CellToWorld(new Cell(5, 1)));
            Assert.AreEqual(new Vector2(6f, 2f), layout.Size);
        }

        [Test]
        public void WorldToCell_RoundTripsEveryCell()
        {
            var layout = new GridLayout2D(5, 4, 1.5f);

            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    var cell = new Cell(x, y);

                    Assert.IsTrue(layout.TryWorldToCell(layout.CellToWorld(cell), out Cell result));
                    Assert.AreEqual(cell, result);
                }
            }
        }

        [Test]
        public void WorldToCell_AcceptsAnyPointInsideTheCell()
        {
            var layout = new GridLayout2D(3, 3, 2f);
            Vector2 center = layout.CellToWorld(new Cell(2, 0));

            Assert.IsTrue(layout.TryWorldToCell(center + new Vector2(0.9f, -0.9f), out Cell result));
            Assert.AreEqual(new Cell(2, 0), result);
        }

        [Test]
        public void WorldToCell_OutsideTheBoard_ReturnsFalse()
        {
            var layout = new GridLayout2D(3, 3, 1f);

            Assert.IsFalse(layout.TryWorldToCell(new Vector2(1.6f, 0f), out _));
            Assert.IsFalse(layout.TryWorldToCell(new Vector2(0f, -1.6f), out _));
            Assert.IsFalse(layout.TryWorldToCell(new Vector2(-10f, 10f), out _));
        }

        [Test]
        public void Contains_MatchesTheBoardBounds()
        {
            var layout = new GridLayout2D(3, 2, 1f);

            Assert.IsTrue(layout.Contains(new Cell(2, 1)));
            Assert.IsFalse(layout.Contains(new Cell(3, 0)));
            Assert.IsFalse(layout.Contains(new Cell(0, 2)));
            Assert.IsFalse(layout.Contains(new Cell(-1, 0)));
        }

        [TestCase(0, 3, 1f)]
        [TestCase(3, 0, 1f)]
        [TestCase(3, 3, 0f)]
        [TestCase(3, 3, -1f)]
        public void InvalidSize_Throws(int width, int height, float cellSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridLayout2D(width, height, cellSize));
        }
    }

    public class BatViewTests
    {
        [TestCase(Direction.Up, 0f)]
        [TestCase(Direction.Left, 90f)]
        [TestCase(Direction.Down, 180f)]
        [TestCase(Direction.Right, 270f)]
        public void AngleFor_RotatesAnUpArrowCounterClockwise(Direction direction, float expected)
        {
            Assert.AreEqual(expected, BatView.AngleFor(direction));
        }

        [Test]
        public void AngleFor_RotatedArrowPointsAlongTheDirectionOffset()
        {
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                Vector3 pointing = Quaternion.Euler(0f, 0f, BatView.AngleFor(direction)) * Vector3.up;
                Cell offset = direction.ToOffset();

                Assert.AreEqual(offset.X, pointing.x, 1e-4f, direction.ToString());
                Assert.AreEqual(offset.Y, pointing.y, 1e-4f, direction.ToString());
            }
        }
    }
}
