using EchoCave.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // The Challenges button is optional in the UXML and hidden through a USS class until the presenter says otherwise.
    public class MenuViewChallengesTests
    {
        private VisualElement _root;
        private Button _challenges;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _root.Add(new VisualElement { name = "menu-layer" });
            _root.Add(new Button { name = "play-button" });
            _root.Add(new Label { name = "version-label" });
            _challenges = new Button { name = "challenges-button" };
            _root.Add(_challenges);
        }

        [Test]
        public void SetChallengesVisible_ShowsAndHidesTheButton()
        {
            var view = new MenuView(_root, "0.1.3");

            view.SetChallengesVisible(false);
            Assert.IsTrue(_challenges.ClassListContains("menu-button-hidden"));

            view.SetChallengesVisible(true);
            Assert.IsFalse(_challenges.ClassListContains("menu-button-hidden"));
        }

        [Test]
        public void AMenuWithoutTheButton_StillWorks()
        {
            _root.Remove(_challenges);

            var view = new MenuView(_root, "0.1.3");

            Assert.DoesNotThrow(() => view.SetChallengesVisible(true));
        }
    }
}
