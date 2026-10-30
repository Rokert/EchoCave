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
    }
}
