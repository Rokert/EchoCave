using System;
using EchoCave.Core;
using EchoCave.Game;
using Rokert.Core;

namespace EchoCave.UI
{
    // Keeps the overlay in step with the tutorial by listening to the EventBus, and forwards its buttons to the game.
    // Plain C#; Dispose unsubscribes from everything.
    public sealed class TutorialPresenter : IDisposable
    {
        public const string ReadLabel = "Got it";
        public const string NextLabel = "Next";
        public const string DoneLabel = "Done";
        public const string LessonDoneText = "Well done!";
        public const string LessonLostText = "Out of moves. Try again!";

        private readonly ITutorialView _view;
        private readonly EventBus _events;
        private readonly ITutorialCommands _commands;

        public TutorialPresenter(ITutorialView view, EventBus events, ITutorialCommands commands)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));

            _view.ContinuePressed += _commands.Continue;
            _view.RetryPressed += _commands.Retry;
            _view.SkipPressed += _commands.Skip;

            _events.Subscribe<TutorialChanged>(OnTutorialChanged);
            _events.Subscribe<LessonLost>(OnLessonLost);
            _events.Subscribe<TutorialEnded>(OnTutorialEnded);
            _events.Subscribe<MenuOpened>(OnMenuOpened);

            Hide();
        }

        public void Dispose()
        {
            _view.ContinuePressed -= _commands.Continue;
            _view.RetryPressed -= _commands.Retry;
            _view.SkipPressed -= _commands.Skip;

            _events.Unsubscribe<TutorialChanged>(OnTutorialChanged);
            _events.Unsubscribe<LessonLost>(OnLessonLost);
            _events.Unsubscribe<TutorialEnded>(OnTutorialEnded);
            _events.Unsubscribe<MenuOpened>(OnMenuOpened);
        }

        private void OnTutorialChanged(TutorialChanged changed)
        {
            switch (changed.Phase)
            {
                case TutorialPhase.InStep:
                    _view.SetVisible(true);
                    _view.SetText(changed.Step.Text);
                    _view.SetRetry(false);
                    _view.SetContinue(changed.Step.Advance == TutorialAdvance.Continue, ReadLabel);
                    _view.SetFocus(changed.Step.Focus, changed.Step.FocusBatId);
                    break;

                case TutorialPhase.LessonComplete:
                    bool isLast = changed.LessonIndex + 1 >= changed.LessonCount;
                    _view.SetVisible(true);
                    _view.SetText(LessonDoneText);
                    _view.SetRetry(false);
                    _view.SetContinue(true, isLast ? DoneLabel : NextLabel);
                    _view.SetFocus(TutorialFocus.None, -1);
                    break;

                default:
                    Hide();
                    break;
            }
        }

        // The lesson ran out of moves: say so and offer the retry, which starts the same lesson again.
        private void OnLessonLost(LessonLost lost)
        {
            _view.SetVisible(true);
            _view.SetText(LessonLostText);
            _view.SetContinue(false, string.Empty);
            _view.SetRetry(true);
            _view.SetFocus(TutorialFocus.None, -1);
        }

        private void OnTutorialEnded(TutorialEnded ended)
        {
            Hide();
        }

        private void OnMenuOpened(MenuOpened opened)
        {
            Hide();
        }

        private void Hide()
        {
            _view.SetVisible(false);
            _view.SetFocus(TutorialFocus.None, -1);
        }
    }
}
