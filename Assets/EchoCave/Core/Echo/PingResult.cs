namespace EchoCave.Core
{
    public enum PingResult
    {
        // The ping was spent and the reveal started.
        Pinged,

        // The budget is empty. Nothing was spent.
        NoPingsLeft,

        // A reveal is still running. Nothing was spent.
        AlreadyActive,

        // No hidden bat to reveal. Nothing was spent.
        NothingToReveal
    }
}
