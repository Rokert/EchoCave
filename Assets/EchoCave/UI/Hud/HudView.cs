using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace EchoCave.UI
{
    // Draws the HUD: it finds its elements in the UXML by name, shows state through USS classes, and forwards
    // the two buttons. No decisions.
    public sealed class HudView : IHudView
    {
        private const string HiddenClass = "hud-hidden";
        private const string PlayIconClass = "round-button__icon--play";
        private const string ReadyClass = "ping-button--ready";
        private const string ActiveClass = "ping-button--active";
        private const string EmptyClass = "ping-button--empty";

        private readonly VisualElement _hud;
        private readonly Button _pauseButton;
        private readonly VisualElement _pauseIcon;
        private readonly Button _pingButton;
        private readonly Label _movesValue;
        private readonly Label _pingCount;

        private readonly float _shakeAmplitude;
        private readonly int _shakeCount;
        private readonly int _shakeMillis;
        private ValueAnimation<float> _shake;

        public event Action PingPressed;
        public event Action PausePressed;

        public HudView(VisualElement root, float shakeAmplitude, int shakeCount, int shakeMillis)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _hud = Require<VisualElement>(root, "hud-root");
            _pauseButton = Require<Button>(root, "pause-button");
            _pauseIcon = Require<VisualElement>(root, "pause-icon");
            _pingButton = Require<Button>(root, "ping-button");
            _movesValue = Require<Label>(root, "moves-value");
            _pingCount = Require<Label>(root, "ping-count");

            _shakeAmplitude = shakeAmplitude;
            _shakeCount = shakeCount;
            _shakeMillis = shakeMillis;

            _pauseButton.clicked += () => PausePressed?.Invoke();
            _pingButton.clicked += () => PingPressed?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            _hud.EnableInClassList(HiddenClass, !visible);
        }

        public void SetMoves(int movesUsed, int moveLimit)
        {
            _movesValue.text = moveLimit > 0 ? $"{movesUsed} / {moveLimit}" : movesUsed.ToString();
        }

        public void SetPings(int pingsLeft)
        {
            _pingCount.text = pingsLeft.ToString();
        }

        public void SetPingState(PingButtonState state)
        {
            _pingButton.EnableInClassList(ReadyClass, state == PingButtonState.Ready);
            _pingButton.EnableInClassList(ActiveClass, state == PingButtonState.Active);
            _pingButton.EnableInClassList(EmptyClass, state == PingButtonState.Empty);
        }

        public void SetPaused(bool paused)
        {
            _pauseIcon.EnableInClassList(PlayIconClass, paused);
        }

        public void ShakePing()
        {
            // UI Toolkit recycles an animation once it ends, and Stop on a recycled one throws. KeepAlive keeps
            // the object valid, so a second refusal can safely cut the first shake short.
            _shake?.Stop();
            _shake = _pingButton.experimental.animation.Start(0f, 1f, _shakeMillis, (element, t) =>
            {
                float x = Mathf.Sin(t * Mathf.PI * 2f * _shakeCount) * _shakeAmplitude * (1f - t);
                element.style.translate = new Translate(new Length(x), new Length(0f));
            }).KeepAlive();
            _shake.OnCompleted(() => _pingButton.style.translate = new Translate(new Length(0f), new Length(0f)));
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null) throw new InvalidOperationException($"The HUD UXML has no {typeof(T).Name} named \"{name}\".");

            return element;
        }
    }
}
