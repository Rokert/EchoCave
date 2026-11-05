using System;

namespace EchoCave.Core
{
    public enum ReplayError
    {
        None,

        // The id is empty, the seconds are not a number or negative, or the attempt count is below 1.
        BadStats,

        // The list is longer than any real attempt can be.
        TooManyMoves,

        // A tap on a bat that is not on the board (never there, or already gone).
        UnknownBat,

        // More pings than the level allows.
        TooManyPings,

        // A ping with no hidden bat on the board: the game spends nothing, so it never records one.
        PingWithNothingToReveal,

        // Something after the board was cleared.
        MovesAfterWin,

        // Something after the move limit was reached.
        MovesAfterLoss,

        // The list ends with bats still on the board.
        NotSolved,

        // The time reported is shorter than a person can tap that many bats.
        ImpossibleTime
    }

    public readonly struct ReplayResult
    {
        public bool IsValid => Error == ReplayError.None;
        public ReplayError Error { get; }

        // Why it was rejected, for a log. Empty when valid.
        public string Message { get; }

        // Taps spent, bounces included: the number that ranks. 0 when rejected.
        public int Moves { get; }

        public int Pings { get; }

        public ReplayResult(ReplayError error, string message, int moves, int pings)
        {
            Error = error;
            Message = message ?? string.Empty;
            Moves = moves;
            Pings = pings;
        }

        public static ReplayResult Rejected(ReplayError error, string message) => new ReplayResult(error, message, 0, 0);
    }

    // Plays an attempt back through the real rules and says whether it really cleared the level, and in how many taps.
    // Pure and deterministic: no clock, no randomness. The server runs the same code on what it receives.
    //
    // What it can prove: every tap was on a bat that was there, the pings fit the budget, the move limit was respected
    // and the board ends empty. What it cannot prove: how long it took, or that a person played (a program that reads
    // the level can send a perfect list). The time is only checked for what is humanly possible.
    public static class ChallengeReplay
    {
        // No real attempt comes near this; it only stops a hostile list from costing time.
        public const int MaxMoves = 10000;

        // minSecondsPerTap: the shortest time a person needs per tap. 0 turns the time check off.
        public static ReplayResult Verify(LevelDefinition level, ChallengeAttempt attempt, MoveResolver resolver, float minSecondsPerTap)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (attempt == null) throw new ArgumentNullException(nameof(attempt));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            if (attempt.ChallengeId.Length == 0 || !IsFiniteNonNegative(attempt.ActiveSeconds) || attempt.Attempts < 1)
                return ReplayResult.Rejected(ReplayError.BadStats, "The id is empty, the time is not a valid number, or the attempts are below 1.");

            if (attempt.Moves.Count > MaxMoves)
                return ReplayResult.Rejected(ReplayError.TooManyMoves, $"{attempt.Moves.Count} moves is more than {MaxMoves}.");

            // Throws InvalidLevelException for a level that is not valid: that is a bug in the level, not a bad attempt.
            LevelSession session = LevelBuilder.Build(level);
            Board board = session.Board;
            int pings = 0;
            bool lost = false;

            for (int i = 0; i < attempt.Moves.Count; i++)
            {
                if (board.Count == 0)
                    return ReplayResult.Rejected(ReplayError.MovesAfterWin, $"Move {i + 1} came after the board was cleared.");
                if (lost)
                    return ReplayResult.Rejected(ReplayError.MovesAfterLoss, $"Move {i + 1} came after the move limit was reached.");

                ChallengeMove move = attempt.Moves[i];
                if (move.Kind == ChallengeMoveKind.Ping)
                {
                    if (pings >= level.PingBudget)
                        return ReplayResult.Rejected(ReplayError.TooManyPings, $"Ping at move {i + 1} is beyond the budget of {level.PingBudget}.");
                    if (!HasHiddenBat(board))
                        return ReplayResult.Rejected(ReplayError.PingWithNothingToReveal, $"Ping at move {i + 1} had no hidden bat to reveal.");

                    pings++;
                    continue;
                }

                if (!board.TryGetBat(move.BatId, out _))
                    return ReplayResult.Rejected(ReplayError.UnknownBat, $"Move {i + 1} taps bat {move.BatId}, which is not on the board.");

                var command = new MoveCommand(session, resolver, move.BatId);
                command.Execute();

                if (board.Count > 0 && level.HasMoveLimit && session.MovesUsed >= level.MoveLimit) lost = true;
            }

            if (board.Count > 0)
                return ReplayResult.Rejected(ReplayError.NotSolved, $"{board.Count} bat(s) are still on the board.");

            if (minSecondsPerTap > 0f && attempt.ActiveSeconds < session.MovesUsed * minSecondsPerTap)
                return ReplayResult.Rejected(ReplayError.ImpossibleTime,
                    $"{session.MovesUsed} taps in {attempt.ActiveSeconds} s is faster than {minSecondsPerTap} s per tap.");

            return new ReplayResult(ReplayError.None, string.Empty, session.MovesUsed, pings);
        }

        private static bool IsFiniteNonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;

        private static bool HasHiddenBat(Board board)
        {
            foreach (Bat bat in board.Bats)
            {
                if (bat.IsHidden) return true;
            }

            return false;
        }
    }
}
