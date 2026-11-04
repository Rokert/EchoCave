using System;
using EchoCave.Game;

namespace EchoCave.UI
{
    // Lets the tutorial presenter talk to the game flow without depending on it.
    public sealed class FlowTutorialCommands : ITutorialCommands
    {
        private readonly GameFlow _flow;

        public FlowTutorialCommands(GameFlow flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
        }

        public void Continue() => _flow.TutorialContinue();

        public void Retry() => _flow.RequestRetry();

        public void Skip() => _flow.TutorialSkip();
    }
}
