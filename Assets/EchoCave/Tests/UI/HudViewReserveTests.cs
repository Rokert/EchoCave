using EchoCave.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // The spare "+1" tag is shown by a USS class on the hint button. The pop animation needs a panel and is checked in Play.
    public class HudViewReserveTests
    {
        private Button _hintButton;
        private HudView _view;

        [SetUp]
        public void SetUp()
        {
            var root = new VisualElement();
            var hud = new VisualElement { name = "hud-root" };
            var pauseButton = new Button { name = "pause-button" };
            pauseButton.Add(new VisualElement { name = "pause-icon" });
            _hintButton = new Button { name = "hint-button" };
            hud.Add(pauseButton);
            hud.Add(new Button { name = "ping-button" });
            hud.Add(_hintButton);
            hud.Add(new Label { name = "hint-count" });
            hud.Add(new Label { name = "moves-value" });
            hud.Add(new Label { name = "level-value" });
            hud.Add(new Label { name = "ping-count" });
            root.Add(hud);

            _view = new HudView(root, 14f, 3, 320);
        }

        [Test]
        public void ASpareInTheReserve_ShowsTheTag_AndNoneHidesIt()
        {
            Assert.IsFalse(_hintButton.ClassListContains("hint-button--reserve"));

            _view.SetHintReserve(1);
            Assert.IsTrue(_hintButton.ClassListContains("hint-button--reserve"));

            _view.SetHintReserve(0);
            Assert.IsFalse(_hintButton.ClassListContains("hint-button--reserve"));
        }

        [Test]
        public void TheTag_DoesNotChangeTheHintButtonState()
        {
            _view.SetHintState(HintButtonState.Ready);

            _view.SetHintReserve(1);

            Assert.IsTrue(_hintButton.ClassListContains("hint-button--ready"));
        }
    }
}
