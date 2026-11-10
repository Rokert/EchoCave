using System;
using EchoCave.UI;

namespace EchoCave.Tests
{
    // Stands in for MenuView and records whether the menu is showing.
    internal sealed class FakeMenuView : IMenuView
    {
        public event Action PlayPressed;
        public event Action HowToPlayPressed;
        public event Action ChallengesPressed;

        public bool HowToPlayVisible;
        public bool ChallengesVisible;

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

        public void SetHowToPlayVisible(bool visible) => HowToPlayVisible = visible;

        public void SetChallengesVisible(bool visible) => ChallengesVisible = visible;

        public bool ChallengesNew;

        public void SetChallengesNew(bool isNew) => ChallengesNew = isNew;

        public void PressChallenges() => ChallengesPressed?.Invoke();

        public void PressHowToPlay() => HowToPlayPressed?.Invoke();

        public void PressPlay() => PlayPressed?.Invoke();

        public bool HasPlaySubscriber => PlayPressed != null;
    }
}
