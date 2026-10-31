using System;
using EchoCave.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // The view is built here by hand with the names the UXML gives, so the state it shows through USS classes
    // and label text can be checked without a panel. Clicks and the shake need a panel and are checked in Play.
    public class HudViewTests
    {
        private VisualElement _root;
        private VisualElement _hud;
        private VisualElement _pauseIcon;
        private Button _pingButton;
        private Label _movesValue;
        private Label _levelValue;
        private Label _pingCount;
        private HudView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _hud = new VisualElement { name = "hud-root" };
            _pauseIcon = new VisualElement { name = "pause-icon" };
            _pingButton = new Button { name = "ping-button" };
            _movesValue = new Label { name = "moves-value" };
            _levelValue = new Label { name = "level-value" };
            _pingCount = new Label { name = "ping-count" };

            var pauseButton = new Button { name = "pause-button" };
            pauseButton.Add(_pauseIcon);
            _hud.Add(pauseButton);
            _hud.Add(_pingButton);
            _hud.Add(_movesValue);
            _hud.Add(_levelValue);
            _hud.Add(_pingCount);
            _root.Add(_hud);

            _view = new HudView(_root, 14f, 3, 320);
        }

        [Test]
        public void SetLevel_ShowsTheNumberOverTheTotal()
        {
            _view.SetLevel(3, 10);

            Assert.AreEqual("3 / 10", _levelValue.text);
        }

        [Test]
        public void SetMoves_ShowsUsedOverLimit_WhenTheLevelHasALimit()
        {
            _view.SetMoves(3, 9);

            Assert.AreEqual("3 / 9", _movesValue.text);
        }

        [Test]
        public void SetMoves_ShowsOnlyTheCount_WhenThereIsNoLimit()
        {
            _view.SetMoves(12, 0);

            Assert.AreEqual("12", _movesValue.text);
        }

        [Test]
        public void SetPings_ShowsTheCountOnTheBadge()
        {
            _view.SetPings(4);

            Assert.AreEqual("4", _pingCount.text);
        }

        [Test]
        public void SetPingState_ShowsExactlyOneStateAtATime()
        {
            _view.SetPingState(PingButtonState.Ready);
            AssertOnly("ping-button--ready");

            _view.SetPingState(PingButtonState.Active);
            AssertOnly("ping-button--active");

            _view.SetPingState(PingButtonState.Empty);
            AssertOnly("ping-button--empty");
        }

        private void AssertOnly(string expected)
        {
            foreach (string state in new[] { "ping-button--ready", "ping-button--active", "ping-button--empty" })
                Assert.AreEqual(state == expected, _pingButton.ClassListContains(state), state);
        }

        [Test]
        public void SetPaused_SwapsThePauseIconForThePlayIcon()
        {
            _view.SetPaused(true);
            Assert.IsTrue(_pauseIcon.ClassListContains("round-button__icon--play"));

            _view.SetPaused(false);
            Assert.IsFalse(_pauseIcon.ClassListContains("round-button__icon--play"));
        }

        [Test]
        public void SetVisible_TogglesTheHiddenClass()
        {
            _view.SetVisible(false);
            Assert.IsTrue(_hud.ClassListContains("hud-hidden"));

            _view.SetVisible(true);
            Assert.IsFalse(_hud.ClassListContains("hud-hidden"));
        }

        [Test]
        public void AMissingElement_IsReportedByName()
        {
            _hud.Remove(_pingButton);

            var thrown = Assert.Throws<InvalidOperationException>(() => new HudView(_root, 14f, 3, 320));

            StringAssert.Contains("ping-button", thrown.Message);
        }

        [Test]
        public void ANullRoot_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new HudView(null, 14f, 3, 320));
        }
    }
}
