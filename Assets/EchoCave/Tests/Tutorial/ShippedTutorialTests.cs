using EchoCave.Game;
using NUnit.Framework;
using UnityEditor;

namespace EchoCave.Tests
{
    // Checks the real assets, so a missing or broken lesson fails the suite instead of failing in front of a player. Red
    // until the lessons exist: EchoCave > Tutorial > Create default lessons.
    public class ShippedTutorialTests
    {
        private const string CatalogPath = "Assets/EchoCave/Data/TutorialCatalog.asset";

        [Test]
        public void TheTutorialCatalog_Exists_AndHoldsValidSolvableLessons()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TutorialCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, $"No catalog at {CatalogPath}. Run EchoCave > Tutorial > Create default lessons.");

            Assert.IsEmpty(catalog.FindProblems(), string.Join("\n", catalog.FindProblems()));
            Assert.GreaterOrEqual(catalog.Count, 1);
        }
    }
}
