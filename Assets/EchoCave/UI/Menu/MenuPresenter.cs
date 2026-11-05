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
        private readonly Action _challenges;

        // startInMenu is the flow's state at the moment the UI is built: the flow raises MenuOpened in its
        // constructor, before any subscriber exists.
        // howToPlay is optional: without it the button stays hidden.
        // challenges is optional too: it opens the list of challenges, and the button stays hidden without it.
        public MenuPresenter(IMenuView view, EventBus events, Action play, bool startInMenu, Action howToPlay = null, Action challenges = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _play = play ?? throw new ArgumentNullException(nameof(play));
            _howToPlay = howToPlay;
            _challenges = challenges;

            _view.PlayPressed += OnPlayPressed;
            _view.HowToPlayPressed += OnHowToPlayPressed;
            _view.ChallengesPressed += OnChallengesPressed;
            _events.Subscribe<MenuOpened>(OnMenuOpened);
            _events.Subscribe<LevelStarted>(OnLevelStarted);
            _events.Subscribe<LessonStarted>(OnLessonStarted);
            _events.Subscribe<ChallengeStarted>(OnChallengeStarted);

            _view.SetHowToPlayVisible(_howToPlay != null);
            _view.SetChallengesVisible(_challenges != null);

            if (startInMenu) _view.Show();
            else _view.Hide();
        }

        public void Dispose()
        {
            _view.PlayPressed -= OnPlayPressed;
            _view.HowToPlayPressed -= OnHowToPlayPressed;
            _view.ChallengesPressed -= OnChallengesPressed;
            _events.Unsubscribe<MenuOpened>(OnMenuOpened);
            _events.Unsubscribe<LevelStarted>(OnLevelStarted);
            _events.Unsubscribe<LessonStarted>(OnLessonStarted);
            _events.Unsubscribe<ChallengeStarted>(OnChallengeStarted);
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

        // The menu steps aside for the challenge list, which brings it back when it closes.
        private void OnChallengesPressed()
        {
            if (_challenges == null) return;

            _view.Hide();
            _challenges();
        }

        // Shows the menu again, for whatever covered it (the challenge list) to call when it is done.
        public void Show() => _view.Show();

        private void OnMenuOpened(MenuOpened opened) => _view.Show();

        private void OnLevelStarted(LevelStarted started) => _view.Hide();

        // A lesson is on screen too, so the menu must not cover it.
        private void OnLessonStarted(LessonStarted started) => _view.Hide();

        // So is a challenge.
        private void OnChallengeStarted(ChallengeStarted started) => _view.Hide();
    }
}
