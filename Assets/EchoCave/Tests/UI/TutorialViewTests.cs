using System;
using EchoCave.Core;
using EchoCave.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // The view is built here by hand with the names the UXML gives, so the state it shows through USS classes and label
    // text can be checked without a panel. The ring's position and pulse need a panel and are checked in Play.
    public class TutorialViewTests
    {
        private sealed class FakeTargets : ITutorialTargets
        {
            public bool TryGetBatRect(int batId, out Rect rect)
            {
                rect = default;
                return false;
            }
        }

        private VisualElement _root;
        private VisualElement _layer;
        private VisualElement _ring;
        private Label _text;
        private Button _continue;
        private Label _continueLabel;
        private Button _retry;
        private Button _skip;
        private TutorialView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _layer = new VisualElement { name = "tutorial-layer" };
            _ring = new VisualElement { name = "tutorial-ring" };
            _text = new Label { name = "tutorial-text" };
            _continue = new Button { name = "tutorial-continue" };
            _continueLabel = new Label { name = "tutorial-continue-label" };
            _continue.Add(_continueLabel);
            _retry = new Button { name = "tutorial-retry" };
            _skip = new Button { name = "tutorial-skip" };
            _layer.Add(_ring);
            _layer.Add(_text);
            _layer.Add(_continue);
            _layer.Add(_retry);
            _layer.Add(_skip);
            _root.Add(_layer);

            _view = new TutorialView(_root, new FakeTargets());
        }

        [TearDown]
        public void TearDown()
        {
            _view.Dispose();
        }

        [Test]
        public void SetVisible_ShowsAndHidesTheLayer()
        {
            _view.SetVisible(false);
            Assert.IsTrue(_layer.ClassListContains("tutorial-hidden"));

            _view.SetVisible(true);
            Assert.IsFalse(_layer.ClassListContains("tutorial-hidden"));
        }

        [Test]
        public void SetText_ShowsTheText()
        {
            _view.SetText("Tap the bat.");

            Assert.AreEqual("Tap the bat.", _text.text);
        }

        [Test]
        public void SetContinue_ShowsTheButton_WithItsLabel()
        {
            _view.SetContinue(true, "Next");

            Assert.IsFalse(_continue.ClassListContains("tutorial-button-hidden"));
            Assert.AreEqual("Next", _continueLabel.text);

            _view.SetContinue(false, string.Empty);
            Assert.IsTrue(_continue.ClassListContains("tutorial-button-hidden"));
        }

        [Test]
        public void SetRetry_ShowsAndHidesTheButton()
        {
            _view.SetRetry(true);
            Assert.IsFalse(_retry.ClassListContains("tutorial-button-hidden"));

            _view.SetRetry(false);
            Assert.IsTrue(_retry.ClassListContains("tutorial-button-hidden"));
        }

        [Test]
        public void AFocusWithNothingToPointAt_KeepsTheRingHidden()
        {
            // The fake targets know no bat, and there is no HUD in this tree: nothing can be pointed at.
            _view.SetFocus(TutorialFocus.Bat, 0);
            Assert.IsTrue(_ring.ClassListContains("tutorial-hidden"));

            _view.SetFocus(TutorialFocus.PingButton, -1);
            Assert.IsTrue(_ring.ClassListContains("tutorial-hidden"));

            _view.SetFocus(TutorialFocus.HintButton, -1);
            Assert.IsTrue(_ring.ClassListContains("tutorial-hidden"));
        }

        [Test]
        public void HidingTheOverlay_ClearsTheFocus()
        {
            _view.SetFocus(TutorialFocus.Bat, 0);

            _view.SetVisible(false);

            Assert.IsTrue(_ring.ClassListContains("tutorial-hidden"));
        }

        [Test]
        public void AMissingElement_IsReportedByName()
        {
            _layer.Remove(_skip);

            var thrown = Assert.Throws<InvalidOperationException>(() => new TutorialView(_root, new FakeTargets()));

            StringAssert.Contains("tutorial-skip", thrown.Message);
        }

        [Test]
        public void MissingParts_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new TutorialView(null, new FakeTargets()));
            Assert.Throws<ArgumentNullException>(() => new TutorialView(_root, null));
        }
    }
}
