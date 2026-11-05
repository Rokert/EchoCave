using EchoCave.UI;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeTextTests
    {
        [Test]
        public void Time_ShowsMinutesSecondsAndTenths()
        {
            Assert.AreEqual("0:00.0", ChallengeText.Time(0f));
            Assert.AreEqual("0:45.2", ChallengeText.Time(45.2f));
            Assert.AreEqual("1:00.0", ChallengeText.Time(60f));
            Assert.AreEqual("2:20.5", ChallengeText.Time(140.5f));
            Assert.AreEqual("12:03.4", ChallengeText.Time(723.4f));
        }

        [Test]
        public void Time_RoundsToTheNearestTenth_AndCarriesIntoTheNextSecondAndMinute()
        {
            Assert.AreEqual("0:01.0", ChallengeText.Time(0.96f));
            Assert.AreEqual("1:00.0", ChallengeText.Time(59.96f));
        }

        [Test]
        public void Time_NeverShowsANonsenseNumber()
        {
            Assert.AreEqual("0:00.0", ChallengeText.Time(-5f));
            Assert.AreEqual("0:00.0", ChallengeText.Time(float.NaN));
            Assert.AreEqual("0:00.0", ChallengeText.Time(float.PositiveInfinity));
        }

        [Test]
        public void CountsAreSingularForOne()
        {
            Assert.AreEqual("1 tap", ChallengeText.Taps(1));
            Assert.AreEqual("52 taps", ChallengeText.Taps(52));
            Assert.AreEqual("1 try", ChallengeText.Tries(1));
            Assert.AreEqual("7 tries", ChallengeText.Tries(7));
            Assert.AreEqual("No pings used", ChallengeText.PingsUsed(0));
            Assert.AreEqual("1 ping used", ChallengeText.PingsUsed(1));
            Assert.AreEqual("2 pings used", ChallengeText.PingsUsed(2));
        }

        [Test]
        public void ResultRankAndLockMessage_ReadAsSentences()
        {
            Assert.AreEqual("52 taps in 2:20.5", ChallengeText.Result(52, 140.5f));
            Assert.AreEqual("Rank 3 on this device", ChallengeText.Rank(3));
            Assert.AreEqual("Win 1 more level to unlock", ChallengeText.LockedMessage(1));
            Assert.AreEqual("Win 4 more levels to unlock", ChallengeText.LockedMessage(4));
        }
    }
}
