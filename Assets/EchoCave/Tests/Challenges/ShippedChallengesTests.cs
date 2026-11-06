using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using UnityEditor;

namespace EchoCave.Tests
{
    // Checks the real assets: the level in the project must be exactly the one its recipe gives.
    public class ShippedChallengesTests
    {
        private const string Challenge001Path = "Assets/EchoCave/Data/Challenges/challenge-001.asset";

        [Test]
        public void Challenge001_HasItsAsset_AndItIsTheLevelOfItsRecipe()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelData>(Challenge001Path);
            Assert.IsNotNull(asset, "Create it with EchoCave > Challenges > Create missing challenge assets.");

            Assert.IsTrue(ChallengeLevels.SameLevel(asset.ToDefinition(), ChallengeLevels.BuildChallenge001(new MoveResolver())),
                "The asset differs from its recipe: a changed challenge resets its leaderboard.");
        }

        [Test]
        public void EveryChallenge_HasItsAsset_AndItIsTheLevelOfItsRecipe()
        {
            foreach (string id in ChallengeLevels.AllIds)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/EchoCave/Data/Challenges/{id}.asset");
                Assert.IsNotNull(asset, $"{id} has no asset: EchoCave > Challenges > Create missing challenge assets.");
                Assert.AreEqual(0, asset.LevelId, id);

                Assert.IsTrue(ChallengeLevels.SameLevel(asset.ToDefinition(), ChallengeLevels.Build(id, new MoveResolver())),
                    $"{id}: the asset differs from its recipe, and a changed challenge resets its leaderboard.");
            }
        }

        [Test]
        public void TheChallengeCatalog_ListsEveryChallenge_InTheOrderOfTheirIds()
        {
            string[] guids = AssetDatabase.FindAssets("t:ChallengeCatalog");
            Assert.AreEqual(1, guids.Length);
            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));

            CollectionAssert.AreEqual(
                ChallengeLevels.AllIds,
                System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(catalog.ToEntries(), e => e.Id)),
                "EchoCave > Challenges > Create or update challenge catalog adds the missing ones.");
        }

        [Test]
        public void TheChallengeCatalog_HasNoProblems_AndStartsWithChallenge001()
        {
            string[] guids = AssetDatabase.FindAssets("t:ChallengeCatalog");
            Assert.AreEqual(1, guids.Length, "Expected exactly one ChallengeCatalog (EchoCave > Challenges > Create or update challenge catalog).");
            var catalog = AssetDatabase.LoadAssetAtPath<ChallengeCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));

            Assert.IsEmpty(catalog.FindProblems(), string.Join("\n", catalog.FindProblems()));
            Assert.AreEqual(ChallengeLevels.Challenge001Id, catalog.ToEntries()[0].Id);
        }

        [Test]
        public void ChallengeAssets_KeepLevelId0_SoTheyNeverEnterTheNumberedLevelProgress()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelData>(Challenge001Path);
            Assert.IsNotNull(asset);

            Assert.AreEqual(0, asset.LevelId);
        }
    }
}
