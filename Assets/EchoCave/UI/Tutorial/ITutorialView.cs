using System;
using EchoCave.Core;

namespace EchoCave.UI
{
    // What the tutorial presenter needs from the overlay, so its tests can use a fake.
    public interface ITutorialView
    {
        event Action ContinuePressed;
        event Action RetryPressed;
        event Action SkipPressed;

        // Shows or hides the whole overlay: the bubble, the ring and the buttons.
        void SetVisible(bool visible);

        void SetText(string text);

        // The Continue button, with the word it shows ("Got it", "Next", "Done").
        void SetContinue(bool visible, string label);

        void SetRetry(bool visible);

        // Draws a ring on the thing to use: a bat (by its index in the level), the ping button or the hint button.
        // None hides it.
        void SetFocus(TutorialFocus focus, int batId);
    }

    // Where things are on the panel, which only the board and the camera know.
    public interface ITutorialTargets
    {
        // The square a bat takes on the panel, in panel coordinates. False when the bat is not on the board.
        bool TryGetBatRect(int batId, out UnityEngine.Rect rect);
    }

    // What the overlay's buttons ask of the game. UiRoot adapts GameFlow to it, and the tests use a fake.
    public interface ITutorialCommands
    {
        void Continue();
        void Retry();
        void Skip();
    }
}
