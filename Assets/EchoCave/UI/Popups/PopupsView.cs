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
        private readonly int _starStaggerMillis;

        // Fade and star tasks still waiting to run, so hiding the popup can cancel them.
        private readonly List<IVisualElementScheduledItem> _pending = new List<IVisualElementScheduledItem>();

        public event Action<PopupAction> ActionPressed;
        public event Action<AudioChannel, bool> ToggleChanged;

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

            _soundToggle.clicked += () => Flip(AudioChannel.Sound, _soundToggle);
            _musicToggle.clicked += () => Flip(AudioChannel.Music, _musicToggle);
        }

        public void Hide()
        {
            CancelPending();

            _layer.AddToClassList(HiddenClass);
            _layer.RemoveFromClassList(ShownClass);
            _complete.AddToClassList(HiddenClass);
            _lost.AddToClassList(HiddenClass);
            _pause.AddToClassList(HiddenClass);
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

        private static string MovesText(int movesUsed, int moveLimit)
        {
            return moveLimit > 0 ? $"Moves {movesUsed} / {moveLimit}" : $"Moves {movesUsed}";
        }

        // Shows one popup and hides the other two. The scrim fades in on the next frame.
        private void Reveal(VisualElement popup)
        {
            CancelPending();

            _complete.EnableInClassList(HiddenClass, popup != _complete);
            _lost.EnableInClassList(HiddenClass, popup != _lost);
            _pause.EnableInClassList(HiddenClass, popup != _pause);

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
