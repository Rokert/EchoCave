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
        private const string HintReadyClass = "hint-button--ready";
        private const string HintEmptyClass = "hint-button--empty";
        private const string HintAdClass = "hint-button--ad";
        private const string HintReserveClass = "hint-button--reserve";

        private readonly VisualElement _hud;
        private readonly Button _pauseButton;
        private readonly VisualElement _pauseIcon;
        private readonly Button _pingButton;
        private readonly Button _hintButton;
        private readonly Label _movesValue;
        private readonly Label _levelValue;
        private readonly Label _levelCaption;
        private readonly Label _pingCount;
        private readonly Label _hintCount;

        private readonly float _shakeAmplitude;
        private readonly int _shakeCount;
        private readonly int _shakeMillis;
        private ValueAnimation<float> _pingShake;
        private ValueAnimation<float> _hintShake;

        private readonly float _hintPulseScale;
        private readonly int _hintPulseMillis;
        private ValueAnimation<float> _hintPulse;

        public event Action PingPressed;
        public event Action HintPressed;
        public event Action PausePressed;

        // hintPulseScale is how big the hint button swells when a free hint arrives, 1 being its normal size.
        public HudView(
            VisualElement root, float shakeAmplitude, int shakeCount, int shakeMillis, float hintPulseScale = 1.25f,
            int hintPulseMillis = 420)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _hud = Require<VisualElement>(root, "hud-root");
            _pauseButton = Require<Button>(root, "pause-button");
            _pauseIcon = Require<VisualElement>(root, "pause-icon");
            _pingButton = Require<Button>(root, "ping-button");
            _hintButton = Require<Button>(root, "hint-button");
            _movesValue = Require<Label>(root, "moves-value");
            _levelValue = Require<Label>(root, "level-value");

            // Optional: a UXML without the caption simply keeps showing whatever word it has.
            _levelCaption = root.Q<Label>("level-caption");
            _pingCount = Require<Label>(root, "ping-count");
            _hintCount = Require<Label>(root, "hint-count");

            _shakeAmplitude = shakeAmplitude;
            _shakeCount = shakeCount;
            _shakeMillis = shakeMillis;
            _hintPulseScale = hintPulseScale;
            _hintPulseMillis = hintPulseMillis;

            _pauseButton.clicked += () => PausePressed?.Invoke();
            _pingButton.clicked += () => PingPressed?.Invoke();
            _hintButton.clicked += () => HintPressed?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            _hud.EnableInClassList(HiddenClass, !visible);
        }

        public void SetMoves(int movesUsed, int moveLimit)
        {
            _movesValue.text = moveLimit > 0 ? $"{movesUsed} / {moveLimit}" : movesUsed.ToString();
        }

        public void SetLevel(int levelNumber, int levelCount)
        {
            _levelValue.text = $"{levelNumber} / {levelCount}";
        }

        public void SetLevelCaption(string caption)
        {
            if (_levelCaption != null) _levelCaption.text = caption;
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

        public void SetHints(int hintsLeft)
        {
            _hintCount.text = hintsLeft.ToString();
        }

        public void SetHintState(HintButtonState state)
        {
            _hintButton.EnableInClassList(HintReadyClass, state == HintButtonState.Ready);
            _hintButton.EnableInClassList(HintEmptyClass, state == HintButtonState.Empty);
            _hintButton.EnableInClassList(HintAdClass, state == HintButtonState.WatchAd);
        }

        public void SetHintVisible(bool visible)
        {
            _hintButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetHintReserve(int reserveCount)
        {
            _hintButton.EnableInClassList(HintReserveClass, reserveCount > 0);
        }

        public void PulseHint()
        {
            _hintPulse?.Stop();
            _hintPulse = _hintButton.experimental.animation.Start(0f, 1f, _hintPulseMillis, (element, t) =>
            {
                float size = 1f + (_hintPulseScale - 1f) * Mathf.Sin(t * Mathf.PI);
                element.style.scale = new Scale(new Vector2(size, size));
            }).KeepAlive();
            _hintPulse.OnCompleted(() => _hintButton.style.scale = new Scale(Vector2.one));
        }

        public void SetPaused(bool paused)
        {
            _pauseIcon.EnableInClassList(PlayIconClass, paused);
        }

        public void ShakePing()
        {
            _pingShake = Shake(_pingButton, _pingShake);
        }

        public void ShakeHint()
        {
            _hintShake = Shake(_hintButton, _hintShake);
        }

        // UI Toolkit recycles an animation once it ends, and Stop on a recycled one throws. KeepAlive keeps
        // the object valid, so a second refusal can safely cut the first shake short.
        private ValueAnimation<float> Shake(VisualElement button, ValueAnimation<float> running)
        {
            running?.Stop();
            ValueAnimation<float> shake = button.experimental.animation.Start(0f, 1f, _shakeMillis, (element, t) =>
            {
                float x = Mathf.Sin(t * Mathf.PI * 2f * _shakeCount) * _shakeAmplitude * (1f - t);
                element.style.translate = new Translate(new Length(x), new Length(0f));
            }).KeepAlive();
            shake.OnCompleted(() => button.style.translate = new Translate(new Length(0f), new Length(0f)));
            return shake;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null) throw new InvalidOperationException($"The HUD UXML has no {typeof(T).Name} named \"{name}\".");

            return element;
        }
    }
}
