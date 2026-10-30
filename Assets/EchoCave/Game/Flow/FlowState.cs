using Rokert.Core;

namespace EchoCave.Game
{
    // Base of the four flow states. They only react to entering; the transitions live in GameFlow.
    internal abstract class FlowState : IState
    {
        protected GameFlow Flow { get; }

        public abstract GameState Id { get; }

        protected FlowState(GameFlow flow)
        {
            Flow = flow;
        }

        public virtual void Enter() { }

        public virtual void Tick() { }

        public virtual void Exit() { }
    }

    internal sealed class MenuState : FlowState
    {
        public override GameState Id => GameState.Menu;

        public MenuState(GameFlow flow) : base(flow) { }

        public override void Enter() => Flow.ShowMenu();
    }

    internal sealed class PlayingState : FlowState
    {
        public override GameState Id => GameState.Playing;

        public PlayingState(GameFlow flow) : base(flow) { }

        public override void Enter() => Flow.BeginLevel();
    }

    internal sealed class WonState : FlowState
    {
        public override GameState Id => GameState.Won;

        public WonState(GameFlow flow) : base(flow) { }

        public override void Enter() => Flow.ReportWon();
    }

    internal sealed class LostState : FlowState
    {
        public override GameState Id => GameState.Lost;

        public LostState(GameFlow flow) : base(flow) { }

        public override void Enter() => Flow.ReportLost();
    }
}
