using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // The bat a hint points at, and why it was chosen.
    public readonly struct HintPick
    {
        public int BatId { get; }
        public bool IsHidden { get; }

        // How many other bats this one's departure frees on the spot.
        public int FreesOthers { get; }

        public HintPick(int batId, bool isHidden, int freesOthers)
        {
            BatId = batId;
            IsHidden = isHidden;
            FreesOthers = freesOthers;
        }
    }

    // Chooses the move a hint recommends. Deterministic: no randomness, no clock, and the board is never touched.
    //
    // Any bat that can leave now is a good move, because removing a bat can only free paths and never block one
    // (D-010), so the level stays solvable. Among them the hint prefers the one that frees the most other bats,
    // since it opens the board up the most. A tie goes to a visible bat (the player can read its arrow without
    // spending anything more), and after that to the lowest id.
    public sealed class HintPicker
    {
        private readonly MoveResolver _resolver;

        public HintPicker(MoveResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        // Null when no bat can leave (an empty board, or a deadlock).
        public HintPick? Pick(Board board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var free = new List<Bat>();
            var freeIds = new HashSet<int>();
            foreach (Bat bat in board.Bats)
            {
                if (_resolver.Resolve(board, bat.Id).Outcome != MoveOutcome.Exited) continue;
                free.Add(bat);
                freeIds.Add(bat.Id);
            }

            HintPick? best = null;
            foreach (Bat candidate in free)
            {
                var pick = new HintPick(candidate.Id, candidate.IsHidden, CountFreedBy(board, candidate, freeIds));
                if (best == null || IsBetter(pick, best.Value)) best = pick;
            }

            return best;
        }

        // Bats that are blocked now and could leave once the candidate is gone, measured on a copy of the board.
        private int CountFreedBy(Board board, Bat candidate, HashSet<int> alreadyFree)
        {
            var without = new Board(board.Width, board.Height);
            foreach (Bat bat in board.Bats)
                if (bat.Id != candidate.Id) without.Add(bat);

            int freed = 0;
            foreach (Bat bat in without.Bats)
            {
                if (alreadyFree.Contains(bat.Id)) continue;
                if (_resolver.Resolve(without, bat.Id).Outcome == MoveOutcome.Exited) freed++;
            }

            return freed;
        }

        private static bool IsBetter(HintPick a, HintPick b)
        {
            if (a.FreesOthers != b.FreesOthers) return a.FreesOthers > b.FreesOthers;
            if (a.IsHidden != b.IsHidden) return !a.IsHidden;
            return a.BatId < b.BatId;
        }
    }
}
