using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class CellAndDirectionTests
    {
        [TestCase(Direction.Up, 2, 4)]
        [TestCase(Direction.Down, 2, 2)]
        [TestCase(Direction.Left, 1, 3)]
        [TestCase(Direction.Right, 3, 3)]
        public void Step_MovesOneCellInDirection(Direction direction, int expectedX, int expectedY)
        {
            Cell result = new Cell(2, 3).Step(direction);

            Assert.AreEqual(new Cell(expectedX, expectedY), result);
        }

        [TestCase(Direction.Up, Direction.Down)]
        [TestCase(Direction.Down, Direction.Up)]
        [TestCase(Direction.Left, Direction.Right)]
        [TestCase(Direction.Right, Direction.Left)]
        public void Opposite_ReversesDirection(Direction direction, Direction expected)
        {
            Assert.AreEqual(expected, direction.Opposite());
        }

        [Test]
        public void Cell_EqualityAndHashAreValueBased()
        {
            var a = new Cell(1, 2);
            var b = new Cell(1, 2);

            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsFalse(a == new Cell(2, 1));
        }

        [Test]
        public void Bat_WithersKeepOtherFieldsAndDoNotMutate()
        {
            var bat = new Bat(1, new Cell(0, 0), Direction.Left, true);

            Bat moved = bat.WithCell(new Cell(3, 3));
            Bat revealed = bat.WithHidden(false);

            Assert.AreEqual(new Cell(0, 0), bat.Cell);
            Assert.IsTrue(bat.IsHidden);
            Assert.AreEqual(new Cell(3, 3), moved.Cell);
            Assert.AreEqual((1, Direction.Left, true), (moved.Id, moved.Direction, moved.IsHidden));
            Assert.IsFalse(revealed.IsHidden);
            Assert.AreEqual(new Cell(0, 0), revealed.Cell);
        }
    }
}
