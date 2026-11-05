using System;

namespace EchoCave.UI
{
    // What the menu presenter needs from the menu view, so its tests can use a fake.
    public interface IMenuView
    {
        event Action PlayPressed;
        event Action HowToPlayPressed;
        event Action ChallengesPressed;

        // The "How to play" button only shows when the game has a tutorial.
        void SetHowToPlayVisible(bool visible);

        // The "Challenges" button only shows when the game has challenges.
        void SetChallengesVisible(bool visible);

        void Show();
        void Hide();
    }
}
