using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class SystemDayClockTests
    {
        [Test]
        public void Today_IsAPositiveDayNumber_TheRewardsCanUse()
        {
            int today = new SystemDayClock().Today;

            Assert.GreaterOrEqual(today, 1);
            Assert.DoesNotThrow(() => EchoCave.Core.RewardMoment.DayStarted(today));
        }

        [Test]
        public void Today_NeverGoesBackwards_AndMovesByOneAtMost()
        {
            // Two reads a moment apart agree except across midnight, so retry once instead of failing at 23:59:59.
            int a = new SystemDayClock().Today;
            int b = new SystemDayClock().Today;
            if (a != b) b = new SystemDayClock().Today;

            Assert.GreaterOrEqual(b, a, "a day number never goes backwards while the program runs");
            Assert.LessOrEqual(b - a, 1, "a day number moves by at most one between two reads");
        }
    }
}
