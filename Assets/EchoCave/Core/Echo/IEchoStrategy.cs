namespace EchoCave.Core
{
    // Decides what a ping reveals. It only looks at the board; the service owns budget and timing.
    public interface IEchoStrategy
    {
        // An empty list means there is nothing to reveal, and the service will not spend a ping.
        EchoPulse CreatePulse(Board board);
    }
}
