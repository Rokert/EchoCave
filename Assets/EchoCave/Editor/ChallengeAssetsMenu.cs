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

        [MenuItem("EchoCave/Challenges/Create challenge-001")]
        private static void CreateChallenge001()
        {
            LevelDefinition recipe = ChallengeLevels.BuildChallenge001(Resolver);
            string path = PathOf(ChallengeLevels.Challenge001Id);

            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing != null)
            {
                Report(existing, recipe, "already exists");
                return;
            }

            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/EchoCave/Data", "Challenges");

            var asset = ScriptableObject.CreateInstance<LevelData>();
            asset.Configure(recipe);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Report(asset, recipe, "created");
        }

        // The list the game plays: challenge-001 for now. An existing catalog is never rewritten, only checked.
        [MenuItem("EchoCave/Challenges/Create challenge catalog")]
        private static void CreateCatalog()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(PathOf(ChallengeLevels.Challenge001Id));
            if (level == null)
            {
                Debug.LogError("[Challenges] Create challenge-001 first (EchoCave > Challenges > Create challenge-001).");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalog>(CatalogPath);
            string what = "already exists";
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ChallengeCatalog>();
                catalog.SetEntries(new[] { new ChallengeCatalog.Entry { challengeId = ChallengeLevels.Challenge001Id, level = level } });
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                AssetDatabase.SaveAssets();
                what = "created";
            }

            IReadOnlyList<string> problems = catalog.FindProblems();
            if (problems.Count == 0) Debug.Log($"[Challenges] Challenge catalog {what}: {catalog.Count} challenge(s), no problems. Assign it to the Challenge Catalog field of GameRoot.", catalog);
            else Debug.LogError($"[Challenges] Challenge catalog {what}, but it has problems:\n{string.Join("\n", problems)}", catalog);
        }

        [MenuItem("EchoCave/Challenges/Check challenge assets")]
        private static void Check()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelData>(PathOf(ChallengeLevels.Challenge001Id));
            if (asset == null)
            {
                Debug.LogError($"[Challenges] {ChallengeLevels.Challenge001Id} has no asset. Create it with EchoCave > Challenges > Create challenge-001.");
                return;
            }

            Report(asset, ChallengeLevels.BuildChallenge001(Resolver), "checked");
        }

        private static void Report(LevelData asset, LevelDefinition recipe, string what)
        {
            if (!ChallengeLevels.SameLevel(asset.ToDefinition(), recipe))
            {
                Debug.LogError($"[Challenges] {ChallengeLevels.Challenge001Id} {what}, but the asset is NOT the level of its recipe. " +
                               "A changed challenge would reset its leaderboard: restore the asset from git, or make a new challenge id.", asset);
                return;
            }

            ChallengeDifficulty.TryMeasure(recipe, Resolver, 3000, 5, out ChallengeDifficulty d);
            Debug.Log($"[Challenges] {ChallengeLevels.Challenge001Id} {what}: {d.Bats} bats, {d.Hidden} hidden, {d.PingBudget} ping(s), " +
                      $"{d.Waves} waves, a blind player wastes {d.ExpectedBlindBounces:0.0} taps, flawless by luck {d.ChanceOfFlawlessBlindRun:P2}.", asset);
        }
    }
}
