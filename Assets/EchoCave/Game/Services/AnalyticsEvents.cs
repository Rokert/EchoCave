namespace EchoCave.Game
{
    // The names of the events sent to the analytics provider, and of their parameters. Once a provider has data under a
    // name, that name is never reused for something else: a new meaning is a new name.
    //
    // Nothing here identifies a person: no names, no device ids, no free text. Levels are the stable level id, challenges
    // their string id.
    public static class AnalyticsEvents
    {
        public const string LevelStart = "level_start";
        public const string LevelWin = "level_win";
        public const string LevelLose = "level_lose";
        public const string PingUsed = "ping_used";
        public const string HintUsed = "hint_used";
        public const string HintRefused = "hint_refused";
        public const string TutorialEnd = "tutorial_end";
        public const string ChallengeStart = "challenge_start";
        public const string ChallengeSolved = "challenge_solved";
        public const string ChallengeFailed = "challenge_failed";
        public const string ChallengeRejected = "challenge_rejected";

        public static class Param
        {
            // A level id, a challenge id, or "tutorial": where the player was when it happened.
            public const string Where = "where";
            public const string Level = "level";
            public const string Challenge = "challenge";
            public const string Moves = "moves";
            public const string MoveLimit = "move_limit";
            public const string Pings = "pings";
            public const string PingsLeft = "pings_left";
            public const string HintsLeft = "hints_left";
            public const string Stars = "stars";
            public const string Reason = "reason";
            public const string Attempt = "attempt";
            public const string Seconds = "seconds";
            public const string Rank = "rank";
            public const string Best = "best";
        }

        // What the player is playing when a ping or a hint is used.
        public const string TutorialPlace = "tutorial";
    }
}
