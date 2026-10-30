using System;

namespace EchoCave.Core
{
    // The win and lose conditions, in one place. Stateless and deterministic.
    public static class LevelRules
    {
        // Won when the board is empty. Lost when the move limit is used up with bats still on the board.
        // Won is checked first, so clearing the board on the last allowed move wins.
        public static LevelStatus Evaluate(LevelSession session, LevelDefinition level)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (level == null) throw new ArgumentNullException(nameof(level));

            if (session.Board.Count == 0) return LevelStatus.Won;
            if (level.HasMoveLimit && session.MovesUsed >= level.MoveLimit) return LevelStatus.Lost;
            return LevelStatus.InProgress;
        }
    }
}
