using System;

namespace EchoCave.Core
{
    // A hint for every N wins in a row with 3 stars on levels that never had 3 stars before.
    //  - A win with fewer than 3 stars, or a lost level, breaks the streak.
    //  - Replaying a level that already has 3 stars changes nothing: it cannot be farmed, and it does not hurt.
    //  - Leaving to the menu is not a moment at all, so it never breaks the streak.
    //  - While the player cannot hold another hint (stock and reserve full) the streak is on standby: wins and losses
    //    are ignored and the count is zero, so a reward is never earned just to be thrown away. The cycle starts again
    //    from zero with the first win after a hint is spent.
    public sealed class StreakReward : IHintRewardStrategy
    {
        public const string StateKey = "streak";

        private readonly int _streakLength;
        private readonly int _hintsPerStreak;

        public string Key => StateKey;

        public HintRewardSource Source => HintRewardSource.Streak;

        // The wins counted so far, always less than the streak length.
        public int Streak { get; private set; }

        public int State => Streak;

        // savedStreak is what the save held. If the streak length was lowered since, it is cut to fit.
        public StreakReward(int streakLength, int hintsPerStreak, int savedStreak)
        {
            if (streakLength < 1) throw new ArgumentOutOfRangeException(nameof(streakLength), streakLength, "A streak is at least one win.");
            if (hintsPerStreak < 1) throw new ArgumentOutOfRangeException(nameof(hintsPerStreak), hintsPerStreak, "A streak must earn at least one hint.");

            _streakLength = streakLength;
            _hintsPerStreak = hintsPerStreak;
            Streak = Math.Max(0, Math.Min(savedStreak, streakLength - 1));
        }

        public int Evaluate(RewardMoment moment)
        {
            if (moment.Kind != RewardMomentKind.DayStarted && moment.HintsFull)
            {
                Streak = 0;
                return 0;
            }

            switch (moment.Kind)
            {
                case RewardMomentKind.LevelLost:
                    Streak = 0;
                    return 0;

                case RewardMomentKind.LevelWon:
                    return OnWin(moment);

                default:
                    return 0;
            }
        }

        private int OnWin(RewardMoment moment)
        {
            if (moment.Stars < PlayerProgress.MaxStars)
            {
                Streak = 0;
                return 0;
            }

            if (moment.LevelAlreadyThreeStarred) return 0;

            Streak++;
            if (Streak < _streakLength) return 0;

            Streak = 0;
            return _hintsPerStreak;
        }
    }
}
