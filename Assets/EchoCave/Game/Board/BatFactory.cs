using System;
using System.Collections.Generic;
using EchoCave.Core;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Spawns and recycles bat views. Every view is a child of the container, so destroying the container
    // frees them all; ObjectPool only forgets items it cannot keep, it never destroys them.
    public sealed class BatFactory
    {
        private readonly BatView _prefab;
        private readonly Transform _container;
        private readonly ObjectPool<BatView> _pool;
        private readonly HashSet<BatView> _active = new HashSet<BatView>();

        public int ActiveCount => _active.Count;
        public int FreeCount => _pool.FreeCount;

        public BatFactory(BatView prefab, Transform container, int prewarm = 0)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (container == null) throw new ArgumentNullException(nameof(container));

            _prefab = prefab;
            _container = container;
            _pool = new ObjectPool<BatView>(Create, OnGet, OnRelease);
            _pool.Prewarm(prewarm);
        }

        // Returns an active view already bound to the bat. Positioning is the board view's job.
        public BatView Get(Bat bat)
        {
            if (bat == null) throw new ArgumentNullException(nameof(bat));

            BatView view = _pool.Get();
            view.Bind(bat.Id, bat.Direction, bat.IsHidden);
            _active.Add(view);
            return view;
        }

        // The registry guards against double release, which the pool itself would accept.
        public void Release(BatView view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (!_active.Remove(view))
                throw new InvalidOperationException($"{view.name} is not an active view of this factory.");

            _pool.Release(view);
        }

        public void ReleaseAll()
        {
            var views = new List<BatView>(_active);
            _active.Clear();

            foreach (BatView view in views) _pool.Release(view);
        }

        private BatView Create() => UnityEngine.Object.Instantiate(_prefab, _container);

        private static void OnGet(BatView view) => view.gameObject.SetActive(true);

        private static void OnRelease(BatView view)
        {
            view.ResetForPool();
            view.gameObject.SetActive(false);
        }
    }
}
