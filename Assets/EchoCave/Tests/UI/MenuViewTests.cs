using System;
using EchoCave.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // The view is built here by hand with the names the UXML gives, so what it shows can be read from the
    // USS classes without a panel. The Play button forwarding its click needs a real pointer event, so it is
    // checked in Play.
    public class MenuViewTests
    {
        private VisualElement _root;
        private VisualElement _layer;
        private Button _play;
        private MenuView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _layer = new VisualElement { name = "menu-layer" };
            _play = new Button { name = "play-button" };
            _layer.Add(_play);
            _root.Add(_layer);
            _view = new MenuView(_root);
        }

        private bool IsShown => !_layer.ClassListContains("menu-hidden");

        [Test]
        public void ShowAndHide_ToggleTheMenuLayer()
        {
            _view.Show();
            Assert.IsTrue(IsShown);

            _view.Hide();
            Assert.IsFalse(IsShown);
        }

        [Test]
        public void AMissingElement_IsReportedByName()
        {
            _layer.Remove(_play);

            var thrown = Assert.Throws<InvalidOperationException>(() => new MenuView(_root));

            StringAssert.Contains("play-button", thrown.Message);
        }

        [Test]
        public void ANullRoot_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new MenuView(null));
        }
    }
}
