using System;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class RevealAllHiddenStrategyTests
    {
        [Test]
        public void CreatePulse_ListsOnlyTheHiddenBats_InBoardOrder()
        {
            var board = new Board(5, 5);
            board.Add(new Bat(4, new Cell(0, 0), Direction.Up, true));
            board.Add(new Bat(1, new Cell(1, 0), Direction.Up));
            board.Add(new Bat(7, new Cell(2, 0), Direction.Up, true));
            board.Add(new Bat(2, new Cell(3, 0), Direction.Up));

            EchoPulse pulse = new RevealAllHiddenStrategy(2f).CreatePulse(board);

            Assert.AreEqual(new[] { 4, 7 }, pulse.BatIds.ToArray());
        }

        [Test]
        public void CreatePulse_CarriesTheConfiguredDuration()
        {
            var board = new Board(2, 2);
            board.Add(new Bat(1, new Cell(0, 0), Direction.Up, true));

            EchoPulse pulse = new RevealAllHiddenStrategy(4.5f).CreatePulse(board);

            Assert.AreEqual(4.5f, pulse.DurationSeconds);
        }

        [Test]
        public void CreatePulse_WithNoHiddenBats_IsEmpty()
        {
            var board = new Board(2, 2);
            board.Add(new Bat(1, new Cell(0, 0), Direction.Up));

            EchoPulse pulse = new RevealAllHiddenStrategy(2f).CreatePulse(board);

            Assert.AreEqual(0, pulse.BatIds.Count);
        }

        [Test]
        public void CreatePulse_DoesNotChangeTheBoard()
        {
            var board = new Board(2, 2);
            board.Add(new Bat(1, new Cell(0, 0), Direction.Up, true));

            new RevealAllHiddenStrategy(2f).CreatePulse(board);

            Assert.IsTrue(board.TryGetBat(1, out Bat bat));
            Assert.IsTrue(bat.IsHidden);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void Constructor_RejectsADurationThatIsNotPositive(float duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RevealAllHiddenStrategy(duration));
        }

        [Test]
        public void CreatePulse_RejectsANullBoard()
        {
            Assert.Throws<ArgumentNullException>(() => new RevealAllHiddenStrategy(2f).CreatePulse(null));
        }
    }
}
