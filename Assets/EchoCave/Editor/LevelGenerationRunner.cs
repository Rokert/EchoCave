using System.Text;
using EchoCave.Core;
using EchoCave.Game;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Editor
{
    // Turns a LevelGenerationPlan into LevelData assets and fills the catalog with them in order. Each level
    // must be at least as hard as the one before it, so levels are generated in sequence.
    public static class LevelGenerationRunner
    {
        private static readonly MoveResolver Resolver = new MoveResolver();

        // Generates every entry in order and rewrites the catalog. Returns a report for the window.
        public static string GenerateAll(LevelGenerationPlan plan)
        {
            var report = new StringBuilder();
            int previousDifficulty = 0;

            for (int i = 0; i < plan.Entries.Count; i++)
            {
                if (!TryGenerate(plan, i, previousDifficulty, report, out int difficulty))
                {
                    AssetDatabase.SaveAssets();
                    return report.ToString();
                }

                previousDifficulty = difficulty;
            }

            WriteCatalog(plan);
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        // Regenerates one entry, harder than the entry before it. The catalog is rewritten too, so it never
        // points at a missing asset.
        public static string GenerateOne(LevelGenerationPlan plan, int index)
        {
            var report = new StringBuilder();
            int previousDifficulty = 0;

            if (index > 0)
            {
                var previous = AssetDatabase.LoadAssetAtPath<LevelData>(plan.PathOf(plan.Entries[index - 1]));
                if (previous != null
                    && LevelMetrics.TryMeasure(previous.ToDefinition(), Resolver, out LevelMetrics metrics))
                    previousDifficulty = metrics.Difficulty;
            }

            if (TryGenerate(plan, index, previousDifficulty, report, out _)) WriteCatalog(plan);
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static bool TryGenerate(LevelGenerationPlan plan, int index, int previousDifficulty, StringBuilder report, out int difficulty)
        {
            LevelGenerationPlan.Entry entry = plan.Entries[index];
            string path = plan.PathOf(entry);
            difficulty = 0;

            if (entry.keepAsIs)
            {
                var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (existing == null)
                {
                    report.AppendLine($"{entry.levelName}: marked as hand-made but {path} does not exist.");
                    return false;
                }

                if (!LevelMetrics.TryMeasure(existing.ToDefinition(), Resolver, out LevelMetrics kept))
                {
                    report.AppendLine($"{entry.levelName}: the hand-made level is invalid or not solvable.");
                    return false;
                }

                difficulty = kept.Difficulty;
                report.AppendLine($"{entry.levelName}: kept as is, difficulty {difficulty}.");
                return true;
            }

            LevelSpec spec = plan.ToSpec(entry);
            LevelDefinition level = new LevelGenerator(Resolver).Generate(spec, entry.seed, previousDifficulty, plan.MaxAttempts);
            if (level == null)
            {
                report.AppendLine(
                    $"{entry.levelName}: no layout fit in {plan.MaxAttempts} attempts (seed {entry.seed}, " +
                    $"difficulty {previousDifficulty} to {previousDifficulty + spec.MaxDifficultyStep}). " +
                    "Try another seed, fewer waves or traps, or a larger step.");
                return false;
            }

            LevelMetrics.TryMeasure(level, Resolver, out LevelMetrics metrics);
            difficulty = metrics.Difficulty;

            var asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            Undo.RecordObject(asset, $"Generate {entry.levelName}");
            asset.Configure(level);
            EditorUtility.SetDirty(asset);

            report.AppendLine(
                $"{entry.levelName}: {metrics.Bats} bats, {metrics.Hidden} hidden, {metrics.BlockedAtStart} traps, " +
                $"{metrics.Waves} waves, difficulty {difficulty}, limit {(level.HasMoveLimit ? level.MoveLimit.ToString() : "none")}.");
            return true;
        }

        private static void WriteCatalog(LevelGenerationPlan plan)
        {
            if (plan.Catalog == null) return;

            var catalog = new SerializedObject(plan.Catalog);
            SerializedProperty levels = catalog.FindProperty("levels");
            levels.arraySize = plan.Entries.Count;
            for (int i = 0; i < plan.Entries.Count; i++)
                levels.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelData>(plan.PathOf(plan.Entries[i]));

            catalog.ApplyModifiedProperties();
            EditorUtility.SetDirty(plan.Catalog);
        }
    }
}
