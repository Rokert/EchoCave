using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using Rokert.Core;

namespace EchoCave.UI
{
    // Decides which popup is on screen from the game's events and sends its buttons back to the game.
    // Plain C#; Dispose unsubscribes from everything.
    //
    // The sound and music switches live here, so they keep their state from level to level. They start from the
    // values the game was left with (saved between sessions) and publish AudioToggled when the player flips one;
    // the audio service and the save listen to it.
    public sealed class PopupsPresenter : IDisposable
    {
        private readonly IPopupsView _view;
        private readonly EventBus _events;
        private readonly IPopupCommands _commands;
        private readonly IChallengeSource _challenges;
        private readonly Action _challengesClosed;

        // How many results the list shows under a challenge.
        private const int TopResults = 3;

        private int _moveLimit;
        private bool _soundOn;
        private bool _musicOn;

        // challenges is optional: without it there is no challenge list. challengesClosed is called when the player leaves the
        // list, so the menu can come back.
        public PopupsPresenter(
            IPopupsView view, EventBus events, IPopupCommands commands, bool soundOn = true, bool musicOn = true,
            IChallengeSource challenges = null, Action challengesClosed = null)
        {
            _challenges = challenges;
            _challengesClosed = challengesClosed;
            _soundOn = soundOn;
            _musicOn = musicOn;
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));

            _view.ActionPressed += OnActionPressed;
            _view.ToggleChanged += OnToggleChanged;
            _view.ChallengePressed += OnChallengePressed;

            _events.Subscribe<LevelStarted>(OnLevelStarted);
            _events.Subscribe<LessonStarted>(OnLessonStarted);
            _events.Subscribe<ChallengeStarted>(OnChallengeStarted);
            _events.Subscribe<ChallengeFailed>(OnChallengeFailed);
            _events.Subscribe<ChallengeResultSaved>(OnChallengeResultSaved);
            _events.Subscribe<ChallengeVerified>(OnChallengeVerified);
            _events.Subscribe<LevelWon>(OnLevelWon);
            _events.Subscribe<LevelLost>(OnLevelLost);
            _events.Subscribe<PauseChanged>(OnPauseChanged);
            _events.Subscribe<MenuOpened>(OnMenuOpened);

            _view.SetHowToPlayVisible(_commands.HasTutorial);
            _view.Hide();
        }

        public void Dispose()
        {
            _view.ActionPressed -= OnActionPressed;
            _view.ToggleChanged -= OnToggleChanged;
            _view.ChallengePressed -= OnChallengePressed;

            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
            _events.Unsubscribe<LessonStarted>(OnLessonStarted);
            _events.Unsubscribe<ChallengeStarted>(OnChallengeStarted);
            _events.Unsubscribe<ChallengeFailed>(OnChallengeFailed);
            _events.Unsubscribe<ChallengeResultSaved>(OnChallengeResultSaved);
            _events.Unsubscribe<ChallengeVerified>(OnChallengeVerified);
            _events.Unsubscribe<LevelWon>(OnLevelWon);
            _events.Unsubscribe<LevelLost>(OnLevelLost);
            _events.Unsubscribe<PauseChanged>(OnPauseChanged);
            _events.Unsubscribe<MenuOpened>(OnMenuOpened);
        }

        private void OnLevelStarted(LevelStarted started)
        {
            _moveLimit = started.MoveLimit;
            _view.Hide();
        }

        // A lesson has its own overlay for winning and losing, so only the popup that was open is closed.
        private void OnLessonStarted(LessonStarted started)
        {
            _view.Hide();
        }

        // A challenge keeps its move limit for the lost popup, and closes whatever popup was open.
        private void OnChallengeStarted(ChallengeStarted started)
        {
            _moveLimit = started.MoveLimit;
            _view.Hide();
        }

        // Only a challenge with a move limit can run out of moves.
        private void OnChallengeFailed(ChallengeFailed failed)
        {
            _view.ShowLost(failed.MovesUsed, _moveLimit);
        }

        // The result was kept: how it went, and where it ranks among the results of this device.
        private void OnChallengeResultSaved(ChallengeResultSaved saved)
        {
            var lines = new List<string>
            {
                ChallengeText.Result(saved.Result.Moves, saved.Result.Seconds),
                ChallengeText.PingsUsed(saved.Result.Pings),
                ChallengeText.Tries(saved.Attempts) + " so far",
                ChallengeText.Rank(saved.Rank)
            };
            _view.ShowChallengeResult(saved.IsBest ? "New best!" : "Challenge solved", lines);
        }

        // A solved board the replay did not accept is not kept, but the player must not be left on an empty board.
        private void OnChallengeVerified(ChallengeVerified verified)
        {
            if (verified.Result.IsValid) return;

            _view.ShowChallengeResult(
                "Not counted",
                new List<string> { "The game could not confirm this run.", "Reason: " + verified.Result.Error });
        }

        // Opens the list of challenges (the menu's Challenges button). Nothing happens when the game has none.
        public void OpenChallenges()
        {
            if (_challenges == null) return;

            bool unlocked = _challenges.Unlocked;
            var rows = new List<ChallengeRowData>(_challenges.Count);
            for (int i = 0; i < _challenges.Count; i++)
            {
                IReadOnlyList<ChallengeResult> top = _challenges.TopOf(i, TopResults);
                int attempts = _challenges.AttemptsOf(i);

                string summary;
                if (top.Count > 0) summary = "Best: " + ChallengeText.Result(top[0].Moves, top[0].Seconds) + " (" + ChallengeText.Tries(attempts) + ")";
                else if (attempts > 0) summary = "Not solved yet (" + ChallengeText.Tries(attempts) + ")";
                else summary = "Not tried yet";

                var lines = new List<string>(top.Count);
                for (int r = 0; r < top.Count; r++) lines.Add((r + 1) + ". " + ChallengeText.Result(top[r].Moves, top[r].Seconds));

                rows.Add(new ChallengeRowData(_challenges.IdOf(i), summary, lines, unlocked));
            }

            _view.ShowChallenges(rows, unlocked ? string.Empty : ChallengeText.LockedMessage(_challenges.LevelsLeft));
        }

        private void OnChallengePressed(int index)
        {
            if (_challenges == null || !_challenges.Unlocked || index < 0 || index >= _challenges.Count) return;

            _view.Hide();
            _challenges.Play(index);
        }

        private void OnLevelWon(LevelWon won)
        {
            _view.ShowComplete(won.Stars, won.MovesUsed, _moveLimit, _commands.HasNextLevel);
        }

        private void OnLevelLost(LevelLost lost)
        {
            _view.ShowLost(lost.MovesUsed, _moveLimit);
        }

        private void OnPauseChanged(PauseChanged changed)
        {
            if (changed.IsPaused) _view.ShowPause(_soundOn, _musicOn);
            else _view.Hide();
        }

        private void OnMenuOpened(MenuOpened opened)
        {
            _view.Hide();
        }

        // The popup goes away at once, so the button answers before the game has moved on.
        private void OnActionPressed(PopupAction action)
        {
            _view.Hide();

            switch (action)
            {
                case PopupAction.Replay:
                case PopupAction.Retry:
                case PopupAction.Restart:
                    _commands.Retry();
                    break;
                case PopupAction.Next:
                    _commands.Next();
                    break;
                case PopupAction.Menu:
                    _commands.Menu();
                    break;
                case PopupAction.Resume:
                    _commands.Resume();
                    break;
                case PopupAction.HowToPlay:
                    _commands.HowToPlay();
                    break;
                case PopupAction.CloseChallenges:
                    _challengesClosed?.Invoke();
                    break;
            }
        }

        private void OnToggleChanged(AudioChannel channel, bool enabled)
        {
            if (channel == AudioChannel.Sound) _soundOn = enabled;
            else _musicOn = enabled;

            _events.Publish(new AudioToggled(channel, enabled));
        }
    }
}
