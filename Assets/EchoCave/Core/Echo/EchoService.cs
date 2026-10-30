using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // Ping budget and the temporary reveal of hidden bats. The board is never touched: a revealed bat is
    // only listed here, so undo and the solver are unaffected, and the reveal can simply expire.
    // Time comes in through Tick, so the core stays free of any clock.
    public sealed class EchoService
    {
        private readonly IEchoStrategy _strategy;
        private readonly HashSet<int> _revealed = new HashSet<int>();

        public int PingsLeft { get; private set; }
        public bool IsActive { get; private set; }
        public float RemainingSeconds { get; private set; }

        public IReadOnlyCollection<int> RevealedBatIds => _revealed;

        // Raised after a ping starts a reveal.
        public event Action<EchoPulse> Pinged;

        // Raised once, when the reveal runs out.
        public event Action Expired;

        public EchoService(IEchoStrategy strategy, int pingBudget)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            if (pingBudget < 0)
                throw new ArgumentOutOfRangeException(nameof(pingBudget), pingBudget, "Ping budget cannot be negative.");

            PingsLeft = pingBudget;
        }

        // A ping that cannot do anything spends nothing and says why.
        public PingResult TryPing(Board board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            if (PingsLeft <= 0) return PingResult.NoPingsLeft;
            if (IsActive) return PingResult.AlreadyActive;

            EchoPulse pulse = _strategy.CreatePulse(board);
            if (pulse.BatIds == null) throw new InvalidOperationException("The echo strategy returned a pulse without bat ids.");
            if (pulse.BatIds.Count == 0) return PingResult.NothingToReveal;
            if (!(pulse.DurationSeconds > 0f))
                throw new InvalidOperationException($"The echo strategy returned a non-positive duration ({pulse.DurationSeconds}).");

            PingsLeft--;
            _revealed.Clear();
            foreach (int id in pulse.BatIds) _revealed.Add(id);
            RemainingSeconds = pulse.DurationSeconds;
            IsActive = true;

            Pinged?.Invoke(pulse);
            return PingResult.Pinged;
        }

        // Advances the reveal timer. A bat that left the board stays listed until the reveal ends, which
        // is harmless and lets an undone bat come back still revealed.
        public void Tick(float deltaSeconds)
        {
            if (!(deltaSeconds >= 0f))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time cannot go backwards.");
            if (!IsActive) return;

            RemainingSeconds -= deltaSeconds;
            if (RemainingSeconds > 0f) return;

            RemainingSeconds = 0f;
            IsActive = false;
            _revealed.Clear();
            Expired?.Invoke();
        }

        public bool IsRevealed(int batId) => _revealed.Contains(batId);

        // Extra pings, for example from a rewarded ad.
        public void GrantPings(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Grant at least one ping.");

            PingsLeft += count;
        }
    }
}
