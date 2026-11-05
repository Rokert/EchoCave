using System;
using System.Collections.Generic;
using EchoCave.Game;
using UnityEngine.UIElements;

namespace EchoCave.UI
{
    // Draws the three popups: it finds its elements in the UXML by name and shows state through USS classes
    // and label text. The fade-in and the stars lighting up one by one are USS transitions started by class
    // changes; they need a panel, so without one (in the tests) the final state is applied at once.
    public sealed class PopupsView : IPopupsView
    {
        private const string HiddenClass = "popup-hidden";
        private const string ShownClass = "popup-layer--shown";
        private const string ToggleOnClass = "toggle--on";
        private const string StarOnClass = "star__fill--on";
        private const int StarCount = 3;

        // The classes change one frame after the popup appears, or the transition has nothing to start from.
        private const long ShowDelayMillis = 20;

        private readonly VisualElement _layer;
        private readonly VisualElement _complete;
        private readonly VisualElement _lost;
        private readonly VisualElement _pause;
        private readonly Label _completeMoves;
        private readonly Label _completeNextLabel;
        private readonly Label _lostMoves;
        private readonly VisualElement[] _stars = new VisualElement[StarCount];
        private readonly Button _soundToggle;
        private readonly Button _musicToggle;
        private readonly Button _howToPlay;
        private readonly int _starStaggerMillis;

        // The challenge popups are optional: a UXML without them simply has no challenge screens.
        private readonly VisualElement _challengeResult;
        private readonly VisualElement _challengeList;
        private readonly Label _challengeTitle;
        private readonly VisualElement _challengeLines;
        private readonly Label _challengeMessage;
        private readonly ScrollView _challengeRows;

        // Fade and star tasks still waiting to run, so hiding the popup can cancel them.
        private readonly List<IVisualElementScheduledItem> _pending = new List<IVisualElementScheduledItem>();

        public event Action<PopupAction> ActionPressed;
        public event Action<AudioChannel, bool> ToggleChanged;

        // Play was pressed on a challenge of the list, by its position.
        public event Action<int> ChallengePressed;

        // Raised as each earned star lights, with its position (0 to 2), so a sound can follow it.
        public event Action<int> StarLit;

        public PopupsView(VisualElement root, int starStaggerMillis)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _layer = Require<VisualElement>(root, "popup-layer");
            _complete = Require<VisualElement>(root, "complete-popup");
            _lost = Require<VisualElement>(root, "lost-popup");
            _pause = Require<VisualElement>(root, "pause-popup");
            _completeMoves = Require<Label>(root, "complete-moves");
            _completeNextLabel = Require<Label>(root, "complete-next-label");
            _lostMoves = Require<Label>(root, "lost-moves");
            _soundToggle = Require<Button>(root, "sound-toggle");
            _musicToggle = Require<Button>(root, "music-toggle");
            for (int i = 0; i < StarCount; i++) _stars[i] = Require<VisualElement>(root, "complete-star-" + i);

            _starStaggerMillis = starStaggerMillis;

            Wire(root, "complete-replay", PopupAction.Replay);
            Wire(root, "complete-next", PopupAction.Next);
            Wire(root, "lost-retry", PopupAction.Retry);
            Wire(root, "lost-menu", PopupAction.Menu);
            Wire(root, "pause-resume", PopupAction.Resume);
            Wire(root, "pause-restart", PopupAction.Restart);
            Wire(root, "pause-menu", PopupAction.Menu);

            // Optional: a UXML without the button simply has no way to replay the tutorial from the pause popup.
            _howToPlay = root.Q<Button>("pause-howto");
            if (_howToPlay != null) _howToPlay.clicked += () => ActionPressed?.Invoke(PopupAction.HowToPlay);

            _challengeResult = root.Q<VisualElement>("challenge-popup");
            _challengeList = root.Q<VisualElement>("challenges-popup");
            if (_challengeResult != null && _challengeList != null)
            {
                _challengeTitle = Require<Label>(root, "challenge-title");
                _challengeLines = Require<VisualElement>(root, "challenge-lines");
                _challengeMessage = Require<Label>(root, "challenges-message");
                _challengeRows = Require<ScrollView>(root, "challenges-rows");
                Wire(root, "challenge-retry", PopupAction.Retry);
                Wire(root, "challenge-menu", PopupAction.Menu);
                Wire(root, "challenges-close", PopupAction.CloseChallenges);
            }
            else
            {
                _challengeResult = null;
                _challengeList = null;
            }

            _soundToggle.clicked += () => Flip(AudioChannel.Sound, _soundToggle);
            _musicToggle.clicked += () => Flip(AudioChannel.Music, _musicToggle);
        }

        public void SetHowToPlayVisible(bool visible)
        {
            _howToPlay?.EnableInClassList(HiddenClass, !visible);
        }

        public void Hide()
        {
            CancelPending();

            _layer.AddToClassList(HiddenClass);
            _layer.RemoveFromClassList(ShownClass);
            foreach (VisualElement popup in Popups()) popup.AddToClassList(HiddenClass);
        }

        public void ShowComplete(int stars, int movesUsed, int moveLimit, bool hasNext)
        {
            _completeMoves.text = MovesText(movesUsed, moveLimit);
            _completeNextLabel.text = hasNext ? "Next" : "Menu";

            foreach (VisualElement star in _stars) star.RemoveFromClassList(StarOnClass);

            Reveal(_complete);
            LightStars(stars);
        }

        public void ShowLost(int movesUsed, int moveLimit)
        {
            _lostMoves.text = MovesText(movesUsed, moveLimit);
            Reveal(_lost);
        }

        public void ShowPause(bool soundOn, bool musicOn)
        {
            _soundToggle.EnableInClassList(ToggleOnClass, soundOn);
            _musicToggle.EnableInClassList(ToggleOnClass, musicOn);
            Reveal(_pause);
        }

        public void ShowChallengeResult(string title, IReadOnlyList<string> lines)
        {
            if (_challengeResult == null) return;

            _challengeTitle.text = title;
            _challengeLines.Clear();
            foreach (string line in lines) _challengeLines.Add(Text(line, "popup__text", "font-medium"));
            Reveal(_challengeResult);
        }

        public void ShowChallenges(IReadOnlyList<ChallengeRowData> rows, string message)
        {
            if (_challengeList == null) return;

            _challengeMessage.text = message ?? string.Empty;
            _challengeMessage.EnableInClassList(HiddenClass, string.IsNullOrEmpty(message));

            _challengeRows.contentContainer.Clear();
            for (int i = 0; i < rows.Count; i++) _challengeRows.contentContainer.Add(BuildRow(i, rows[i]));

            Reveal(_challengeList);
        }

        // One card per challenge: its name, how it went, the best results and the Play button. The button is built here
        // with the same classes the UXML gives the others, so it looks and sounds like them.
        private VisualElement BuildRow(int index, ChallengeRowData row)
        {
            var card = new VisualElement();
            card.AddToClassList("challenge-row");
            card.Add(Text(row.Title, "challenge-row__title", "font-bold"));
            card.Add(Text(row.Summary, "challenge-row__summary", "font-medium"));
            foreach (string line in row.Lines) card.Add(Text(line, "challenge-row__line", "font-medium"));

            var button = new Button();
            button.AddToClassList("pill-button");
            var lip = new VisualElement { pickingMode = PickingMode.Ignore };
            lip.AddToClassList("pill-button__lip");
            var face = new VisualElement { pickingMode = PickingMode.Ignore };
            face.AddToClassList("pill-button__face");
            face.Add(Text(row.CanPlay ? "Play" : "Locked", "pill-button__label", "font-semibold"));
            button.Add(lip);
            button.Add(face);

            if (row.CanPlay)
            {
                button.clicked += () => ChallengePressed?.Invoke(index);
            }
            else
            {
                button.AddToClassList("pill-button--locked");
                button.SetEnabled(false);
            }

            card.Add(button);
            return card;
        }

        private static Label Text(string text, params string[] classes)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (string name in classes) label.AddToClassList(name);
            return label;
        }

        private IEnumerable<VisualElement> Popups()
        {
            yield return _complete;
            yield return _lost;
            yield return _pause;
            if (_challengeResult != null) yield return _challengeResult;
            if (_challengeList != null) yield return _challengeList;
        }

        private static string MovesText(int movesUsed, int moveLimit)
        {
            return moveLimit > 0 ? $"Moves {movesUsed} / {moveLimit}" : $"Moves {movesUsed}";
        }

        // Shows one popup and hides the other two. The scrim fades in on the next frame.
        private void Reveal(VisualElement popup)
        {
            CancelPending();

            foreach (VisualElement other in Popups()) other.EnableInClassList(HiddenClass, other != popup);

            _layer.RemoveFromClassList(HiddenClass);
            _layer.RemoveFromClassList(ShownClass);

            if (_layer.panel == null) _layer.AddToClassList(ShownClass);
            else Schedule(_layer, () => _layer.AddToClassList(ShownClass), ShowDelayMillis);
        }

        // The earned stars light up one after another, after the popup has started to appear.
        private void LightStars(int stars)
        {
            int earned = Math.Max(0, Math.Min(StarCount, stars));
            for (int i = 0; i < earned; i++)
            {
                VisualElement star = _stars[i];
                int index = i;
                if (_layer.panel == null)
                {
                    LightStar(star, index);
                    continue;
                }

                Schedule(star, () => LightStar(star, index), ShowDelayMillis + _starStaggerMillis * (i + 1));
            }
        }

        private void LightStar(VisualElement star, int index)
        {
            star.AddToClassList(StarOnClass);
            StarLit?.Invoke(index);
        }

        private void Schedule(VisualElement element, Action action, long delayMillis)
        {
            _pending.Add(element.schedule.Execute(action).StartingIn(delayMillis));
        }

        private void CancelPending()
        {
            foreach (IVisualElementScheduledItem item in _pending) item.Pause();
            _pending.Clear();
        }

        private void Flip(AudioChannel channel, Button toggle)
        {
            bool on = !toggle.ClassListContains(ToggleOnClass);
            toggle.EnableInClassList(ToggleOnClass, on);
            ToggleChanged?.Invoke(channel, on);
        }

        private void Wire(VisualElement root, string name, PopupAction action)
        {
            Require<Button>(root, name).clicked += () => ActionPressed?.Invoke(action);
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null) throw new InvalidOperationException($"The popups UXML has no {typeof(T).Name} named \"{name}\".");

            return element;
        }
    }
}
