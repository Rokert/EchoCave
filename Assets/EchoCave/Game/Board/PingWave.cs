using System;
using DG.Tweening;
using UnityEngine;

namespace EchoCave.Game
{
    // One expanding ring. Pooled by the board view and built in code, so it needs no prefab: the board view
    // creates it, hands it the ring sprite and plays it. Only draws.
    public sealed class PingWave : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Tween _tween;
        private float _halfSize;

        internal void Setup(Sprite sprite, int sortingOrder)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.sortingOrder = sortingOrder;
            _halfSize = SpriteMetrics.Size(sprite).x * 0.5f;
        }

        // Grows the ring's band from nothing to reachWorld at a steady speedWorld (world units per second) while it
        // fades out. bandShare is how far from the sprite's centre the band sits, as a share of its half size.
        internal void Play(float speedWorld, float reachWorld, float bandShare, Action onComplete)
        {
            Stop();

            gameObject.SetActive(true);
            Apply(0f, reachWorld, bandShare);

            _tween = DOTween.To(() => 0f, progress => Apply(progress, reachWorld, bandShare), 1f, reachWorld / speedWorld)
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke());
        }

        internal void Stop()
        {
            _tween?.Kill();
            _tween = null;
        }

        private void Apply(float progress, float reachWorld, float bandShare)
        {
            float radius = progress * reachWorld;
            transform.localScale = Vector3.one * (radius / (_halfSize * bandShare));

            Color color = _renderer.color;
            color.a = 1f - progress;
            _renderer.color = color;
        }
    }
}
