using EchoCave.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // The How to play button is optional in the UXML and hidden through a USS class until the presenter says otherwise.
    public class MenuViewHowToPlayTests
    {
        private VisualElement _root;
        private Button _howTo;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _root.Add(new VisualElement { name = "menu-layer" });
            _root.Add(new Button { name = "play-button" });
            _root.Add(new Label { name = "version-label" });
            _howTo = new Button { name = "howto-button" };
            _root.Add(_howTo);
        }

        [Test]
        public void SetHowToPlayVisible_ShowsAndHidesTheButton()
        {
            var view = new MenuView(_root, "0.1.1");

            view.SetHowToPlayVisible(false);
            Assert.IsTrue(_howTo.ClassListContains("menu-button-hidden"));

            view.SetHowToPlayVisible(true);
            Assert.IsFalse(_howTo.ClassListContains("menu-button-hidden"));
        }

        [Test]
        public void AMenuWithoutTheButton_StillWorks()
        {
            _root.Remove(_howTo);

            var view = new MenuView(_root, "0.1.1");

            Assert.DoesNotThrow(() => view.SetHowToPlayVisible(true));
        }
    }
}
