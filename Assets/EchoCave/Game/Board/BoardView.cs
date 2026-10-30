using System;
using System.Collections.Generic;
using DG.Tweening;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Draws the board and plays move animations. It decides nothing: the presenter calls Play with the
    // result the core already produced. Positions are local to this transform, so the board can be moved
    // or scaled in the scene.
    public sealed class BoardView : MonoBehaviour, IBoardView
    {
        [SerializeField] private BatView batPrefab;
        [SerializeField] private Transform batContainer;
        [SerializeField] private BoardViewSettings settings;

        private readonly Dictionary<int, BatView> _views = new Dictionary<int, BatView>();
        private BatFactory _factory;

        public GridLayout2D Layout { get; private set; }

        // Replaces whatever is on screen. Animations still running are cut short and their callbacks never fire.
        public void Build(Board board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            Clear();
            Layout = new GridLayout2D(board.Width, board.Height, settings.CellSize);
            _factory ??= new BatFactory(batPrefab, batContainer, board.Count);

            foreach (Bat bat in board.Bats) Spawn(bat);
        }

        public bool TryGetCell(Vector2 world, out Cell cell)
        {
            if (Layout == null)
            {
                cell = default;
                return false;
            }

            return Layout.TryWorldToCell(transform.InverseTransformPoint(world), out cell);
        }

        public void Spawn(Bat bat)
        {
            if (_views.ContainsKey(bat.Id)) throw new InvalidOperationException($"Bat {bat.Id} is already on the board view.");

            BatView view = _factory.Get(bat);
            view.transform.localPosition = Layout.CellToWorld(bat.Cell);
            _views.Add(bat.Id, view);
        }

        public void Clear()
        {
            _factory?.ReleaseAll();
            _views.Clear();
        }

        // Plays one tap. onComplete runs when the animation ends, after an exited bat has been recycled.
        public void Play(MoveResult result, Action onComplete = null)
        {
            int batId = result.Bat.Id;
            if (!_views.TryGetValue(batId, out BatView view))
                throw new InvalidOperationException($"Bat {batId} has no view to animate.");

            MovePlan plan = MovePathPlanner.Plan(result, Layout, settings.ExitOvershootCells, settings.BumpCells);
            Transform bat = view.transform;
            Sequence sequence = DOTween.Sequence().SetLink(view.gameObject);

            if (plan.Outcome == MoveOutcome.Exited)
            {
                sequence.Append(bat.DOLocalMove(plan.Target, plan.TravelCells / settings.FlySpeed).SetEase(settings.ExitEase));
                sequence.OnComplete(() =>
                {
                    _views.Remove(batId);
                    _factory.Release(view);
                    onComplete?.Invoke();
                });
                return;
            }

            if (plan.TravelCells > 0f)
                sequence.Append(bat.DOLocalMove(plan.Target, plan.TravelCells / settings.FlySpeed).SetEase(Ease.Linear));

            sequence.Append(bat.DOLocalMove(plan.Bump, settings.BumpDuration).SetEase(Ease.OutQuad));
            sequence.AppendCallback(() => PunchBlocker(result));
            sequence.Append(bat.DOLocalMove(plan.Start, settings.ReturnDuration).SetEase(settings.ReturnEase));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void PunchBlocker(MoveResult result)
        {
            if (result.BlockingBat == null || !_views.TryGetValue(result.BlockingBat.Id, out BatView blocker)) return;

            Cell offset = result.Bat.Direction.ToOffset();
            Vector3 punch = new Vector3(offset.X, offset.Y, 0f) * (settings.BlockerPunchCells * Layout.CellSize);
            blocker.transform.DOPunchPosition(punch, settings.BlockerPunchDuration, 6).SetLink(blocker.gameObject);
        }
    }
}
