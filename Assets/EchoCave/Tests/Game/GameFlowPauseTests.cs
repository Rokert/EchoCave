using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    public class GameFlowPauseTests
    {
        // Level 0: one bat that leaves freely. Level 1: two bats in a row, limit 2 (the left one is blocked).
        // Level 2: a hidden bat and two pings, no limit.
        private static readonly LevelDefinition Easy = new LevelDefinition(
            3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 0, 1, 1);

        private static readonly LevelDefinition Tight = new LevelDefinition(
            3,
            1,
            new[]
            {
                new BatDefinition(new Cell(0, 0), Direction.Right, false),
                new BatDefinition(new Cell(1, 0), Direction.Right, false)
            },
            2, 0, 2, 2);

        private static readonly LevelDefinition Hidden = new LevelDefinition(
            3,
            1,
            new[]
            {
                new BatDefinition(new Cell(0, 0), Direction.Right, true),
                new BatDefinition(new Cell(1, 0), Direction.Right, false)
            },
            0, 2, 2, 2);

        private const float Reveal = 2f;

        private FakeBoardView _view;
        private GameFlow _flow;

        private readonly List<PauseChanged> _pauses = new List<PauseChanged>();
        private readonly List<MoveSpent> _moves = new List<MoveSpent>();
        private readonly List<PingUsed> _pingsUsed = new List<PingUsed>();
        private readonly List<PingRefused> _pingsRefused = new List<PingRefused>();
        private int _menuOpened;
        private int _expired;

        [SetUp]
        public void SetUp()
        {
            _pauses.Clear();
            _moves.Clear();
            _pingsUsed.Clear();
            _pingsRefused.Clear();
            _menuOpened = 0;
            _expired = 0;

            _view = new FakeBoardView();
            var bus = new EventBus();
            bus.Subscribe<PauseChanged>(e => _pauses.Add(e));
            bus.Subscribe<MoveSpent>(e => _moves.Add(e));
            bus.Subscribe<PingUsed>(e => _pingsUsed.Add(e));
            bus.Subscribe<PingRefused>(e => _pingsRefused.Add(e));
            bus.Subscribe<MenuOpened>(_ => _menuOpened++);
            bus.Subscribe<EchoExpired>(_ => _expired++);
            _flow = new GameFlow(_view, new[] { Easy, Tight, Hidden }, new MoveResolver(), bus, new RevealAllHiddenStrategy(Reveal));

            // Creating the flow already opens the menu once; the tests count the openings after that.
            _menuOpened = 0;
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

        // --- pause ---

        [Test]
        public void WhilePaused_TapsDoNothing_AndAfterResumeTheyWorkAgain()
        {
            StartLevel(0);
            _flow.RequestPause();

            _flow.HandleTap(Center(1, 0));

            Assert.AreEqual(0, _view.Played.Count);
            Assert.AreEqual(0, _flow.Session.MovesUsed);

            _flow.RequestResume();
            _flow.HandleTap(Center(1, 0));

            Assert.AreEqual(1, _view.Played.Count);
            Assert.AreEqual(1, _flow.Session.MovesUsed);
        }

        [Test]
        public void WhilePaused_PingsAreIgnoredSilently_AndWorkAfterResume()
        {
            StartLevel(2);
            _flow.RequestPause();

            _flow.RequestPing();

            Assert.AreEqual(0, _pingsUsed.Count);
            Assert.AreEqual(0, _pingsRefused.Count, "a ping while paused must not complain or shake anything");
            Assert.AreEqual(0, _view.RejectedCount);
            Assert.AreEqual(2, _flow.Echo.PingsLeft);

            _flow.RequestResume();
            _flow.RequestPing();

            Assert.AreEqual(1, _pingsUsed.Count);
        }

        [Test]
        public void WhilePaused_TheEchoClockStandsStill()
        {
            StartLevel(2);
            _flow.RequestPing();
            _flow.RequestPause();

            _flow.Tick(10f);
            Assert.AreEqual(0, _expired, "time must not run while paused");
            Assert.IsTrue(_flow.Echo.IsActive);

            _flow.RequestResume();
            _flow.Tick(Reveal);

            Assert.AreEqual(1, _expired);
        }

        [Test]
        public void WinningWhilePaused_WaitsForTheResume()
        {
            StartLevel(0);
            _flow.HandleTap(Center(1, 0));   // the last bat leaves; its animation is still running
            _flow.RequestPause();

            _view.FinishAnimation();
            _flow.Tick(0f);
            Assert.AreEqual(GameState.Playing, _flow.State, "the result must wait while paused");

            _flow.RequestResume();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Won, _flow.State);
        }

        [Test]
        public void OnlyALevelInPlay_CanBePaused()
        {
            _flow.RequestPause();
            Assert.IsFalse(_flow.IsPaused, "not in the menu");

            StartLevel(0);
            TapAndSettle(1, 0);
            Assert.AreEqual(GameState.Won, _flow.State);
            _flow.RequestPause();

            Assert.IsFalse(_flow.IsPaused, "not once the level is won");
            Assert.AreEqual(0, _pauses.Count);
        }

        [Test]
        public void PauseChanged_IsPublishedOnlyWhenTheStateChanges()
        {
            StartLevel(0);

            _flow.RequestPause();
            _flow.RequestPause();
            _flow.RequestResume();
            _flow.RequestResume();

            Assert.AreEqual(2, _pauses.Count);
            Assert.IsTrue(_pauses[0].IsPaused);
            Assert.IsFalse(_pauses[1].IsPaused);
        }

        [Test]
        public void Retry_EndsThePause()
        {
            StartLevel(1);
            _flow.RequestPause();

            _flow.RequestRetry();

            Assert.IsFalse(_flow.IsPaused);
            Assert.IsFalse(_pauses[_pauses.Count - 1].IsPaused);

            _flow.HandleTap(Center(1, 0));
            Assert.AreEqual(1, _flow.Session.MovesUsed, "taps must work in the fresh attempt");
        }

        [Test]
        public void TheMenu_EndsThePause()
        {
            StartLevel(0);
            _flow.RequestPause();

            _flow.RequestMenu();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Menu, _flow.State);
            Assert.IsFalse(_flow.IsPaused);
            Assert.IsFalse(_pauses[_pauses.Count - 1].IsPaused);
        }

        // --- Next ---

        [Test]
        public void Next_PressedTwiceBeforeTheTick_MovesOnlyOneLevel()
        {
            StartLevel(0);
            TapAndSettle(1, 0);
            Assert.AreEqual(GameState.Won, _flow.State);

            _flow.RequestNext();
            _flow.RequestNext();
            _flow.Tick(0f);

            Assert.AreEqual(GameState.Playing, _flow.State);
            Assert.AreEqual(1, _flow.LevelIndex, "a double tap on Next must not skip a level");
        }

        // --- MoveSpent ---

        [Test]
        public void MoveSpent_IsPublishedForEveryResolvedTap_WithTheCountLimitAndOutcome()
        {
            StartLevel(1);

            _flow.HandleTap(Center(0, 0));   // blocked: bounces
            _view.FinishAnimation();
            _flow.HandleTap(Center(1, 0));   // leaves

            Assert.AreEqual(2, _moves.Count);
            Assert.AreEqual(1, _moves[0].MovesUsed);
            Assert.AreEqual(2, _moves[0].MoveLimit);
            Assert.AreEqual(MoveOutcome.Bounced, _moves[0].Outcome);
            Assert.AreEqual(2, _moves[1].MovesUsed);
            Assert.AreEqual(MoveOutcome.Exited, _moves[1].Outcome);
        }

        [Test]
        public void MoveSpent_IsNotPublished_ForTapsThatSpendNothing()
        {
            StartLevel(1);

            _flow.HandleTap(Center(2, 0));   // an empty cell
            _flow.HandleTap(Center(9, 9));   // off the board

            Assert.AreEqual(0, _moves.Count);
        }

        [Test]
        public void MoveSpent_CountsFromTheNewAttempt_AfterARetry()
        {
            StartLevel(1);
            TapAndSettle(0, 0);
            Assert.AreEqual(1, _moves[0].MovesUsed);

            _flow.RequestRetry();
            _flow.HandleTap(Center(0, 0));

            Assert.AreEqual(2, _moves.Count);
            Assert.AreEqual(1, _moves[1].MovesUsed, "the counter restarts with the attempt");
        }

        // --- MenuOpened ---

        [Test]
        public void MenuOpened_IsPublished_WhenTheFlowReturnsToTheMenu()
        {
            StartLevel(0);
            Assert.AreEqual(0, _menuOpened);

            _flow.RequestMenu();
            _flow.Tick(0f);

            Assert.AreEqual(1, _menuOpened);
        }
    }
}
