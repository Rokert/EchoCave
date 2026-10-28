namespace EchoCave.Core
{
    public enum MoveOutcome
    {
        Exited,
        Bounced
    }

    // Describes what a tap would do. It never changes the board by itself.
    public readonly struct MoveResult
    {
        public MoveOutcome Outcome { get; }

        // The bat that was tapped.
        public Bat Bat { get; }

        // Furthest cell the bat reached before leaving the board (Exited) or before the blocker (Bounced).
        // For a bounced bat, it equals the bat's own cell when the blocker is adjacent.
        public Cell StopCell { get; }

        // The bat in the way. Null when the outcome is Exited.
        public Bat BlockingBat { get; }

        private MoveResult(MoveOutcome outcome, Bat bat, Cell stopCell, Bat blockingBat)
        {
            Outcome = outcome;
            Bat = bat;
            StopCell = stopCell;
            BlockingBat = blockingBat;
        }

        public static MoveResult Exit(Bat bat, Cell lastCellInside)
        {
            return new MoveResult(MoveOutcome.Exited, bat, lastCellInside, null);
        }

        public static MoveResult Bounce(Bat bat, Cell stopCell, Bat blockingBat)
        {
            return new MoveResult(MoveOutcome.Bounced, bat, stopCell, blockingBat);
        }
    }
}
