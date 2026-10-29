using System;
using System.Collections.Generic;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Authoring asset for one level. Gameplay code works with the LevelDefinition it produces.
    [CreateAssetMenu(fileName = "Level", menuName = "EchoCave/Level")]
    public sealed class LevelData : ScriptableObject
    {
        [Serializable]
        public struct BatEntry
        {
            public Vector2Int cell;
            public Direction direction;
            public bool hidden;
        }

        [SerializeField, Min(1)] private int width = 5;
        [SerializeField, Min(1)] private int height = 5;
        [SerializeField] private List<BatEntry> bats = new List<BatEntry>();

        [Tooltip("0 means no move limit.")]
        [SerializeField, Min(0)] private int moveLimit;
        [SerializeField, Min(0)] private int pingBudget;
        [SerializeField, Min(1)] private int threeStarMoves = 1;
        [SerializeField, Min(1)] private int twoStarMoves = 1;

        public LevelDefinition ToDefinition()
        {
            var definitions = new List<BatDefinition>(bats.Count);
            foreach (BatEntry bat in bats)
                definitions.Add(new BatDefinition(new Cell(bat.cell.x, bat.cell.y), bat.direction, bat.hidden));

            return new LevelDefinition(width, height, definitions, moveLimit, pingBudget, threeStarMoves, twoStarMoves);
        }

        internal void Configure(LevelDefinition definition)
        {
            width = definition.Width;
            height = definition.Height;
            moveLimit = definition.MoveLimit;
            pingBudget = definition.PingBudget;
            threeStarMoves = definition.ThreeStarMoves;
            twoStarMoves = definition.TwoStarMoves;

            bats = new List<BatEntry>(definition.Bats.Count);
            foreach (BatDefinition bat in definition.Bats)
            {
                bats.Add(new BatEntry
                {
                    cell = new Vector2Int(bat.Cell.X, bat.Cell.Y),
                    direction = bat.Direction,
                    hidden = bat.IsHidden
                });
            }
        }
    }
}
