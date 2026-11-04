using System;

namespace EchoCave.UI
{
    // What the menu presenter needs from the menu view, so its tests can use a fake.
    public interface IMenuView
    {
        event Action PlayPressed;
        event Action HowToPlayPressed;

        // The "How to play" button only shows when the game has a tutorial.
        void SetHowToPlayVisible(bool visible);

        void Show();
        void Hide();
    }
}
