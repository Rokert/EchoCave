namespace EchoCave.Core
{
    public enum ChallengeMoveKind
    {
        // A tap on a bat. Spends a move.
        Tap,

        // An echo ping. Spends a ping, not a move.
        Ping
    }

    // One thing the player did in a challenge, in order. Only these two exist: a challenge has no hints and no undo.
    public readonly struct ChallengeMove
    {
        public ChallengeMoveKind Kind { get; }

        // The bat that was tapped. Not used by a ping.
        public int BatId { get; }

        private ChallengeMove(ChallengeMoveKind kind, int batId)
        {
            Kind = kind;
            BatId = batId;
        }

        public static ChallengeMove Tap(int batId) => new ChallengeMove(ChallengeMoveKind.Tap, batId);

        public static ChallengeMove Ping() => new ChallengeMove(ChallengeMoveKind.Ping, -1);
    }
}
