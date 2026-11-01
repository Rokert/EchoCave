using System;
using DG.Tweening;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Draws one bat: the bat sprite plus an arrow overlay, or a dark "?" silhouette when it is hidden.
    // Only draws; the presenter decides what to show. The arrow sprite must point up at rotation zero.
    public sealed class BatView : MonoBehaviour
    {
        [SerializeField] private GameObject body;
        [SerializeField] private Transform arrow;
        [SerializeField] private GameObject silhouette;

        private bool _restCaptured;
        private Vector3 _restScale;
        private Quaternion _restRotation;
        private Vector3 _bodyRestScale;
        private Vector3 _arrowRestScale;
        private float _arrowAlpha;
        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _arrowRenderer;
        private SpriteRenderer _silhouetteRenderer;
        private bool _hiddenByDefault;
        private bool _revealed;

        // 0 shows only the silhouette, 1 only the bat; in between both are drawn, cross-fading.
        private float _blend = 1f;
        private float _popScale = 1f;
        private Tween _fade;

        // 1 at rest; a hint pulses it up and down. It multiplies the body and the arrow, which the fade also sizes.
        private float _hintScale = 1f;
        private Tween _hint;

        public int BatId { get; private set; }

        public void Bind(int batId, Direction direction, bool isHidden)
        {
            CaptureRest();
            BatId = batId;
            _hiddenByDefault = isHidden;
            arrow.localRotation = Quaternion.Euler(0f, 0f, AngleFor(direction));
            SetHidden(isHidden);
        }

        // Immediate: shows or hides a bat that is hidden by default, with no fade.
        public void SetRevealed(bool revealed)
        {
            SetHidden(_hiddenByDefault && !revealed);
            _revealed = _hiddenByDefault && revealed;
        }

        public void SetHidden(bool isHidden)
        {
            CaptureRest();
            KillFade();
            _revealed = false;
            _popScale = 1f;
            Apply(isHidden ? 0f : 1f);
        }

        // A bat that is hidden by default fades into the bat after delaySeconds, with a small pop. Does nothing for
        // other bats or one that is already revealed. onStart runs when the fade really begins, after the delay.
        internal void Reveal(float delaySeconds, float fadeSeconds, float popScale, Action onStart = null)
        {
            if (!_hiddenByDefault || _revealed) return;

            CaptureRest();
            _revealed = true;
            _popScale = popScale;
            Fade(1f, delaySeconds, fadeSeconds, onStart);
        }

        // Fades a revealed bat back into its silhouette.
        internal void Conceal(float fadeSeconds)
        {
            if (!_hiddenByDefault || !_revealed) return;

            _revealed = false;
            Fade(0f, 0f, fadeSeconds, null);
        }

        // Pulses the bat, forever, until StopHint or until it is recycled.
        internal void StartHint(float pulseScale, float halfPulseSeconds)
        {
            CaptureRest();
            KillHint();

            _hint = DOTween.To(() => _hintScale, value => { _hintScale = value; Apply(_blend); }, pulseScale, halfPulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        internal void StopHint()
        {
            KillHint();
            _hintScale = 1f;
            Apply(_blend);
        }

        // Stops animations and restores the prefab pose, because fly-out and bounce tweens change both.
        // Position is left alone: the board view sets it every time a bat is spawned.
        internal void ResetForPool()
        {
            CaptureRest();
            transform.DOKill();
            KillFade();
            KillHint();
            _hintScale = 1f;
            transform.localScale = _restScale;
            transform.localRotation = _restRotation;
            Apply(1f);
        }

        private void Fade(float target, float delaySeconds, float fadeSeconds, Action onStart)
        {
            KillFade();

            _fade = DOTween.To(() => _blend, Apply, target, fadeSeconds)
                .SetDelay(delaySeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
            if (onStart != null) _fade.OnStart(() => onStart());
        }

        private void KillHint()
        {
            _hint?.Kill();
            _hint = null;
        }

        private void KillFade()
        {
            _fade?.Kill();
            _fade = null;
        }

        // Draws the bat at a point of the silhouette-to-bat blend. The pop only shows on the way in: it shrinks
        // from _popScale to the resting size as the blend reaches 1.
        private void Apply(float blend)
        {
            _blend = Mathf.Clamp01(blend);

            body.SetActive(_blend > 0f);
            arrow.gameObject.SetActive(_blend > 0f);
            silhouette.SetActive(_blend < 1f);

            SetAlpha(_bodyRenderer, _blend);
            SetAlpha(_arrowRenderer, _blend * _arrowAlpha);
            SetAlpha(_silhouetteRenderer, 1f - _blend);

            body.transform.localScale = _bodyRestScale * (Mathf.Lerp(_popScale, 1f, _blend) * _hintScale);
            arrow.localScale = _arrowRestScale * _hintScale;
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        // Lazy because the first call can come before Awake when the container is inactive.
        private void CaptureRest()
        {
            if (_restCaptured) return;

            _restScale = transform.localScale;
            _restRotation = transform.localRotation;
            _bodyRestScale = body.transform.localScale;
            _arrowRestScale = arrow.localScale;
            _bodyRenderer = body.GetComponent<SpriteRenderer>();
            _arrowRenderer = arrow.GetComponent<SpriteRenderer>();
            _silhouetteRenderer = silhouette.GetComponent<SpriteRenderer>();
            _arrowAlpha = _arrowRenderer.color.a;
            _restCaptured = true;
        }

        // Counter-clockwise degrees around Z for an arrow that points up at zero.
        internal static float AngleFor(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return 0f;
                case Direction.Left: return 90f;
                case Direction.Down: return 180f;
                case Direction.Right: return 270f;
                default: throw new System.ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
