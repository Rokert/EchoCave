using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class LevelDataTests
    {
        private LevelData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<LevelData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void RoundTrip_DefinitionToDataAndBack_KeepsEverything()
        {
            var original = new LevelDefinition(
                5, 4,
                new[]
                {
                    new BatDefinition(new Cell(0, 3), Direction.Right),
                    new BatDefinition(new Cell(4, 0), Direction.Up, true)
                },
                moveLimit: 7, pingBudget: 3, threeStarMoves: 3, twoStarMoves: 6);

            _data.Configure(original);
            LevelDefinition result = _data.ToDefinition();

            Assert.AreEqual((5, 4, 7, 3, 3, 6),
                (result.Width, result.Height, result.MoveLimit, result.PingBudget, result.ThreeStarMoves, result.TwoStarMoves));
            Assert.AreEqual(2, result.Bats.Count);
            Assert.AreEqual((new Cell(0, 3), Direction.Right, false),
                (result.Bats[0].Cell, result.Bats[0].Direction, result.Bats[0].IsHidden));
            Assert.AreEqual((new Cell(4, 0), Direction.Up, true),
                (result.Bats[1].Cell, result.Bats[1].Direction, result.Bats[1].IsHidden));
        }

        [Test]
        public void DataToBoard_BuildsTheSameBatsAtTheSameCells()
        {
            var definition = new LevelDefinition(
                3, 3,
                new[] { new BatDefinition(new Cell(1, 2), Direction.Down, true) },
                moveLimit: 0, pingBudget: 1, threeStarMoves: 1, twoStarMoves: 2);
            _data.Configure(definition);

            LevelSession session = LevelBuilder.Build(_data.ToDefinition());

            Assert.IsTrue(session.Board.TryGetBat(new Cell(1, 2), out Bat bat));
            Assert.AreEqual(Direction.Down, bat.Direction);
            Assert.IsTrue(bat.IsHidden);
        }

        [Test]
        public void NewAsset_HasNoBats_SoItIsRejectedAsInvalid()
        {
            var errors = LevelValidator.Validate(_data.ToDefinition());

            Assert.IsNotEmpty(errors);
        }
    }
}
