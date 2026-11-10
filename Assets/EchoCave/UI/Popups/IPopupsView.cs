using System;
using System.Collections.Generic;
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
        Restart,
        HowToPlay,

        // Back from the challenge list.
        CloseChallenges
    }

    // One challenge in the list: its name, how it went, the best results to show under it, and whether it can be played.
    public readonly struct ChallengeRowData
    {
        public string Title { get; }
        public string Summary { get; }
        public IReadOnlyList<string> Lines { get; }
        public bool CanPlay { get; }

        public ChallengeRowData(string title, string summary, IReadOnlyList<string> lines, bool canPlay)
        {
            Title = title;
            Summary = summary;
            Lines = lines;
            CanPlay = canPlay;
        }
    }

    // What the popups presenter needs from the popups view, so its tests can use a fake.
    public interface IPopupsView
    {
        event Action<PopupAction> ActionPressed;

        // The player flipped a switch in the pause popup; the bool is the new state.
        event Action<AudioChannel, bool> ToggleChanged;

        // Play was pressed on a challenge of the list, by its position.
        event Action<int> ChallengePressed;

        // Hides whatever popup is showing, with its scrim.
        void Hide();

        // A moveLimit of 0 means the level has none. When there is no next level the Next button reads Menu.
        void ShowComplete(int stars, int movesUsed, int moveLimit, bool hasNext);

        void ShowLost(int movesUsed, int moveLimit);

        void ShowPause(bool soundOn, bool musicOn);

        // The challenge list. A message (empty for none) is shown above it, for example why the challenges are locked.
        void ShowChallenges(IReadOnlyList<ChallengeRowData> rows, string message);

        // A line under the stars of the level complete popup, for news such as a challenge that just opened. Empty hides
        // it. Set it before ShowComplete.
        void SetCompleteNote(string note);

        // A solved challenge: a title and a few lines. Also used to say a run was not counted.
        void ShowChallengeResult(string title, IReadOnlyList<string> lines);

        // The "How to play" button of the pause popup only shows when the game has a tutorial.
        void SetHowToPlayVisible(bool visible);
    }
}
