using EchoCave.Core;

namespace EchoCave.Game
{
    // Published on the EventBus by GameFlow, so the HUD and popups never have to poll it.

    public readonly struct LevelStarted
    {
        public int LevelIndex { get; }

        // 0 means no move limit.
        public int MoveLimit { get; }

        public int PingBudget { get; }

        public LevelStarted(int levelIndex, int moveLimit, int pingBudget)
        {
            LevelIndex = levelIndex;
            MoveLimit = moveLimit;
            PingBudget = pingBudget;
        }
    }

    // A tap was resolved and spent a move. MoveLimit is 0 when the level has none.
    public readonly struct MoveSpent
    {
        public int MovesUsed { get; }
        public int MoveLimit { get; }
        public MoveOutcome Outcome { get; }

        public MoveSpent(int movesUsed, int moveLimit, MoveOutcome outcome)
        {
            MovesUsed = movesUsed;
            MoveLimit = moveLimit;
            Outcome = outcome;
        }
    }

    // The game was paused or resumed.
    public readonly struct PauseChanged
    {
        public bool IsPaused { get; }

        public PauseChanged(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }

    public enum AudioChannel
    {
        Sound,
        Music
    }

    // The player flipped the sound or the music switch. Nothing plays yet; the audio service of ROK-145
    // will listen to this and keep the setting.
    public readonly struct AudioToggled
    {
        public AudioChannel Channel { get; }
        public bool Enabled { get; }

        public AudioToggled(AudioChannel channel, bool enabled)
        {
            Channel = channel;
            Enabled = enabled;
        }
    }

    // The flow went back to the menu, so there is no level on screen.
    public readonly struct MenuOpened { }

    public readonly struct LevelWon
    {
        public int LevelIndex { get; }
        public int MovesUsed { get; }
        public int Stars { get; }

        public LevelWon(int levelIndex, int movesUsed, int stars)
        {
            LevelIndex = levelIndex;
            MovesUsed = movesUsed;
            Stars = stars;
        }
    }

    public readonly struct LevelLost
    {
        public int LevelIndex { get; }
        public int MovesUsed { get; }

        public LevelLost(int levelIndex, int movesUsed)
        {
            LevelIndex = levelIndex;
            MovesUsed = movesUsed;
        }
    }

    // A ping started a reveal.
    public readonly struct PingUsed
    {
        public int PingsLeft { get; }
        public float DurationSeconds { get; }

        public PingUsed(int pingsLeft, float durationSeconds)
        {
            PingsLeft = pingsLeft;
            DurationSeconds = durationSeconds;
        }
    }

    // The reveal ran out and the hidden bats went dark again.
    public readonly struct EchoExpired { }

    public enum HintRefusal
    {
        NoHintsLeft,

        // A move is still animating.
        Busy,

        // No bat can leave, which a valid level never produces.
        NothingToHint,

        // The bat a hint pointed at is still on the board and still pulsing: that hint is still good, so a second
        // one would only spend a hint to show the same thing.
        AlreadyHinted,

        // A tutorial lesson that does not teach hints yet.
        NotInLesson,

        // A challenge has no hints.
        NotInChallenge
    }

    // A hint was spent and the chosen bat is pulsing.
    public readonly struct HintUsed
    {
        public int HintsLeft { get; }
        public int BatId { get; }

        public HintUsed(int hintsLeft, int batId)
        {
            HintsLeft = hintsLeft;
            BatId = batId;
        }
    }

    // A hint was asked for and refused; it costs nothing.
    public readonly struct HintRefused
    {
        public HintRefusal Reason { get; }

        public HintRefused(HintRefusal reason)
        {
            Reason = reason;
        }
    }

    // The number of hints the player holds changed.
    public readonly struct HintsChanged
    {
        public int Count { get; }

        public HintsChanged(int count)
        {
            Count = count;
        }
    }

    // The number of spare hints waiting in the reserve changed.
    public readonly struct HintReserveChanged
    {
        public int Count { get; }

        public HintReserveChanged(int count)
        {
            Count = count;
        }
    }

    // A free hint arrived (a daily gift or a streak). Only published when at least one hint was kept; ToReserve says
    // how many of them went to the reserve because the stock was full.
    public readonly struct HintRewarded
    {
        public HintRewardSource Source { get; }
        public int ToStock { get; }
        public int ToReserve { get; }

        public HintRewarded(HintRewardSource source, int toStock, int toReserve)
        {
            Source = source;
            ToStock = toStock;
            ToReserve = toReserve;
        }
    }

    // A tutorial lesson began (the first, the next, or the same one again after a retry). Lessons use their own events,
    // so progress, stars, rewards and the level popups never count them.
    public readonly struct LessonStarted
    {
        public int LessonIndex { get; }
        public int LessonCount { get; }

        // 0 means no move limit.
        public int MoveLimit { get; }

        public int PingBudget { get; }

        public LessonStarted(int lessonIndex, int lessonCount, int moveLimit, int pingBudget)
        {
            LessonIndex = lessonIndex;
            LessonCount = lessonCount;
            MoveLimit = moveLimit;
            PingBudget = pingBudget;
        }
    }

    public readonly struct LessonWon
    {
        public int LessonIndex { get; }
        public int MovesUsed { get; }

        public LessonWon(int lessonIndex, int movesUsed)
        {
            LessonIndex = lessonIndex;
            MovesUsed = movesUsed;
        }
    }

    public readonly struct LessonLost
    {
        public int LessonIndex { get; }
        public int MovesUsed { get; }

        public LessonLost(int lessonIndex, int movesUsed)
        {
            LessonIndex = lessonIndex;
            MovesUsed = movesUsed;
        }
    }

    // The tutorial moved: a new step, a new lesson, a won lesson waiting for Continue, or the end. Step is null unless a
    // step is showing.
    public readonly struct TutorialChanged
    {
        public TutorialPhase Phase { get; }
        public int LessonIndex { get; }
        public int LessonCount { get; }
        public TutorialStep Step { get; }

        public TutorialChanged(TutorialPhase phase, int lessonIndex, int lessonCount, TutorialStep step)
        {
            Phase = phase;
            LessonIndex = lessonIndex;
            LessonCount = lessonCount;
            Step = step;
        }
    }

    public enum TutorialEnd
    {
        // The last lesson was won and the player pressed Continue.
        Completed,

        // The player pressed Skip.
        Skipped,

        // The player went back to the menu in the middle of it.
        Left
    }

    public readonly struct TutorialEnded
    {
        public TutorialEnd Reason { get; }

        // Completed and Skipped mean the player has seen the tutorial; Left does not.
        public bool Seen => Reason != TutorialEnd.Left;

        public TutorialEnded(TutorialEnd reason)
        {
            Reason = reason;
        }
    }

    // A challenge began (the first time, or a new attempt after a retry). Challenges use their own events, so progress, stars,
    // rewards and the level popups never count them.
    public readonly struct ChallengeStarted
    {
        public int ChallengeIndex { get; }

        // How many challenges there are, for the HUD's "1 / 3".
        public int ChallengeCount { get; }

        public string ChallengeId { get; }

        // 0 means no move limit.
        public int MoveLimit { get; }

        public int PingBudget { get; }

        // Which try this is in the current session, counting from 1.
        public int Attempt { get; }

        public ChallengeStarted(int challengeIndex, int challengeCount, string challengeId, int moveLimit, int pingBudget, int attempt)
        {
            ChallengeIndex = challengeIndex;
            ChallengeCount = challengeCount;
            ChallengeId = challengeId;
            MoveLimit = moveLimit;
            PingBudget = pingBudget;
            Attempt = attempt;
        }
    }

    // The board of a challenge was cleared. The attempt is the list of everything the player did, ready to be replayed.
    public readonly struct ChallengeSolved
    {
        public int ChallengeIndex { get; }
        public string ChallengeId { get; }
        public ChallengeAttempt Attempt { get; }

        public ChallengeSolved(int challengeIndex, string challengeId, ChallengeAttempt attempt)
        {
            ChallengeIndex = challengeIndex;
            ChallengeId = challengeId;
            Attempt = attempt;
        }
    }

    // A challenge with a move limit ran out of moves.
    public readonly struct ChallengeFailed
    {
        public int ChallengeIndex { get; }
        public string ChallengeId { get; }
        public int MovesUsed { get; }

        public ChallengeFailed(int challengeIndex, string challengeId, int movesUsed)
        {
            ChallengeIndex = challengeIndex;
            ChallengeId = challengeId;
            MovesUsed = movesUsed;
        }
    }

    // A ping did nothing; the reason says why, so the HUD can tell the player.
    public readonly struct PingRefused
    {
        public PingResult Reason { get; }

        public PingRefused(PingResult reason)
        {
            Reason = reason;
        }
    }
}
