using System;
using System.Collections.Generic;
using EchoCave.Game;
using EchoCave.UI;

namespace EchoCave.Tests
{
    internal enum ShownPopup
    {
        None,
        Complete,
        Lost,
        Pause,
        ChallengeList,
        ChallengeResult
    }

    // Stands in for PopupsView and records which popup it was last told to show, and with what.
    internal sealed class FakePopupsView : IPopupsView
    {
        public event Action<PopupAction> ActionPressed;
        public event Action<AudioChannel, bool> ToggleChanged;
        public event Action<int> ChallengePressed;

        public ShownPopup Shown = ShownPopup.None;
        public int HideCount;
        public int Stars;
        public int MovesUsed;
        public int MoveLimit;
        public bool HasNext;
        public bool SoundOn;
        public bool MusicOn;
        public bool HowToPlayVisible;
        public IReadOnlyList<ChallengeRowData> Rows;
        public string Message;
        public string ResultTitle;
        public string CompleteNote = string.Empty;
        public IReadOnlyList<string> ResultLines;

        public void ShowChallenges(IReadOnlyList<ChallengeRowData> rows, string message)
        {
            Shown = ShownPopup.ChallengeList;
            Rows = rows;
            Message = message;
        }

        public void ShowChallengeResult(string title, IReadOnlyList<string> lines)
        {
            Shown = ShownPopup.ChallengeResult;
            ResultTitle = title;
            ResultLines = lines;
        }

        public void SetCompleteNote(string note) => CompleteNote = note;

        public void PressChallenge(int index) => ChallengePressed?.Invoke(index);

        public void SetHowToPlayVisible(bool visible) => HowToPlayVisible = visible;

        public void Hide()
        {
            Shown = ShownPopup.None;
            HideCount++;
        }

        public void ShowComplete(int stars, int movesUsed, int moveLimit, bool hasNext)
        {
            Shown = ShownPopup.Complete;
            Stars = stars;
            MovesUsed = movesUsed;
            MoveLimit = moveLimit;
            HasNext = hasNext;
        }

        public void ShowLost(int movesUsed, int moveLimit)
        {
            Shown = ShownPopup.Lost;
            MovesUsed = movesUsed;
            MoveLimit = moveLimit;
        }

        public void ShowPause(bool soundOn, bool musicOn)
        {
            Shown = ShownPopup.Pause;
            SoundOn = soundOn;
            MusicOn = musicOn;
        }

        public void Press(PopupAction action) => ActionPressed?.Invoke(action);

        public void Toggle(AudioChannel channel, bool enabled) => ToggleChanged?.Invoke(channel, enabled);
    }

    internal sealed class FakePopupCommands : IPopupCommands
    {
        public bool HasNextLevel { get; set; } = true;
        public bool HasTutorial { get; set; } = true;
        public int HowToPlays;
        public int Retries;
        public int Nexts;
        public int Menus;
        public int Resumes;

        public void Retry() => Retries++;

        public void Next() => Nexts++;

        public void Menu() => Menus++;

        public void Resume() => Resumes++;

        public void HowToPlay() => HowToPlays++;
    }
}
