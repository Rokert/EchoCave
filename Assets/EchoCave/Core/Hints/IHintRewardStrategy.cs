namespace EchoCave.Core
{
    // Where a free hint came from, so the HUD and the sound can tell the player.
    public enum HintRewardSource
    {
        Daily,
        Streak
    }

    public enum RewardMomentKind
    {
        LevelWon,
        LevelLost,
        DayStarted
    }

    // Something that happened in the game that a reward strategy may care about. Built by the game edge from its
    // events and its clock, so the strategies stay pure: no Unity, no DateTime.
    public readonly struct RewardMoment
    {
        public RewardMomentKind Kind { get; }

        // LevelWon only: the stars of this win, 1 to 3.
        public int Stars { get; }

        // LevelWon only: the level already had 3 stars before this win, so winning it again is a replay.
        public bool LevelAlreadyThreeStarred { get; }

        // DayStarted only: a number that grows by one per calendar day, from 1.
        public int Day { get; }

        // The player cannot hold one more hint (stock and reserve are full). A streak waits while this is true.
        public bool HintsFull { get; }

        private RewardMoment(RewardMomentKind kind, int stars, bool alreadyThreeStarred, int day, bool hintsFull)
        {
            Kind = kind;
            Stars = stars;
            LevelAlreadyThreeStarred = alreadyThreeStarred;
            Day = day;
            HintsFull = hintsFull;
        }

        public static RewardMoment Won(int stars, bool levelAlreadyThreeStarred, bool hintsFull = false)
        {
            if (stars < 1 || stars > PlayerProgress.MaxStars)
                throw new System.ArgumentOutOfRangeException(nameof(stars), stars, "A win earns 1 to 3 stars.");

            return new RewardMoment(RewardMomentKind.LevelWon, stars, levelAlreadyThreeStarred, 0, hintsFull);
        }

        public static RewardMoment Lost(bool hintsFull = false) => new RewardMoment(RewardMomentKind.LevelLost, 0, false, 0, hintsFull);

        public static RewardMoment DayStarted(int day)
        {
            if (day < 1) throw new System.ArgumentOutOfRangeException(nameof(day), day, "Days count from 1.");

            return new RewardMoment(RewardMomentKind.DayStarted, 0, false, day, false);
        }
    }

    // One way of earning free hints. It keeps one small integer of state (a streak, the last day it paid out), which
    // the game edge saves under Key, and answers each moment with how many hints it earns.
    public interface IHintRewardStrategy
    {
        // The name the state is saved under. Never changes once shipped.
        string Key { get; }

        HintRewardSource Source { get; }

        // What to save so the strategy can pick up where it left off.
        int State { get; }

        // The hints this moment earns: 0 for most moments.
        int Evaluate(RewardMoment moment);
    }
}
