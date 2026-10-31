using System;
using EchoCave.Game;
using Rokert.Core;

namespace EchoCave.UI
{
    // Shows the menu while the game is in it and starts the first level when Play is pressed.
    // Plain C#; Dispose unsubscribes from everything.
    public sealed class MenuPresenter : IDisposable
    {
        private readonly IMenuView _view;
        private readonly EventBus _events;
        private readonly Action _play;

        // startInMenu is the flow's state at the moment the UI is built: the flow raises MenuOpened in its
        // constructor, before any subscriber exists.
        public MenuPresenter(IMenuView view, EventBus events, Action play, bool startInMenu)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _play = play ?? throw new ArgumentNullException(nameof(play));

            _view.PlayPressed += OnPlayPressed;
            _events.Subscribe<MenuOpened>(OnMenuOpened);
            _events.Subscribe<LevelStarted>(OnLevelStarted);

            if (startInMenu) _view.Show();
            else _view.Hide();
        }

        public void Dispose()
        {
            _view.PlayPressed -= OnPlayPressed;
            _events.Unsubscribe<MenuOpened>(OnMenuOpened);
            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
        }

        // The menu goes away at once, so the button answers before the level has loaded.
        private void OnPlayPressed()
        {
            _view.Hide();
            _play();
        }

        private void OnMenuOpened(MenuOpened opened) => _view.Show();

        private void OnLevelStarted(LevelStarted started) => _view.Hide();
    }
}
