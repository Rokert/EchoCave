using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // Reveals every hidden bat on the board for the same time. The duration is a tunable, so the caller
    // supplies it; there is no default here.
    public sealed class RevealAllHiddenStrategy : IEchoStrategy
    {
        private readonly float _durationSeconds;

        public RevealAllHiddenStrategy(float durationSeconds)
        {
            if (!(durationSeconds > 0f))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "Duration must be positive.");

            _durationSeconds = durationSeconds;
        }

        public EchoPulse CreatePulse(Board board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var ids = new List<int>();
            foreach (Bat bat in board.Bats)
            {
                if (bat.IsHidden) ids.Add(bat.Id);
            }

            return new EchoPulse(ids, _durationSeconds);
        }
    }
}
