using System;
using EchoCave.Core;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Connects taps to the core: a tap on a bat becomes a MoveCommand, and the view animates its result.
    // Taps are ignored while an animation plays, so the view never has to animate over a half-finished move.
    public sealed class BoardPresenter
    {
        private readonly LevelSession _session;
        private readonly MoveResolver _resolver;
        private readonly CommandHistory _history;
        private readonly IBoardView _view;

        private bool _busy;

        // Raised once per executed move, right after the core resolved it and before the animation starts,
        // so counters can update at the moment of the tap.
        public event Action<MoveResult> MoveExecuted;

        // Raised when a tap lands on a bat and is accepted, before the move is resolved.
        public event Action<int> BatTapped;

        // Raised when a tap lands on a bat that CanTap refused.
        public event Action TapRefused;

        // Decides whether a tap on this bat id may move it. Null lets every tap through. The tutorial uses it to limit
        // the first steps to one bat.
        public Func<int, bool> CanTap { get; set; }

        public BoardPresenter(LevelSession session, MoveResolver resolver, CommandHistory history, IBoardView view)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool IsBusy => _busy;

        // Draws the board. Building cuts running animations short without calling back, so busy is cleared here.
        public void Start()
        {
            _busy = false;
            _view.Build(_session.Board);
        }

        public void HandleTap(Vector2 world)
        {
            if (_busy) return;
            if (!_view.TryGetCell(world, out Cell cell)) return;
            if (!_session.Board.TryGetBat(cell, out Bat bat)) return;

            if (CanTap != null && !CanTap(bat.Id))
            {
                TapRefused?.Invoke();
                return;
            }

            BatTapped?.Invoke(bat.Id);

            var command = new MoveCommand(_session, _resolver, bat.Id);
            _history.Execute(command);
            MoveExecuted?.Invoke(command.Result);

            // Set before Play: a view may call back synchronously.
            _busy = true;
            _view.Play(command.Result, () => _busy = false);
        }
    }
}
