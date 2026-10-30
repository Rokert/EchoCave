using System;
using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class EchoServiceTests
    {
        // Hands back whatever the test sets, and counts how often it was asked.
        private sealed class FakeStrategy : IEchoStrategy
        {
            public int[] Ids = { 2, 3 };
            public float Duration = 2f;
            public int Calls;

            public EchoPulse CreatePulse(Board board)
            {
                Calls++;
                return new EchoPulse(Ids, Duration);
            }
        }

        private Board _board;
        private FakeStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _board = new Board(5, 5);
            _board.Add(new Bat(1, new Cell(0, 0), Direction.Up));
            _board.Add(new Bat(2, new Cell(1, 0), Direction.Up, true));
            _board.Add(new Bat(3, new Cell(2, 0), Direction.Up, true));
            _strategy = new FakeStrategy();
        }

        private EchoService Service(int budget) => new EchoService(_strategy, budget);

        [Test]
        public void Ping_SpendsOnePing_AndStartsTheReveal()
        {
            EchoService echo = Service(2);

            PingResult result = echo.TryPing(_board);

            Assert.AreEqual(PingResult.Pinged, result);
            Assert.AreEqual(1, echo.PingsLeft);
            Assert.IsTrue(echo.IsActive);
            Assert.AreEqual(2f, echo.RemainingSeconds);
            Assert.IsTrue(echo.IsRevealed(2));
            Assert.IsTrue(echo.IsRevealed(3));
            Assert.IsFalse(echo.IsRevealed(1));
        }

        [Test]
        public void Ping_AtZeroBudget_IsRefused_WithoutAskingTheStrategy()
        {
            EchoService echo = Service(0);
            int pinged = 0;
            echo.Pinged += _ => pinged++;

            PingResult result = echo.TryPing(_board);

            Assert.AreEqual(PingResult.NoPingsLeft, result);
            Assert.AreEqual(0, echo.PingsLeft);
            Assert.IsFalse(echo.IsActive);
            Assert.AreEqual(0, _strategy.Calls);
            Assert.AreEqual(0, pinged);
        }

        [Test]
        public void TheLastPing_IsSpent_ThenTheNextOneIsRefused()
        {
            EchoService echo = Service(1);

            Assert.AreEqual(PingResult.Pinged, echo.TryPing(_board));
            echo.Tick(2f);

            Assert.AreEqual(PingResult.NoPingsLeft, echo.TryPing(_board));
            Assert.AreEqual(0, echo.PingsLeft);
        }

        [Test]
        public void Reveal_LastsUntilTheDurationIsUp_NotBefore()
        {
            EchoService echo = Service(1);
            echo.TryPing(_board);

            echo.Tick(1.5f);

            Assert.IsTrue(echo.IsActive);
            Assert.AreEqual(0.5f, echo.RemainingSeconds);
            Assert.IsTrue(echo.IsRevealed(2));

            echo.Tick(0.5f);

            Assert.IsFalse(echo.IsActive);
            Assert.AreEqual(0f, echo.RemainingSeconds);
            Assert.IsFalse(echo.IsRevealed(2));
            Assert.AreEqual(0, echo.RevealedBatIds.Count);
        }

        [Test]
        public void Reveal_ExpiresEvenWhenTheFrameOvershoots()
        {
            EchoService echo = Service(1);
            echo.TryPing(_board);

            echo.Tick(10f);

            Assert.IsFalse(echo.IsActive);
            Assert.AreEqual(0f, echo.RemainingSeconds);
        }

        [Test]
        public void Expired_IsRaisedOnce()
        {
            EchoService echo = Service(1);
            int expired = 0;
            echo.Expired += () => expired++;
            echo.TryPing(_board);

            echo.Tick(1f);
            Assert.AreEqual(0, expired);

            echo.Tick(1f);
            echo.Tick(1f);

            Assert.AreEqual(1, expired);
        }

        [Test]
        public void Ping_WhileRevealing_IsRefused_AndSpendsNothing()
        {
            EchoService echo = Service(2);
            echo.TryPing(_board);
            echo.Tick(0.5f);

            PingResult result = echo.TryPing(_board);

            Assert.AreEqual(PingResult.AlreadyActive, result);
            Assert.AreEqual(1, echo.PingsLeft);
            Assert.AreEqual(1.5f, echo.RemainingSeconds, "the running reveal must not restart");
            Assert.AreEqual(1, _strategy.Calls);
        }

        [Test]
        public void Ping_AfterTheRevealEnded_IsAllowedAgain()
        {
            EchoService echo = Service(2);
            echo.TryPing(_board);
            echo.Tick(2f);

            PingResult result = echo.TryPing(_board);

            Assert.AreEqual(PingResult.Pinged, result);
            Assert.AreEqual(0, echo.PingsLeft);
            Assert.IsTrue(echo.IsActive);
        }

        [Test]
        public void Ping_WithNothingToReveal_SpendsNothing()
        {
            _strategy.Ids = new int[0];
            EchoService echo = Service(2);
            int pinged = 0;
            echo.Pinged += _ => pinged++;

            PingResult result = echo.TryPing(_board);

            Assert.AreEqual(PingResult.NothingToReveal, result);
            Assert.AreEqual(2, echo.PingsLeft);
            Assert.IsFalse(echo.IsActive);
            Assert.AreEqual(0, pinged);
        }

        [Test]
        public void Pinged_CarriesTheStrategysPulse()
        {
            EchoService echo = Service(1);
            var received = new List<EchoPulse>();
            echo.Pinged += received.Add;

            echo.TryPing(_board);

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(new[] { 2, 3 }, received[0].BatIds.ToArray());
            Assert.AreEqual(2f, received[0].DurationSeconds);
        }

        [Test]
        public void GrantPings_AddsToTheBudget_EvenFromZero()
        {
            EchoService echo = Service(0);

            echo.GrantPings(2);

            Assert.AreEqual(2, echo.PingsLeft);
            Assert.AreEqual(PingResult.Pinged, echo.TryPing(_board));
            Assert.AreEqual(1, echo.PingsLeft);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GrantPings_RejectsNonPositiveCounts(int count)
        {
            EchoService echo = Service(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => echo.GrantPings(count));
            Assert.AreEqual(1, echo.PingsLeft);
        }

        [Test]
        public void Tick_WithoutAReveal_DoesNothing()
        {
            EchoService echo = Service(1);
            int expired = 0;
            echo.Expired += () => expired++;

            echo.Tick(5f);

            Assert.IsFalse(echo.IsActive);
            Assert.AreEqual(0, expired);
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        public void Tick_RejectsTimeThatGoesBackwardsOrIsNotANumber(float delta)
        {
            EchoService echo = Service(1);
            echo.TryPing(_board);

            Assert.Throws<ArgumentOutOfRangeException>(() => echo.Tick(delta));
            Assert.AreEqual(2f, echo.RemainingSeconds);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Ping_RejectsAStrategyWithABadDuration_AndSpendsNothing(float duration)
        {
            _strategy.Duration = duration;
            EchoService echo = Service(1);

            Assert.Throws<InvalidOperationException>(() => echo.TryPing(_board));
            Assert.AreEqual(1, echo.PingsLeft);
            Assert.IsFalse(echo.IsActive);
        }

        [Test]
        public void WithTheDefaultStrategy_OnlyHiddenBatsAreRevealed()
        {
            var echo = new EchoService(new RevealAllHiddenStrategy(3f), 1);

            echo.TryPing(_board);

            Assert.IsFalse(echo.IsRevealed(1));
            Assert.IsTrue(echo.IsRevealed(2));
            Assert.IsTrue(echo.IsRevealed(3));
            Assert.AreEqual(3f, echo.RemainingSeconds);
        }

        [Test]
        public void Constructor_AndTryPing_RejectBadArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new EchoService(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EchoService(_strategy, -1));
            Assert.Throws<ArgumentNullException>(() => Service(1).TryPing(null));
        }
    }
}
