using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using UnityEditor;

namespace EchoCave.Tests
{
    // Runs over every LevelData asset in the project, so a bad level fails the suite as soon as it is saved.
    public class ShippedLevelsTests
    {
        [Test]
        public void EveryLevel_HasAUniquePositiveId()
        {
            var ids = new List<int>();
            foreach (string guid in AssetDatabase.FindAssets("t:LevelData"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) ids.Add(asset.LevelId);
            }

            Assert.IsEmpty(LevelIdRules.Validate(ids), "Level ids are what saved progress hangs on.");
        }

        // Saves of version 1 were keyed by catalog position and are converted as position + 1, which only holds
        // while these ten levels keep ids 1 to 10 in this order.
        [Test]
        public void TheFirstTenCatalogLevels_KeepTheIdsThatSaveMigrationRelyOn()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelCatalog");
            Assert.AreEqual(1, guids.Length, "Expected exactly one LevelCatalog.");
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));

            IReadOnlyList<int> ids = catalog.ToLevelIds();

            Assert.GreaterOrEqual(ids.Count, 10);
            for (int i = 0; i < 10; i++) Assert.AreEqual(i + 1, ids[i], $"catalog position {i}");
        }

        [Test]
        public void EveryLevel_IsValid_Solvable_AndFitsItsMoveLimit()
        {
            var solver = new LevelSolver(new MoveResolver());
            var problems = new List<string>();
            int checkedLevels = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:LevelData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (asset == null) continue;

                checkedLevels++;
                LevelDefinition definition = asset.ToDefinition();

                IReadOnlyList<string> errors = LevelValidator.Validate(definition);
                if (errors.Count > 0)
                {
                    problems.Add($"{path}: invalid - {string.Join(" ", errors)}");
                    continue;
                }

                SolveResult result = solver.Solve(definition);
                if (!result.IsSolvable)
                    problems.Add($"{path}: not solvable, deadlocked bats {string.Join(",", result.DeadlockedBatIds)}");
                else if (!result.FitsMoveLimit)
                    problems.Add($"{path}: move limit {definition.MoveLimit} is below the minimum {result.MinMoves}");
            }

            Assert.Greater(checkedLevels, 0, "No LevelData assets found, so nothing was validated.");
            Assert.IsEmpty(problems, $"Checked {checkedLevels} level(s):\n" + string.Join("\n", problems));
        }

        // The first ten levels climb in difficulty: the tiers of the project plan, and a curve that never drops.
        [Test]
        public void TheCatalog_FollowsTheDifficultyCurve()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelCatalog");
            Assert.AreEqual(1, guids.Length, "Expected exactly one LevelCatalog asset.");
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
            IReadOnlyList<LevelDefinition> levels = catalog.ToDefinitions();
            Assert.GreaterOrEqual(levels.Count, 10, "The catalog must hold the first 10 levels.");

            var resolver = new MoveResolver();
            int previous = 0;
            var problems = new List<string>();

            for (int i = 0; i < levels.Count; i++)
            {
                string name = $"Level {i + 1}";
                LevelDefinition level = levels[i];
                if (!LevelMetrics.TryMeasure(level, resolver, out LevelMetrics metrics))
                {
                    problems.Add($"{name}: invalid or not solvable");
                    continue;
                }

                if (i < 3 && (metrics.Hidden > 0 || level.PingBudget > 0))
                    problems.Add($"{name}: levels 1-3 have no hidden bats and no pings");
                if (i >= 3 && metrics.Hidden == 0)
                    problems.Add($"{name}: from level 4 on there is at least one hidden bat");
                if (metrics.Hidden > 0 && level.PingBudget == 0)
                    problems.Add($"{name}: has hidden bats but no pings to reveal them");
                if (level.ThreeStarMoves < metrics.MinMoves)
                    problems.Add($"{name}: three stars ({level.ThreeStarMoves}) is below the minimum {metrics.MinMoves}, so it cannot be earned");
                if (metrics.Difficulty < previous)
                    problems.Add($"{name}: difficulty {metrics.Difficulty} is below the previous level's {previous}");

                previous = metrics.Difficulty;
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
