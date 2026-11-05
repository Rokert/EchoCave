namespace EchoCave.Game
{
    // Whether a hint could be given right now, without giving one.
    public enum HintStatus
    {
        // Not playing, or paused: the request would be ignored in silence.
        Unavailable,

        // A hint would be given.
        Ready,

        // A move is still animating.
        Busy,

        // The bat of the last hint is still on the board.
        AlreadyHinted,

        // The player holds no hints. The only status a rewarded ad can fix.
        NoHintsLeft,

        // A tutorial lesson with no hint of its own: the button is for a later lesson.
        NotInLesson,

        // A challenge has no hints.
        NotInChallenge
    }

    // What the hint refill needs from the game flow, so it can be tested without one.
    public interface IHintGate
    {
        HintStatus CheckHint();

        void RequestHint();
    }
}
