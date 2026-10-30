using System;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Where a bat has to travel for one move. Positions are in board space, the same space as GridLayout2D.
    public readonly struct MovePlan
    {
        public MoveOutcome Outcome { get; }

        // Where the bat starts, which is also where a bounced bat ends up.
        public Vector2 Start { get; }

        // Exited: a point past the board edge. Bounced: where the bat stops in front of the blocker.
        public Vector2 Target { get; }

        // Bounced only: how far the bat pushes into the blocker.
        public Vector2 Bump { get; }

        // Distance from Start to Target, in cells.
        public float TravelCells { get; }

        public MovePlan(MoveOutcome outcome, Vector2 start, Vector2 target, Vector2 bump, float travelCells)
        {
            Outcome = outcome;
            Start = start;
            Target = target;
            Bump = bump;
            TravelCells = travelCells;
        }
    }

    // Turns the core's MoveResult into positions so the view only has to tween between them.
    public static class MovePathPlanner
    {
        public static MovePlan Plan(MoveResult result, GridLayout2D layout, float exitOvershootCells, float bumpCells)
        {
            if (result.Bat == null) throw new ArgumentException("The move result has no bat.", nameof(result));
            if (layout == null) throw new ArgumentNullException(nameof(layout));

            Cell offset = result.Bat.Direction.ToOffset();
            var direction = new Vector2(offset.X, offset.Y);

            Vector2 start = layout.CellToWorld(result.Bat.Cell);
            Vector2 stop = layout.CellToWorld(result.StopCell);

            if (result.Outcome == MoveOutcome.Exited)
            {
                // StopCell is the last cell inside the board; one more cell puts the bat on the edge, then the overshoot.
                Vector2 target = stop + direction * (layout.CellSize * (1f + exitOvershootCells));
                return new MovePlan(MoveOutcome.Exited, start, target, target, Cells(start, target, layout));
            }

            Vector2 bump = stop + direction * (layout.CellSize * bumpCells);
            return new MovePlan(MoveOutcome.Bounced, start, stop, bump, Cells(start, stop, layout));
        }

        private static float Cells(Vector2 from, Vector2 to, GridLayout2D layout)
        {
            return Vector2.Distance(from, to) / layout.CellSize;
        }
    }
}
