using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    public class GameFlowEchoTests
    {
        // Row of four cells. Bats 0 and 2 are hidden; bat 1 is not. The right-most bat leaves freely, bat 1
        // waits for it and bat 0 waits for bat 1. Two pings, no move limit.
        private static readonly LevelDefinition Echoed = new LevelDefinition(
            4,
            1,
            new[]
            {
                new BatDefinition(new Cell(0, 0), Direction.Right, true),
                new BatDefinition(new Cell(1, 0), Direction.Right, false),
                new BatDefinition(new Cell(3, 0), Direction.Right, true)
            },
            0, 2, 3, 3);

        // One visible bat that leaves freely: nothing to reveal.
        private static readonly LevelDefinition Plain = new LevelDefinition(
            3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 2, 1, 1);

        // Same bats as Echoed with a limit of three moves, so three wasted taps lose.
        private static readonly LevelDefinition Risky = new LevelDefinition(
            4,
            1,
            new[]
            {
                new BatDefinition(new Cell(0, 0), Direction.Right, true),
                new BatDefinition(new Cell(1, 0), Direction.Right, false),
                new BatDefinition(new Cell(3, 0), Direction.Right, true)
            },
            3, 2, 3, 3);

        private const float Reveal = 2f;

        private FakeBoardView _view;
        private GameFlow _flow;

        private readonly List<LevelStarted> _started = new List<LevelStarted>();
        private readonly List<PingUsed> _used = new List<PingUsed>();
        private readonly List<PingRefused> _refused = new List<PingRefused>();
        private int _expired;

        [SetUp]
        public void SetUp()
        {
            _started.Clear();
            _used.Clear();
            _refused.Clear();
            _expired = 0;

            _view = new FakeBoardView();
            var bus = new EventBus();
            bus.Subscribe<LevelStarted>(e => _started.Add(e));
            bus.Subscribe<PingUsed>(e => _used.Add(e));
            bus.Subscribe<PingRefused>(e => _refused.Add(e));
            bus.Subscribe<EchoExpired>(_ => _expired++);
            _flow = new GameFlow(_view, new[] { Echoed, Plain, Risky }, new MoveResolver(), bus, new RevealAllHiddenStrategy(Reveal));
        }

        private static Vector2 Center(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);

        private void StartLevel(int index)
        {
            _flow.RequestPlay(index);
            _flow.Tick(0f);
        }

        private void TapAndSettle(int x, int y)
        {
            _flow.HandleTap(Center(x, y));
            _view.FinishAnimation();
            _flow.Tick(0f);
        }

        [Test]
        public void LevelStarted_CarriesThePingBudget()
        {
            StartLevel(0);

            Assert.AreEqual(2, _started[0].PingBudget);
            Assert.AreEqual(2, _flow.Echo.PingsLeft);
        }

        [Test]
        public void Ping_RevealsTheHiddenBats_AndCostsNoMove()
        {
            StartLevel(0);

            _flow.RequestPing();

            CollectionAssert.AreEquivalent(new[] { 0, 2 }, _view.Revealed);
            Assert.AreEqual(1, _used.Count);
            Assert.AreEqual(1, _used[0].PingsLeft);
            Assert.AreEqual(Reveal, _used[0].DurationSeconds);
            Assert.AreEqual(0, _flow.Session.MovesUsed);
            Assert.AreEqual(0, _refused.Count);
        }

        [Test]
        public void Reveal_EndsAfterItsDuration_AndTheViewHidesTheBatsAgain()
        {
            StartLevel(0);
            _flow.RequestPing();

            _flow.Tick(1.5f);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, _view.Revealed);
            Assert.AreEqual(0, _expired);

            _flow.Tick(0.5f);

            Assert.IsEmpty(_view.Revealed);
            Assert.AreEqual(1, _expired);
        }

        [Test]
        public void NoPingsLeft_IsRefused_WithFeedback()
        {
            StartLevel(0);
            _flow.RequestPing();
            _flow.Tick(Reveal);
            _flow.RequestPing();
            _flow.Tick(Reveal);
            Assert.AreEqual(0, _flow.Echo.PingsLeft);
            Assert.AreEqual(0, _view.RejectedCount);

            _flow.RequestPing();

            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(PingResult.NoPingsLeft, _refused[0].Reason);
            Assert.AreEqual(1, _view.RejectedCount);
            Assert.IsEmpty(_view.Revealed);
        }

        [Test]
        public void PingWhileRevealing_IsRefused_WithFeedback_AndSpendsNothing()
        {
            StartLevel(0);
            _flow.RequestPing();

            _flow.RequestPing();

            Assert.AreEqual(PingResult.AlreadyActive, _refused[0].Reason);
            Assert.AreEqual(1, _view.RejectedCount);
            Assert.AreEqual(1, _flow.Echo.PingsLeft);
            Assert.AreEqual(1, _used.Count);
        }

        [Test]
        public void PingWithNothingHidden_IsRefused_WithFeedback()
        {
            StartLevel(1);

            _flow.RequestPing();

            Assert.AreEqual(PingResult.NothingToReveal, _refused[0].Reason);
            Assert.AreEqual(1, _view.RejectedCount);
            Assert.AreEqual(2, _flow.Echo.PingsLeft);
        }

        [Test]
        public void PingOutsidePlaying_IsIgnored()
        {
            _flow.RequestPing();
            Assert.AreEqual(GameState.Menu, _flow.State);

            StartLevel(1);
            TapAndSettle(1, 0);
            Assert.AreEqual(GameState.Won, _flow.State);
            _flow.RequestPing();

            Assert.AreEqual(0, _used.Count);
            Assert.AreEqual(0, _refused.Count);
            Assert.AreEqual(0, _view.RejectedCount);
        }

        [Test]
        public void PingingWhileAMoveAnimates_IsAllowed()
        {
            StartLevel(0);
            _flow.HandleTap(Center(3, 0));   // the free hidden bat leaves; its animation is still running

            _flow.RequestPing();

            Assert.AreEqual(1, _used.Count);
            CollectionAssert.AreEquivalent(new[] { 0 }, _view.Revealed, "bat 2 already left the board");
        }

        [Test]
        public void Retry_RestoresThePings_AndLeavesNoRevealRunning()
        {
            StartLevel(0);
            _flow.RequestPing();
            Assert.AreEqual(1, _flow.Echo.PingsLeft);

            _flow.RequestRetry();

            Assert.AreEqual(2, _flow.Echo.PingsLeft);
            Assert.IsFalse(_flow.Echo.IsActive);
            Assert.IsEmpty(_view.Revealed);

            _flow.Tick(10f);

            Assert.AreEqual(0, _expired, "a reveal carried over from the abandoned attempt would expire here");
        }

        [Test]
        public void LeavingForTheMenu_DropsTheEcho()
        {
            StartLevel(0);
            _flow.RequestPing();

            _flow.RequestMenu();
            _flow.Tick(0f);

            Assert.IsNull(_flow.Echo);

            _flow.Tick(10f);
            _flow.Tick(10f);

            Assert.AreEqual(0, _expired);
        }

        [Test]
        public void Losing_RevealsEveryHiddenBat()
        {
            StartLevel(2);

            TapAndSettle(0, 0);   // bounces
            TapAndSettle(0, 0);
            TapAndSettle(0, 0);

            Assert.AreEqual(GameState.Lost, _flow.State);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, _view.Revealed);
        }

        [Test]
        public void TheEchoClockStopsOnceTheLevelIsLost()
        {
            StartLevel(2);
            _flow.RequestPing();
            TapAndSettle(0, 0);
            TapAndSettle(0, 0);
            TapAndSettle(0, 0);
            Assert.AreEqual(GameState.Lost, _flow.State);

            _flow.Tick(10f);

            Assert.AreEqual(0, _expired);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, _view.Revealed, "the loss reveal must stay on screen");
        }
    }
}
