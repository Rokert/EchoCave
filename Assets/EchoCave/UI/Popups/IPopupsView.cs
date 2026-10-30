using System;
using EchoCave.Game;

namespace EchoCave.UI
{
    // The buttons the popups offer. Replay, Retry and Restart all start the level again; each keeps its own
    // name because each sits in a different popup.
    public enum PopupAction
    {
        Replay,
        Next,
        Retry,
        Menu,
        Resume,
        Restart
    }

    // What the popups presenter needs from the popups view, so its tests can use a fake.
    public interface IPopupsView
    {
        event Action<PopupAction> ActionPressed;

        // The player flipped a switch in the pause popup; the bool is the new state.
        event Action<AudioChannel, bool> ToggleChanged;

        // Hides whatever popup is showing, with its scrim.
        void Hide();

        // A moveLimit of 0 means the level has none. When there is no next level the Next button reads Menu.
        void ShowComplete(int stars, int movesUsed, int moveLimit, bool hasNext);

        void ShowLost(int movesUsed, int moveLimit);

        void ShowPause(bool soundOn, bool musicOn);
    }
}
