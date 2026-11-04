namespace EchoCave.UI
{
    // What a popup button asks of the game. UiRoot adapts GameFlow to it, and the presenter's tests use a fake.
    public interface IPopupCommands
    {
        // Whether winning the current level leads to another one.
        bool HasNextLevel { get; }

        // Whether the game has a tutorial to replay.
        bool HasTutorial { get; }

        void Retry();
        void Next();
        void Menu();
        void Resume();

        // Starts the tutorial again from its first lesson. When it ends, the game returns to the menu.
        void HowToPlay();
    }
}
