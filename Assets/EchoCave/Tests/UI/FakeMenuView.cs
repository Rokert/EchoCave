using System;
using EchoCave.UI;

namespace EchoCave.Tests
{
    // Stands in for MenuView and records whether the menu is showing.
    internal sealed class FakeMenuView : IMenuView
    {
        public event Action PlayPressed;

        public bool Visible;
        public int ShowCount;
        public int HideCount;

        public void Show()
        {
            Visible = true;
            ShowCount++;
        }

        public void Hide()
        {
            Visible = false;
            HideCount++;
        }

        public void PressPlay() => PlayPressed?.Invoke();

        public bool HasPlaySubscriber => PlayPressed != null;
    }
}
