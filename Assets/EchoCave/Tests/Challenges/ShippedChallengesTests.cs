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
            Assert.IsNotNull(asset, "Create it with EchoCave > Challenges > Create challenge-001.");

            Assert.IsTrue(ChallengeLevels.SameLevel(asset.ToDefinition(), ChallengeLevels.BuildChallenge001(new MoveResolver())),
                "The asset differs from its recipe: a changed challenge resets its leaderboard.");
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
