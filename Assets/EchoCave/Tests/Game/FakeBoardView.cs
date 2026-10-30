using System;
using System.Collections.Generic;
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

        // Reports a miss while still handing back a real bat's cell, which is what a careless presenter would act on.
        public bool ForceMiss;

        // Like the real view, building or clearing cuts a running animation short without calling back.
        public void Build(Board board)
        {
            Built = board;
            BuildCount++;
            PendingComplete = null;
        }

        public void Clear()
        {
            Built = null;
            ClearCount++;
            PendingComplete = null;
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

        public void FinishAnimation()
        {
            Action complete = PendingComplete;
            PendingComplete = null;
            complete?.Invoke();
        }
    }
}
