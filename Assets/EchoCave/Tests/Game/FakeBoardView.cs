using System;
using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using EchoCave.Game;
using UnityEngine;

namespace EchoCave.Tests
{
    // Stands in for BoardView. World == cell: the point (x + 0.5, y + 0.5) is inside cell (x, y), and the
    // board is whatever was last built. Play only records the call; FinishAnimation ends it by hand.
    internal sealed class FakeBoardView : IBoardView
    {
        public readonly List<MoveResult> Played = new List<MoveResult>();
        public Action PendingComplete;
        public Board Built;
        public int BuildCount;
        public int ClearCount;

        // The ids last passed to SetRevealed. Empty after a build, like fresh bat views.
        public int[] Revealed = new int[0];
        public int RejectedCount;

        // How many pings were drawn, and the ids the last one revealed.
        public int PingShownCount;

        // Reports a miss while still handing back a real bat's cell, which is what a careless presenter would act on.
        public bool ForceMiss;

        // Like the real view, building or clearing cuts a running animation short without calling back,
        // and every bat starts unrevealed.
        public void Build(Board board)
        {
            Built = board;
            BuildCount++;
            PendingComplete = null;
            Revealed = new int[0];
        }

        public void Clear()
        {
            Built = null;
            ClearCount++;
            PendingComplete = null;
            Revealed = new int[0];
        }

        public bool TryGetCell(Vector2 world, out Cell cell)
        {
            cell = new Cell(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
            if (ForceMiss || Built == null) return false;

            return Built.IsInside(cell);
        }

        public void Play(MoveResult result, Action onComplete)
        {
            Played.Add(result);
            PendingComplete = onComplete;
        }

        public void SetRevealed(IReadOnlyCollection<int> batIds)
        {
            Revealed = batIds.ToArray();
        }

        public void ShowPing(IReadOnlyCollection<int> batIds)
        {
            PingShownCount++;
            Revealed = batIds.ToArray();
        }

        public void ShowRejected()
        {
            RejectedCount++;
        }

        public void FinishAnimation()
        {
            Action complete = PendingComplete;
            PendingComplete = null;
            complete?.Invoke();
        }
    }
}
