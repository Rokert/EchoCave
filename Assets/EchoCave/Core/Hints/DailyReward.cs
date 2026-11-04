using System;

namespace EchoCave.Core
{
    // A gift of hints the first time the game opens on a new calendar day.
    //  - The very first day pays nothing: a new player already starts with hints, and so does a player whose save is
    //    older than this reward. The gift begins the next day.
    //  - Any day different from the last paid one pays, even an earlier one. A wrong clock on the phone must not lock
    //    the gift for a year, and toggling the date cannot be abused: the stock and the reserve are capped.
    public sealed class DailyReward : IHintRewardStrategy
    {
        public const string StateKey = "dailyDay";

        private readonly int _hintsPerDay;

        public string Key => StateKey;

        public HintRewardSource Source => HintRewardSource.Daily;

        // The day the gift was last handled; 0 until the game has run once.
        public int LastDay { get; private set; }

        public int State => LastDay;

        public DailyReward(int hintsPerDay, int lastDay)
        {
            if (hintsPerDay < 1) throw new ArgumentOutOfRangeException(nameof(hintsPerDay), hintsPerDay, "A gift is at least one hint.");

            _hintsPerDay = hintsPerDay;
            LastDay = Math.Max(0, lastDay);
        }

        public int Evaluate(RewardMoment moment)
        {
            if (moment.Kind != RewardMomentKind.DayStarted) return 0;
            if (moment.Day == LastDay) return 0;

            bool firstRun = LastDay == 0;
            LastDay = moment.Day;
            return firstRun ? 0 : _hintsPerDay;
        }
    }
}
