using System.Collections.Generic;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // The levels of the game, in play order.
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "EchoCave/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelData> levels = new List<LevelData>();

        public int Count => levels.Count;

        // The stable id of each level, in play order. Check them with LevelIdRules.Validate.
        public IReadOnlyList<int> ToLevelIds()
        {
            var ids = new List<int>(levels.Count);
            foreach (LevelData level in levels) ids.Add(level.LevelId);

            return ids;
        }

        public IReadOnlyList<LevelDefinition> ToDefinitions()
        {
            var definitions = new List<LevelDefinition>(levels.Count);
            foreach (LevelData level in levels) definitions.Add(level.ToDefinition());

            return definitions;
        }
    }
}
