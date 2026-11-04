using System;
using EchoCave.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoCave.UI
{
    // Draws the tutorial overlay: it finds its elements in the UXML by name, shows state through USS classes, forwards
    // the three buttons, and keeps a pulsing ring on the bat or the button the step points at. No decisions.
    public sealed class TutorialView : ITutorialView, IDisposable
    {
        private const string HiddenClass = "tutorial-hidden";
        private const string ButtonHiddenClass = "tutorial-button-hidden";
        private const long RefreshMillis = 33;

        private readonly VisualElement _layer;
        private readonly VisualElement _ring;
        private readonly Label _text;
        private readonly Button _continue;
        private readonly Label _continueLabel;
        private readonly Button _retry;
        private readonly Button _skip;
        private readonly Button _pingButton;
        private readonly Button _hintButton;
        private readonly ITutorialTargets _targets;
        private readonly float _ringPadding;
        private readonly float _pulseScale;
        private readonly float _pulseSeconds;
        private readonly IVisualElementScheduledItem _refresh;

        private TutorialFocus _focus;
        private int _batId;

        public event Action ContinuePressed;
        public event Action RetryPressed;
        public event Action SkipPressed;

        // ringPadding grows the ring beyond the thing it points at, in panel units; the ring swells by pulseScale
        // (1 being no swell) once every pulseSeconds.
        public TutorialView(VisualElement root, ITutorialTargets targets, float ringPadding = 14f, float pulseScale = 1.1f, float pulseSeconds = 0.9f)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _targets = targets ?? throw new ArgumentNullException(nameof(targets));
            _layer = Require<VisualElement>(root, "tutorial-layer");
            _ring = Require<VisualElement>(root, "tutorial-ring");
            _text = Require<Label>(root, "tutorial-text");
            _continue = Require<Button>(root, "tutorial-continue");
            _continueLabel = Require<Label>(root, "tutorial-continue-label");
            _retry = Require<Button>(root, "tutorial-retry");
            _skip = Require<Button>(root, "tutorial-skip");

            // The two buttons belong to the HUD; without them the ring just has nothing to point at.
            _pingButton = root.Q<Button>("ping-button");
            _hintButton = root.Q<Button>("hint-button");

            _ringPadding = ringPadding;
            _pulseScale = pulseScale;
            _pulseSeconds = Mathf.Max(0.1f, pulseSeconds);

            _continue.clicked += () => ContinuePressed?.Invoke();
            _retry.clicked += () => RetryPressed?.Invoke();
            _skip.clicked += () => SkipPressed?.Invoke();

            _refresh = _layer.schedule.Execute(UpdateRing).Every(RefreshMillis);
            _refresh.Pause();
        }

        public void Dispose()
        {
            _refresh.Pause();
        }

        public void SetVisible(bool visible)
        {
            _layer.EnableInClassList(HiddenClass, !visible);
            if (!visible) SetFocus(TutorialFocus.None, -1);
        }

        public void SetText(string text)
        {
            _text.text = text;
        }

        public void SetContinue(bool visible, string label)
        {
            _continue.EnableInClassList(ButtonHiddenClass, !visible);
            _continueLabel.text = label;
        }

        public void SetRetry(bool visible)
        {
            _retry.EnableInClassList(ButtonHiddenClass, !visible);
        }

        public void SetFocus(TutorialFocus focus, int batId)
        {
            _focus = focus;
            _batId = batId;

            if (focus == TutorialFocus.None)
            {
                _refresh.Pause();
                _ring.EnableInClassList(HiddenClass, true);
                return;
            }

            UpdateRing();
            _refresh.Resume();
        }

        // Follows the target every few milliseconds: a bat moves, the camera reframes on a new level, the phone rotates.
        private void UpdateRing()
        {
            if (!TryGetTarget(out Rect target))
            {
                _ring.EnableInClassList(HiddenClass, true);
                return;
            }

            Vector2 topLeft = _layer.WorldToLocal(target.position);
            _ring.style.left = topLeft.x - _ringPadding;
            _ring.style.top = topLeft.y - _ringPadding;
            _ring.style.width = target.width + _ringPadding * 2f;
            _ring.style.height = target.height + _ringPadding * 2f;

            float phase = Mathf.Repeat(Time.unscaledTime / _pulseSeconds, 1f);
            float size = 1f + (_pulseScale - 1f) * Mathf.Sin(phase * Mathf.PI);
            _ring.style.scale = new Scale(new Vector2(size, size));
            _ring.EnableInClassList(HiddenClass, false);
        }

        private bool TryGetTarget(out Rect target)
        {
            target = default;

            switch (_focus)
            {
                case TutorialFocus.Bat:
                    return _targets.TryGetBatRect(_batId, out target);
                case TutorialFocus.PingButton:
                    return TryGetBounds(_pingButton, out target);
                case TutorialFocus.HintButton:
                    return TryGetBounds(_hintButton, out target);
                default:
                    return false;
            }
        }

        // worldBound is NaN until the first layout pass, and the HUD may not be on screen yet.
        private static bool TryGetBounds(VisualElement element, out Rect bounds)
        {
            bounds = default;
            if (element == null) return false;

            Rect rect = element.worldBound;
            if (float.IsNaN(rect.x) || float.IsNaN(rect.width) || rect.width <= 0f) return false;

            bounds = rect;
            return true;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null) throw new InvalidOperationException($"The tutorial UXML has no {typeof(T).Name} named \"{name}\".");

            return element;
        }
    }
}
