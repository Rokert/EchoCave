using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Editor
{
    // Writes the challenge levels as LevelData assets, from the recipes in ChallengeLevels. An asset that already exists
    // is never overwritten: it is checked against its recipe and reported. A challenge asset keeps level id 0, because
    // challenges are identified by their string id and never enter the saved progress of the numbered levels.
    public static class ChallengeAssetsMenu
    {
        public const string Folder = "Assets/EchoCave/Data/Challenges";
        private static readonly MoveResolver Resolver = new MoveResolver();

        public const string CatalogPath = "Assets/EchoCave/Data/ChallengeCatalog.asset";

        public static string PathOf(string challengeId) => $"{Folder}/{challengeId}.asset";

        // Creates the asset of every challenge that has none yet, and checks the ones that already exist.
        [MenuItem("EchoCave/Challenges/Create missing challenge assets")]
        private static void CreateMissing()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/EchoCave/Data", "Challenges");

            foreach (string id in ChallengeLevels.AllIds)
            {
                LevelDefinition recipe = ChallengeLevels.Build(id, Resolver);
                string path = PathOf(id);

                var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (existing != null)
                {
                    Report(id, existing, recipe, "already exists");
                    continue;
                }

                var asset = ScriptableObject.CreateInstance<LevelData>();
                asset.Configure(recipe);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                Report(id, asset, recipe, "created");
            }
        }

        // The list the game plays. A new catalog holds every challenge; an existing one only gets the missing ones added
        // at the end, so no challenge is ever moved or rewritten.
        [MenuItem("EchoCave/Challenges/Create or update challenge catalog")]
        private static void CreateOrUpdateCatalog()
        {
            var levels = new List<ChallengeCatalog.Entry>();
            foreach (string id in ChallengeLevels.AllIds)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(PathOf(id));
                if (level == null)
                {
                    Debug.LogError($"[Challenges] {id} has no asset. Run EchoCave > Challenges > Create missing challenge assets first.");
                    return;
                }

                levels.Add(new ChallengeCatalog.Entry { challengeId = id, level = level });
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalog>(CatalogPath);
            string what;
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ChallengeCatalog>();
                catalog.SetEntries(levels);
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                what = "created";
            }
            else
            {
                int added = 0;
                foreach (ChallengeCatalog.Entry entry in levels)
                {
                    if (catalog.Has(entry.challengeId)) continue;

                    catalog.Append(entry);
                    added++;
                }

                if (added > 0) EditorUtility.SetDirty(catalog);
                what = added > 0 ? $"updated ({added} added)" : "already up to date";
            }

            AssetDatabase.SaveAssets();

            IReadOnlyList<string> problems = catalog.FindProblems();
            if (problems.Count == 0) Debug.Log($"[Challenges] Challenge catalog {what}: {catalog.Count} challenge(s), no problems. Assign it to the Challenge Catalog field of GameRoot.", catalog);
            else Debug.LogError($"[Challenges] Challenge catalog {what}, but it has problems:\n{string.Join("\n", problems)}", catalog);
        }

        [MenuItem("EchoCave/Challenges/Check challenge assets")]
        private static void Check()
        {
            foreach (string id in ChallengeLevels.AllIds)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(PathOf(id));
                if (asset == null)
                {
                    Debug.LogError($"[Challenges] {id} has no asset. Create it with EchoCave > Challenges > Create missing challenge assets.");
                    continue;
                }

                Report(id, asset, ChallengeLevels.Build(id, Resolver), "checked");
            }
        }

        private static void Report(string id, LevelData asset, LevelDefinition recipe, string what)
        {
            if (!ChallengeLevels.SameLevel(asset.ToDefinition(), recipe))
            {
                Debug.LogError($"[Challenges] {id} {what}, but the asset is NOT the level of its recipe. " +
                               "A changed challenge would reset its leaderboard: restore the asset from git, or make a new challenge id.", asset);
                return;
            }

            ChallengeDifficulty.TryMeasure(recipe, Resolver, 3000, 5, out ChallengeDifficulty d);
            Debug.Log($"[Challenges] {id} {what}: {d.Bats} bats, {d.Hidden} hidden, {d.PingBudget} ping(s), " +
                      $"{d.Waves} waves, a blind player wastes {d.ExpectedBlindBounces:0.0} taps, flawless by luck {d.ChanceOfFlawlessBlindRun:P2}.", asset);
        }
    }
}
