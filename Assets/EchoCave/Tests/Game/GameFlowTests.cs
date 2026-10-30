using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    public class GameFlowTests
    {
        // Level 0: one bat that leaves freely, no limit. Level 1: two bats in a row, limit 2. The bat on the
        // left is blocked by the one on the right, so it has to go second or the limit runs out.
        private static readonly LevelDefinition Easy = new LevelDefinition(
            3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 0, 1, 1);

        private static readonly LevelDefinition Tight = new LevelDefinition(
            3, 1,
            new[]
            {
                new BatDefinition(new Cell(0, 0), Direction.Right, false),
                new BatDefinition(new Cell(1, 0), Direction.Right, false)
            },
            2, 0, 2, 2);

        private FakeBoardView _view;
        private EventBus _bus;
        private GameFlow _flow;

        // These levels have no hidden bats and no pings; the echo has its own tests.
        private readonly IEchoStrategy _echoStrategy = new RevealAllHiddenStrategy(2f);

        private readonly List<LevelStarted> _started = new List<LevelStarted>();
        private readonly List<LevelWon> _won = new List<LevelWon>();
        private readonly List<LevelLost> _lost = new List<LevelLost>();

        [SetUp]
        public void SetUp()
        {
            _started.Clear();
            _won.Clear();
            _lost.Clear();

            _view = new FakeBoardView();
            _bus = new EventBus();
            _bus.Subscribe<LevelStarted>(e => _started.Add(e));
            _bus.Subscribe<LevelWon>(e => _won.Add(e));
            _bus.Subscribe<LevelLost>(e => _lost.Add(e));
            _flow = new GameFlow(_view, new[] { Easy, Tight }, new MoveResolver(), _bus, _echoStrategy);
        }

        private static Vector2 Center(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);

        private void StartLevel(int index)
        {
            _flow.RequestPlay(index);
            _flow.Tick(0f);
        }

        // One tap, its animation, and the frame after it.
        private void TapAndSettle(int x, int y)
        {
            _flow.HandleTap(Center(x, y));
            _view.FinishAnimation();
            _flow.Tick(0f);
        }

        private void WinEasy()
        {
            StartLevel(0);
            TapAndSettle(1, 0);
        }

        private void LoseTight()
        {
            StartLevel(1);
            TapAndSettle(0, 0);   // bounces
            TapAndSettle(1, 0);   // exits, limit reached with bat 0 left
        }

        [Test]
        public void Start_BeginsInTheMenu_WithAnEmptyView()
        {
            Assert.AreEqual(GameState.Menu, _flow.State);
            Assert.IsNull(_flow.Session);
            Assert.AreEqual(1, _view.ClearCount);
            Assert.IsNull(_view.Built);
        }

        [Test]
        public void RequestPlay_BuildsTheLevel_AndPublishesLevelStarted()
        {
            StartLevel(1);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(2, _view.Built.Count);
            Assert.AreEqual(1, _started.Count);
            Assert.AreEqual(1, _started[0].LevelIndex);
            Assert.AreEqual(2, _started[0].MoveLimit);
        }

        [Test]
        public void ClearingTheBoard_WinsOnlyAfterTheAnimationEnds()
        {
            StartLevel(0);

            _flow.HandleTap(Center(1, 0));
            _flow.Tick(0f);
            Assert.AreEqual(GameState.Playing, _flow.State, "the bat is still flying");
            Assert.AreEqual(0, _won.Count);

            _view.FinishAnimation();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Won, _flow.State);
            Assert.AreEqual(1, _won.Count);
            Assert.AreEqual(0, _won[0].LevelIndex);
            Assert.AreEqual(1, _won[0].MovesUsed);
            Assert.AreEqual(3, _won[0].Stars);
        }

        [Test]
        public void ClearingTheBoardOnTheLastAllowedMove_Wins()
        {
            StartLevel(1);

            TapAndSettle(1, 0);
            TapAndSettle(0, 0);

            Assert.AreEqual(GameState.Won, _flow.State);
            Assert.AreEqual(0, _lost.Count);
            Assert.AreEqual(2, _won[0].MovesUsed);
            Assert.AreEqual(3, _won[0].Stars);
        }

        [Test]
        public void UsingUpTheMoves_LosesOnlyAfterTheAnimationEnds()
        {
            StartLevel(1);
            TapAndSettle(0, 0);

            _flow.HandleTap(Center(1, 0));
            _flow.Tick(0f);
            Assert.AreEqual(GameState.Playing, _flow.State, "the last move is still animating");

            _view.FinishAnimation();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Lost, _flow.State);
            Assert.AreEqual(1, _lost.Count);
            Assert.AreEqual(2, _lost[0].MovesUsed);
            Assert.AreEqual(0, _won.Count);
        }

        [Test]
        public void RetryAfterWinning_RebuildsTheLevelFromScratch()
        {
            WinEasy();

            _flow.RequestRetry();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(0, _flow.Session.MovesUsed);
            Assert.AreEqual(1, _flow.Session.Board.Count);
            Assert.AreEqual(0, _flow.LevelIndex);
            Assert.AreEqual(2, _started.Count);
        }

        [Test]
        public void RetryAfterLosing_RestartsTheSameLevel()
        {
            LoseTight();

            _flow.RequestRetry();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(1, _flow.LevelIndex);
            Assert.AreEqual(0, _flow.Session.MovesUsed);
            Assert.AreEqual(2, _flow.Session.Board.Count);
        }

        [Test]
        public void NextAfterWinning_LoadsTheNextLevel()
        {
            WinEasy();

            _flow.RequestNext();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(1, _flow.LevelIndex);
            Assert.AreEqual(2, _view.Built.Count);
        }

        [Test]
        public void NextOnTheLastLevel_ReturnsToTheMenu()
        {
            StartLevel(1);
            TapAndSettle(1, 0);
            TapAndSettle(0, 0);
            Assert.AreEqual(GameState.Won, _flow.State);

            _flow.RequestNext();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Menu, _flow.State);
        }

        [Test]
        public void NextAfterLosing_IsIgnored()
        {
            LoseTight();

            _flow.RequestNext();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Lost, _flow.State);
            Assert.AreEqual(1, _flow.LevelIndex);
        }

        [Test]
        public void RetryWhilePlaying_RestartsInPlace_AndDropsTheRunningAnimation()
        {
            StartLevel(1);
            _flow.HandleTap(Center(0, 0));   // bounce, still animating
            Assert.AreEqual(1, _flow.Session.MovesUsed);

            _flow.RequestRetry();

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(0, _flow.Session.MovesUsed);
            Assert.AreEqual(2, _view.BuildCount);

            _flow.HandleTap(Center(1, 0));   // a busy presenter would ignore this
            Assert.AreEqual(1, _flow.Session.MovesUsed);
        }

        [Test]
        public void MenuRequest_ClearsTheBoard_FromAnyOtherState()
        {
            StartLevel(0);
            _flow.RequestMenu();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Menu, _flow.State);
            Assert.IsNull(_flow.Session);
            Assert.IsNull(_view.Built);

            WinEasy();
            _flow.RequestMenu();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Menu, _flow.State);
        }

        [Test]
        public void IntentsThatDoNotApplyToTheState_AreIgnored()
        {
            _flow.RequestRetry();
            _flow.RequestNext();
            _flow.RequestMenu();
            _flow.Tick(0f);
            Assert.AreEqual(GameState.Menu, _flow.State);

            StartLevel(0);
            _flow.RequestNext();
            _flow.Tick(0f);
            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(0, _flow.LevelIndex);
        }

        // Retry wins the frame, so the menu flag is never consumed. If it survived the state change it would
        // pull the player out of the level on the next Tick.
        [Test]
        public void AnIntentThatLostTheRace_DoesNotFireInTheNextState()
        {
            WinEasy();

            _flow.RequestRetry();
            _flow.RequestMenu();
            _flow.Tick(0f);
            Assert.AreEqual(GameState.Playing, _flow.State);

            _flow.Tick(0f);
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
        }

        [Test]
        public void TapsOutsidePlaying_AreIgnored()
        {
            LoseTight();
            int playedBefore = _view.Played.Count;

            _flow.HandleTap(Center(0, 0));

            Assert.AreEqual(playedBefore, _view.Played.Count);
            Assert.AreEqual(2, _flow.Session.MovesUsed);
        }

        [Test]
        public void RequestPlay_RejectsAnUnknownLevel()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _flow.RequestPlay(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => _flow.RequestPlay(-1));
        }

        // Two-star moves above the move limit: valid to build in code, rejected by the validator.
        [Test]
        public void Constructor_RejectsAnInvalidLevel_AndNamesIt()
        {
            var invalid = new LevelDefinition(
                3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 5, 0, 5, 7);

            var thrown = Assert.Throws<ArgumentException>(
                () => new GameFlow(_view, new[] { Easy, invalid }, new MoveResolver(), _bus, _echoStrategy));

            StringAssert.Contains("Level 2", thrown.Message);
        }

        [Test]
        public void Constructor_RejectsNullsAndAnEmptyCatalog()
        {
            var resolver = new MoveResolver();
            var levels = new[] { Easy };

            Assert.Throws<ArgumentNullException>(() => new GameFlow(null, levels, resolver, _bus, _echoStrategy));
            Assert.Throws<ArgumentNullException>(() => new GameFlow(_view, null, resolver, _bus, _echoStrategy));
            Assert.Throws<ArgumentNullException>(() => new GameFlow(_view, levels, null, _bus, _echoStrategy));
            Assert.Throws<ArgumentNullException>(() => new GameFlow(_view, levels, resolver, null, _echoStrategy));
            Assert.Throws<ArgumentNullException>(() => new GameFlow(_view, levels, resolver, _bus, null));
            Assert.Throws<ArgumentException>(() => new GameFlow(_view, new LevelDefinition[0], resolver, _bus, _echoStrategy));
        }
    }
}
