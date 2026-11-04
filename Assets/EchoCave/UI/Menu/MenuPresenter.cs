using System;
using EchoCave.Game;
using Rokert.Core;

namespace EchoCave.UI
{
    // Shows the menu while the game is in it, starts the game when Play is pressed and the tutorial when How to play is.
    // Plain C#; Dispose unsubscribes from everything.
    public sealed class MenuPresenter : IDisposable
    {
        private readonly IMenuView _view;
        private readonly EventBus _events;
        private readonly Action _play;
        private readonly Action _howToPlay;

        // startInMenu is the flow's state at the moment the UI is built: the flow raises MenuOpened in its
        // constructor, before any subscriber exists.
        // howToPlay is optional: without it the button stays hidden.
        public MenuPresenter(IMenuView view, EventBus events, Action play, bool startInMenu, Action howToPlay = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _play = play ?? throw new ArgumentNullException(nameof(play));
            _howToPlay = howToPlay;

            _view.PlayPressed += OnPlayPressed;
            _view.HowToPlayPressed += OnHowToPlayPressed;
            _events.Subscribe<MenuOpened>(OnMenuOpened);
            _events.Subscribe<LevelStarted>(OnLevelStarted);
            _events.Subscribe<LessonStarted>(OnLessonStarted);

            _view.SetHowToPlayVisible(_howToPlay != null);

            if (startInMenu) _view.Show();
            else _view.Hide();
        }

        public void Dispose()
        {
            _view.PlayPressed -= OnPlayPressed;
            _view.HowToPlayPressed -= OnHowToPlayPressed;
            _events.Unsubscribe<MenuOpened>(OnMenuOpened);
            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
            _events.Unsubscribe<LessonStarted>(OnLessonStarted);
        }

        // The menu goes away at once, so the button answers before the level has loaded.
        private void OnPlayPressed()
        {
            _view.Hide();
            _play();
        }

        private void OnHowToPlayPressed()
        {
            if (_howToPlay == null) return;

            _view.Hide();
            _howToPlay();
        }

        private void OnMenuOpened(MenuOpened opened) => _view.Show();

        private void OnLevelStarted(LevelStarted started) => _view.Hide();

        // A lesson is on screen too, so the menu must not cover it.
        private void OnLessonStarted(LessonStarted started) => _view.Hide();
    }
}
