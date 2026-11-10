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

        // The NEW tag on the Challenges button: a challenge is open that the player has not seen in the list yet.
        void SetChallengesNew(bool isNew);

        void Show();
        void Hide();
    }
}
