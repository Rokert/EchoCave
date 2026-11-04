using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class EchoPinTests
    {
        private static Board BoardWithHidden()
        {
            var board = new Board(4, 1);
            board.Add(new Bat(0, new Cell(0, 0), Direction.Right, true));
            board.Add(new Bat(1, new Cell(2, 0), Direction.Right, true));
            return board;
        }

        private static EchoService NewEcho(int pings = 2) => new EchoService(new RevealAllHiddenStrategy(2f), pings);

        [Test]
        public void APinnedBat_IsRevealed_WithNoEchoRunning()
        {
            EchoService echo = NewEcho();

            echo.Pin(1);

            Assert.IsTrue(echo.IsRevealed(1));
            CollectionAssert.AreEquivalent(new[] { 1 }, echo.RevealedBatIds);
            Assert.IsFalse(echo.IsActive);
        }

        [Test]
        public void APinnedBat_StaysRevealed_WhenTheEchoRunsOut()
        {
            EchoService echo = NewEcho();
            echo.TryPing(BoardWithHidden());
            echo.Pin(1);

            echo.Tick(2.1f);

            Assert.IsFalse(echo.IsActive);
            CollectionAssert.AreEquivalent(new[] { 1 }, echo.RevealedBatIds, "only the pinned bat outlives the echo");
            Assert.IsFalse(echo.IsRevealed(0));
        }

        [Test]
        public void TheEchoAndThePin_AreListedTogether_WithoutRepeats()
        {
            EchoService echo = NewEcho();
            echo.Pin(0);

            echo.TryPing(BoardWithHidden());

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, echo.RevealedBatIds);
            Assert.AreEqual(2, echo.RevealedBatIds.Count());
        }

        [Test]
        public void Pinning_CostsNoPing_AndDoesNotBlockTheNextPing()
        {
            EchoService echo = NewEcho(pings: 1);
            echo.Pin(0);

            Assert.AreEqual(1, echo.PingsLeft);
            Assert.AreEqual(PingResult.Pinged, echo.TryPing(BoardWithHidden()));
        }

        [Test]
        public void PinningTwice_ChangesNothing()
        {
            EchoService echo = NewEcho();
            echo.Pin(1);
            echo.Pin(1);

            Assert.AreEqual(1, echo.RevealedBatIds.Count());
        }

        [Test]
        public void ANewEcho_StartsWithNothingPinned()
        {
            EchoService first = NewEcho();
            first.Pin(1);

            Assert.IsEmpty(NewEcho().RevealedBatIds);
        }
    }
}
