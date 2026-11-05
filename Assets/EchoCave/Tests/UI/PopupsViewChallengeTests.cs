using System.Linq;
using EchoCave.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EchoCave.Tests
{
    // The challenge screens of the popups view, built by hand with the names the UXML gives them (like PopupsViewTests).
    // Without a panel the fade is skipped, so the final state is applied at once. Clicks need a panel and are checked in Play.
    public class PopupsViewChallengeTests
    {
        private VisualElement _root;
        private VisualElement _layer;
        private VisualElement _resultPopup;
        private VisualElement _listPopup;
        private Label _title;
        private VisualElement _lines;
        private Label _message;
        private ScrollView _rows;

        private static void AddCommon(VisualElement layer)
        {
            foreach (string name in new[] { "complete-popup", "lost-popup", "pause-popup" }) layer.Add(new VisualElement { name = name });
            foreach (string name in new[] { "complete-moves", "complete-next-label", "lost-moves" }) layer.Add(new Label { name = name });
            foreach (string name in new[] { "complete-replay", "complete-next", "lost-retry", "lost-menu", "pause-resume", "pause-restart", "pause-menu", "sound-toggle", "music-toggle" })
                layer.Add(new Button { name = name });
            for (int i = 0; i < 3; i++) layer.Add(new VisualElement { name = "complete-star-" + i });
        }

        private PopupsView Build(bool withChallenges = true)
        {
            _root = new VisualElement();
            _layer = new VisualElement { name = "popup-layer" };
            AddCommon(_layer);

            if (withChallenges)
            {
                _resultPopup = new VisualElement { name = "challenge-popup" };
                _listPopup = new VisualElement { name = "challenges-popup" };
                _title = new Label { name = "challenge-title" };
                _lines = new VisualElement { name = "challenge-lines" };
                _message = new Label { name = "challenges-message" };
                _rows = new ScrollView { name = "challenges-rows" };
                _layer.Add(_resultPopup);
                _layer.Add(_listPopup);
                _layer.Add(_title);
                _layer.Add(_lines);
                _layer.Add(_message);
                _layer.Add(_rows);
                foreach (string name in new[] { "challenge-retry", "challenge-menu", "challenges-close" }) _layer.Add(new Button { name = name });
            }

            _root.Add(_layer);
            return new PopupsView(_root, 280);
        }

        private static bool Hidden(VisualElement element) => element.ClassListContains("popup-hidden");

        private static ChallengeRowData Row(string title, bool canPlay, params string[] lines)
        {
            return new ChallengeRowData(title, title + " summary", lines, canPlay);
        }

        [Test]
        public void ShowChallengeResult_ShowsOnlyThatPopup_WithItsTitleAndOneLabelPerLine()
        {
            PopupsView view = Build();

            view.ShowChallengeResult("New best!", new[] { "52 taps in 2:20.5", "Rank 1 on this device" });

            Assert.IsFalse(Hidden(_resultPopup));
            Assert.IsTrue(Hidden(_listPopup));
            Assert.IsFalse(_layer.ClassListContains("popup-hidden"));
            Assert.AreEqual("New best!", _title.text);
            CollectionAssert.AreEqual(new[] { "52 taps in 2:20.5", "Rank 1 on this device" }, _lines.Children().OfType<Label>().Select(l => l.text).ToArray());
        }

        [Test]
        public void ShowingTheResultTwice_ReplacesTheLines_NotAppendsThem()
        {
            PopupsView view = Build();

            view.ShowChallengeResult("A", new[] { "one", "two", "three" });
            view.ShowChallengeResult("B", new[] { "only" });

            Assert.AreEqual(1, _lines.childCount);
            Assert.AreEqual("B", _title.text);
        }

        [Test]
        public void ShowChallenges_BuildsOneCardPerChallenge_WithItsTitleSummaryAndLines()
        {
            PopupsView view = Build();

            view.ShowChallenges(new[] { Row("challenge-001", true, "1. 52 taps in 2:20.5"), Row("challenge-002", true) }, string.Empty);

            Assert.IsFalse(Hidden(_listPopup));
            Assert.IsTrue(Hidden(_resultPopup));
            VisualElement[] cards = _rows.contentContainer.Children().ToArray();
            Assert.AreEqual(2, cards.Length);
            string[] texts = cards[0].Query<Label>().ToList().Select(l => l.text).ToArray();
            CollectionAssert.AreEqual(new[] { "challenge-001", "challenge-001 summary", "1. 52 taps in 2:20.5", "Play" }, texts);
        }

        [Test]
        public void ShowingTheListAgain_ReplacesTheCards()
        {
            PopupsView view = Build();

            view.ShowChallenges(new[] { Row("a", true), Row("b", true), Row("c", true) }, string.Empty);
            view.ShowChallenges(new[] { Row("a", true) }, string.Empty);

            Assert.AreEqual(1, _rows.contentContainer.childCount);
        }

        [Test]
        public void ALockedCard_ShowsLocked_AndIsDimmedAndDisabled()
        {
            PopupsView view = Build();
            view.ShowChallenges(new[] { Row("a", false) }, "Win 3 more levels to unlock");

            Button button = _rows.contentContainer.Q<Button>();
            Assert.IsFalse(button.enabledSelf);
            Assert.IsTrue(button.ClassListContains("pill-button--locked"));
            Assert.AreEqual("Locked", button.Q<Label>().text);
        }

        [Test]
        public void TheMessage_ShowsWhenThereIsOne_AndHidesWhenThereIsNone()
        {
            PopupsView view = Build();

            view.ShowChallenges(new[] { Row("a", false) }, "Win 3 more levels to unlock");
            Assert.IsFalse(Hidden(_message));
            Assert.AreEqual("Win 3 more levels to unlock", _message.text);

            view.ShowChallenges(new[] { Row("a", true) }, string.Empty);
            Assert.IsTrue(Hidden(_message));
        }

        [Test]
        public void Hide_HidesTheChallengePopupsToo()
        {
            PopupsView view = Build();
            view.ShowChallengeResult("x", new[] { "y" });

            view.Hide();

            Assert.IsTrue(Hidden(_resultPopup));
            Assert.IsTrue(Hidden(_listPopup));
            Assert.IsTrue(_layer.ClassListContains("popup-hidden"));
        }

        [Test]
        public void ShowingAnotherPopup_HidesTheChallengeOne()
        {
            PopupsView view = Build();
            view.ShowChallengeResult("x", new[] { "y" });

            view.ShowLost(3, 4);

            Assert.IsTrue(Hidden(_resultPopup));
        }

        [Test]
        public void AUxmlWithoutTheChallengePopups_StillWorks_AndTheChallengeScreensDoNothing()
        {
            PopupsView view = Build(withChallenges: false);

            Assert.DoesNotThrow(() => view.ShowChallengeResult("x", new[] { "y" }));
            Assert.DoesNotThrow(() => view.ShowChallenges(new[] { Row("a", true) }, string.Empty));
            Assert.DoesNotThrow(() => view.Hide());
            Assert.IsTrue(_layer.ClassListContains("popup-hidden"), "nothing was shown");
        }
    }
}
