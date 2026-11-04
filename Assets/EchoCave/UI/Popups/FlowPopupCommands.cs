using System;
using EchoCave.Game;

namespace EchoCave.UI
{
    // Lets the popups presenter talk to the game flow without depending on it.
    public sealed class FlowPopupCommands : IPopupCommands
    {
        private readonly GameFlow _flow;

        public FlowPopupCommands(GameFlow flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
        }

        public bool HasNextLevel => _flow.LevelIndex + 1 < _flow.LevelCount;

        public bool HasTutorial => _flow.HasTutorial;

        public void HowToPlay() => _flow.RequestTutorial(thenStartGame: false);

        public void Retry() => _flow.RequestRetry();

        public void Next() => _flow.RequestNext();

        public void Menu() => _flow.RequestMenu();

        public void Resume() => _flow.RequestResume();
    }
}
