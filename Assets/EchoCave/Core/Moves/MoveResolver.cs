using System;

namespace EchoCave.Core
{
    // Stateless and deterministic. Hidden bats block exactly like visible ones.
    public sealed class MoveResolver
    {
        public MoveResult Resolve(Board board, int batId)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!board.TryGetBat(batId, out Bat bat))
                throw new ArgumentException($"No bat with id {batId} on the board.", nameof(batId));

            Cell last = bat.Cell;
            Cell next = last.Step(bat.Direction);

            while (board.IsInside(next))
            {
                if (board.TryGetBat(next, out Bat blocker))
                    return MoveResult.Bounce(bat, last, blocker);

                last = next;
                next = last.Step(bat.Direction);
            }

            return MoveResult.Exit(bat, last);
        }
    }
}
