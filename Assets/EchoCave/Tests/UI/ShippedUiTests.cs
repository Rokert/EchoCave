using EchoCave.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // Builds the real UXML into the real views, so a renamed or missing element fails the suite instead of failing when
    // the game starts. The challenge screens are optional in the views, so these tests also show each one on screen: a
    // typo in a name would otherwise leave it silently doing nothing.
    public class ShippedUiTests
    {
        private static VisualElement Load(string file)
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EchoCave/UI/Views/" + file);
            Assert.IsNotNull(tree, file);
            return tree.Instantiate();
        }

        [Test]
        public void ThePopupsUxml_BuildsThePopupsView_AndTheChallengeResultShows()
        {
            VisualElement root = Load("Popups.uxml");
            var view = new PopupsView(root, 0);

            view.ShowChallengeResult("New best!", new[] { "52 taps in 2:20.5", "Rank 1 on this device" });

            Assert.IsFalse(root.Q("challenge-popup").ClassListContains("popup-hidden"));
            Assert.AreEqual("New best!", root.Q<Label>("challenge-title").text);
            Assert.AreEqual(2, root.Q("challenge-lines").childCount);
        }

        [Test]
        public void ThePopupsUxml_ShowsTheChallengeList_WithACardPerChallenge()
        {
            VisualElement root = Load("Popups.uxml");
            var view = new PopupsView(root, 0);

            view.ShowChallenges(
                new[] { new ChallengeRowData("challenge-001", "Not tried yet", new string[0], true) }, "Win 2 more levels to unlock");

            Assert.IsFalse(root.Q("challenges-popup").ClassListContains("popup-hidden"));
            Assert.AreEqual(1, root.Q<ScrollView>("challenges-rows").contentContainer.childCount);
            Assert.IsFalse(root.Q<Label>("challenges-message").ClassListContains("popup-hidden"));
        }

        [Test]
        public void TheMenuUxml_BuildsTheMenuView_WithAChallengesButtonThatStartsHidden()
        {
            VisualElement root = Load("Menu.uxml");
            var view = new MenuView(root, "0.1.3");
            Button button = root.Q<Button>("challenges-button");

            Assert.IsNotNull(button, "the menu has no challenges-button");
            Assert.IsTrue(button.ClassListContains("menu-button-hidden"));

            view.SetChallengesVisible(true);
            Assert.IsFalse(button.ClassListContains("menu-button-hidden"));
        }

        // The HUD is hidden (display none) while the menu shows, and the challenge list opens from the menu: a popup
        // inside the HUD would be built and never seen. This is what happened the first time.
        [Test]
        public void ThePopups_AndTheMenu_AreNotInsideTheHud_WhichIsHiddenInTheMenu()
        {
            VisualElement root = Load("Hud.uxml");
            VisualElement hud = root.Q("hud-root");

            Assert.IsNotNull(hud);
            Assert.IsNotNull(root.Q("popup-layer"), "the document has no popups");
            Assert.IsNotNull(root.Q("menu-layer"), "the document has no menu");
            Assert.IsNull(hud.Q("popup-layer"), "the popups are inside the HUD, so they cannot show from the menu");
            Assert.IsNull(hud.Q("menu-layer"), "the menu is inside the HUD");
            Assert.IsNotNull(root.Q("popups-root"), "UiRoot gives the popups their orientation class through this name");
        }
    }
}
