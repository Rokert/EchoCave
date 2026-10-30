namespace EchoCave.UI
{
    // What a popup button asks of the game. UiRoot adapts GameFlow to it, and the presenter's tests use a fake.
    public interface IPopupCommands
    {
        // Whether winning the current level leads to another one.
        bool HasNextLevel { get; }

        void Retry();
        void Next();
        void Menu();
        void Resume();
    }
}
