using System;
using EchoCave.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // The view is built here by hand with the names the UXML gives, so the state it shows through USS classes
    // and label text can be checked without a panel. Without a panel the fade and the star animation are
    // skipped and the final state is applied at once; the animations themselves are checked in Play.
    public class PopupsViewTests
    {
        private VisualElement _root;
        private VisualElement _layer;
        private VisualElement _complete;
        private VisualElement _lost;
        private VisualElement _pause;
        private Label _completeMoves;
        private Label _completeNextLabel;
        private Label _lostMoves;
        private VisualElement[] _stars;
        private Button _soundToggle;
        private Button _musicToggle;
        private PopupsView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _layer = new VisualElement { name = "popup-layer" };
            _complete = new VisualElement { name = "complete-popup" };
            _lost = new VisualElement { name = "lost-popup" };
            _pause = new VisualElement { name = "pause-popup" };
            _completeMoves = new Label { name = "complete-moves" };
            _completeNextLabel = new Label { name = "complete-next-label" };
            _lostMoves = new Label { name = "lost-moves" };
            _soundToggle = new Button { name = "sound-toggle" };
            _musicToggle = new Button { name = "music-toggle" };

            _stars = new VisualElement[3];
            for (int i = 0; i < 3; i++)
            {
                _stars[i] = new VisualElement { name = "complete-star-" + i };
                _complete.Add(_stars[i]);
            }

            _complete.Add(_completeMoves);
            _lost.Add(_lostMoves);
            foreach (string name in new[] { "complete-replay", "complete-next", "lost-retry", "lost-menu", "pause-resume", "pause-restart", "pause-menu" })
                _layer.Add(new Button { name = name });
            _layer.Add(_completeNextLabel);
            _pause.Add(_soundToggle);
            _pause.Add(_musicToggle);

            _layer.Add(_complete);
            _layer.Add(_lost);
            _layer.Add(_pause);
            _root.Add(_layer);

            _view = new PopupsView(_root, 280);
        }

        private static bool IsShown(VisualElement element) => !element.ClassListContains("popup-hidden");

        private bool StarIsOn(int index) => _stars[index].ClassListContains("star__fill--on");

        [Test]
        public void Hide_HidesTheScrimAndEveryPopup()
        {
            _view.ShowLost(9, 9);

            _view.Hide();

            Assert.IsFalse(IsShown(_layer));
            Assert.IsFalse(_layer.ClassListContains("popup-layer--shown"));
            Assert.IsFalse(IsShown(_complete));
            Assert.IsFalse(IsShown(_lost));
            Assert.IsFalse(IsShown(_pause));
        }

        [Test]
        public void ShowComplete_ShowsOnlyThatPopup_OnTheScrim()
        {
            _view.ShowComplete(3, 7, 9, true);

            Assert.IsTrue(IsShown(_layer));
            Assert.IsTrue(_layer.ClassListContains("popup-layer--shown"));
            Assert.IsTrue(IsShown(_complete));
            Assert.IsFalse(IsShown(_lost));
            Assert.IsFalse(IsShown(_pause));
        }

        [Test]
        public void ShowComplete_ShowsTheMoves_WithTheLimitWhenThereIsOne()
        {
            _view.ShowComplete(2, 9, 12, true);
            Assert.AreEqual("Moves 9 / 12", _completeMoves.text);

            _view.ShowComplete(2, 9, 0, true);
            Assert.AreEqual("Moves 9", _completeMoves.text);
        }

        [Test]
        public void ShowComplete_NextReadsMenu_WhenThereIsNoNextLevel()
        {
            _view.ShowComplete(3, 5, 9, true);
            Assert.AreEqual("Next", _completeNextLabel.text);

            _view.ShowComplete(3, 5, 9, false);
            Assert.AreEqual("Menu", _completeNextLabel.text);
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        public void ShowComplete_LightsExactlyTheEarnedStars(int earned, int expectedOn)
        {
            _view.ShowComplete(earned, 5, 9, true);

            int on = 0;
            for (int i = 0; i < 3; i++)
            {
                if (StarIsOn(i)) on++;
            }

            Assert.AreEqual(expectedOn, on);
            for (int i = 0; i < expectedOn; i++) Assert.IsTrue(StarIsOn(i), "stars light up from the left");
        }

        [TestCase(5)]
        [TestCase(-2)]
        public void ShowComplete_KeepsTheStarsBetweenZeroAndThree(int earned)
        {
            _view.ShowComplete(earned, 5, 9, true);

            int expected = Math.Max(0, Math.Min(3, earned));
            int on = 0;
            for (int i = 0; i < 3; i++)
            {
                if (StarIsOn(i)) on++;
            }

            Assert.AreEqual(expected, on);
        }

        [Test]
        public void ShowComplete_StartsFromDarkStars_EachTime()
        {
            _view.ShowComplete(3, 5, 9, true);

            _view.ShowComplete(1, 8, 9, true);

            Assert.IsTrue(StarIsOn(0));
            Assert.IsFalse(StarIsOn(1), "stars from the previous win must not stay lit");
            Assert.IsFalse(StarIsOn(2));
        }

        [Test]
        public void ShowLost_ShowsOnlyThatPopup_WithTheMoves()
        {
            _view.ShowLost(9, 9);

            Assert.IsTrue(IsShown(_lost));
            Assert.IsFalse(IsShown(_complete));
            Assert.IsFalse(IsShown(_pause));
            Assert.AreEqual("Moves 9 / 9", _lostMoves.text);
        }

        [Test]
        public void ShowPause_ShowsTheSwitchesAsGiven()
        {
            _view.ShowPause(true, false);

            Assert.IsTrue(IsShown(_pause));
            Assert.IsTrue(_soundToggle.ClassListContains("toggle--on"));
            Assert.IsFalse(_musicToggle.ClassListContains("toggle--on"));

            _view.ShowPause(false, true);

            Assert.IsFalse(_soundToggle.ClassListContains("toggle--on"));
            Assert.IsTrue(_musicToggle.ClassListContains("toggle--on"));
        }

        [Test]
        public void ShowingAnotherPopup_HidesThePrevious()
        {
            _view.ShowLost(9, 9);
            _view.ShowPause(true, true);

            Assert.IsTrue(IsShown(_pause));
            Assert.IsFalse(IsShown(_lost));
        }

        [Test]
        public void AMissingElement_IsReportedByName()
        {
            _layer.Remove(_completeNextLabel);

            var thrown = Assert.Throws<InvalidOperationException>(() => new PopupsView(_root, 280));

            StringAssert.Contains("complete-next-label", thrown.Message);
        }

        [Test]
        public void ANullRoot_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new PopupsView(null, 280));
        }

        [Test]
        public void EachEarnedStar_RaisesStarLit_InOrder_AndNoOtherDoes()
        {
            var lit = new System.Collections.Generic.List<int>();
            _view.StarLit += lit.Add;

            _view.ShowComplete(2, 5, 9, true);

            CollectionAssert.AreEqual(new[] { 0, 1 }, lit);
        }

        [Test]
        public void ThreeStars_RaiseThreeSounds_AndZeroStarsNone()
        {
            var lit = new System.Collections.Generic.List<int>();
            _view.StarLit += lit.Add;

            _view.ShowComplete(3, 5, 9, true);
            Assert.AreEqual(3, lit.Count);

            lit.Clear();
            _view.ShowComplete(0, 9, 9, true);
            Assert.IsEmpty(lit);
        }
    }
}
