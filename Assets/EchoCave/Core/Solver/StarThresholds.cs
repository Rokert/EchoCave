using System;

namespace EchoCave.Core
{
    public readonly struct StarThresholds
    {
        public int ThreeStarMoves { get; }
        public int TwoStarMoves { get; }

        public StarThresholds(int threeStarMoves, int twoStarMoves)
        {
            ThreeStarMoves = threeStarMoves;
            TwoStarMoves = twoStarMoves;
        }

        // Three stars for the minimum, two stars with up to twoStarSlack extra taps (never beyond the move
        // limit, when there is one).
        public static StarThresholds Suggest(int minMoves, int moveLimit, int twoStarSlack)
        {
            int three = Math.Max(1, minMoves);
            int two = three + Math.Max(0, twoStarSlack);

            if (moveLimit > 0 && two > moveLimit)
                two = Math.Max(three, moveLimit);

            return new StarThresholds(three, two);
        }
    }
}
