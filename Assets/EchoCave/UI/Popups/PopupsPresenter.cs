using System;
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

        private int _moveLimit;
        private bool _soundOn;
        private bool _musicOn;

        public PopupsPresenter(IPopupsView view, EventBus events, IPopupCommands commands, bool soundOn = true, bool musicOn = true)
        {
            _soundOn = soundOn;
            _musicOn = musicOn;
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));

            _view.ActionPressed += OnActionPressed;
            _view.ToggleChanged += OnToggleChanged;

            _events.Subscribe<LevelStarted>(OnLevelStarted);
            _events.Subscribe<LessonStarted>(OnLessonStarted);
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

            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
            _events.Unsubscribe<LessonStarted>(OnLessonStarted);
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
