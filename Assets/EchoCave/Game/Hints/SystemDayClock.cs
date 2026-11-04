using System;

namespace EchoCave.Game
{
    // Adapts the device's local date to a day number. The player's own midnight is what counts, so the daily gift
    // is there when they wake up, wherever they are.
    public sealed class SystemDayClock : IDayClock
    {
        private static readonly DateTime Epoch = new DateTime(2020, 1, 1);

        public int Today => (DateTime.Now.Date - Epoch).Days + 1;
    }
}
