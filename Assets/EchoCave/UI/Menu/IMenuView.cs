using System;

namespace EchoCave.UI
{
    // What the menu presenter needs from the menu view, so its tests can use a fake.
    public interface IMenuView
    {
        event Action PlayPressed;

        void Show();
        void Hide();
    }
}
