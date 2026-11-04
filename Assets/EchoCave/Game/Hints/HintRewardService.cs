using System;
using System.Collections.Generic;
using EchoCave.Core;
using Rokert.Core;

namespace EchoCave.Game
{
    // The adapter between the game and the reward strategies: it turns what happens (a win, a lost level, a new day)
    // into reward moments, hands each moment to every strategy, pays what they earn into the hint stock, saves
    // their state, and tells the HUD and the sound when a hint really arrived. Plain C#; GameRoot builds it.
    //
    // While the stock and the reserve are both full the streak is on standby (it ignores wins and losses and sits at
    // zero), and it starts counting again once a hint is spent.
    //
    // A win comes from ProgressRecorder, because only it sees the stars the level had before the win was saved.
    // A lost level comes from the EventBus. Leaving to the menu is not listened to at all.
    public sealed class HintRewardService : IDisposable
    {
        private readonly IReadOnlyList<IHintRewardStrategy> _strategies;
        private readonly IHintStock _stock;
        private readonly ISaveService _save;
        private readonly EventBus _events;
        private readonly IDayClock _clock;

        public HintRewardService(
            IReadOnlyList<IHintRewardStrategy> strategies, IHintStock stock, ISaveService save, EventBus events, IDayClock clock)
        {
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            _events.Subscribe<LevelLost>(OnLevelLost);
        }

        public void Dispose()
        {
            _events.Unsubscribe<LevelLost>(OnLevelLost);
        }

        // previousStars is what the level had before this win: 0 when it was never won.
        public void OnLevelWon(int stars, int previousStars)
        {
            Deliver(RewardMoment.Won(stars, previousStars >= PlayerProgress.MaxStars, _stock.IsFull));
        }

        // Call when the game starts and whenever it comes back to the foreground: a new day may have begun.
        public void CheckDay()
        {
            Deliver(RewardMoment.DayStarted(_clock.Today));
        }

        private void OnLevelLost(LevelLost lost)
        {
            Deliver(RewardMoment.Lost(_stock.IsFull));
        }

        private void Deliver(RewardMoment moment)
        {
            foreach (IHintRewardStrategy strategy in _strategies)
            {
                int earned = strategy.Evaluate(moment);
                _save.SetRewardState(strategy.Key, strategy.State);
                if (earned <= 0) continue;

                HintGrant grant = _stock.GrantFree(earned);
                if (grant.Granted > 0) _events.Publish(new HintRewarded(strategy.Source, grant.ToStock, grant.ToReserve));
            }
        }
    }
}
