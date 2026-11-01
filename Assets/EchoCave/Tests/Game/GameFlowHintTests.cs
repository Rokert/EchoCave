using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    public class GameFlowHintTests
    {
        // Row of four cells. Bats 0 and 2 are hidden, bat 1 is not. Only the right-most bat (2) can leave now:
        // bat 1 waits for it and bat 0 waits for bat 1. Two pings, no move limit.
        private static readonly LevelDefinition Hidden = new LevelDefinition(
            4,
            1,
            new[]
            {
                new BatDefinition(new Cell(0, 0), Direction.Right, true),
                new BatDefinition(new Cell(1, 0), Direction.Right, false),
                new BatDefinition(new Cell(3, 0), Direction.Right, true)
            },
            0, 2, 3, 3);

        // One visible bat that leaves freely.
        private static readonly LevelDefinition Visible = new LevelDefinition(
            3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, false) }, 0, 2, 1, 1);

        private const float EchoSeconds = 2f;

        private FakeBoardView _view;
        private FakeSaveService _save;
        private GameFlow _flow;

        private readonly List<HintUsed> _used = new List<HintUsed>();
        private readonly List<HintRefused> _refused = new List<HintRefused>();

        [SetUp]
        public void SetUp()
        {
            _used.Clear();
            _refused.Clear();

            _view = new FakeBoardView();
            _save = new FakeSaveService(3);
            var bus = new EventBus();
            bus.Subscribe<HintUsed>(e => _used.Add(e));
            bus.Subscribe<HintRefused>(e => _refused.Add(e));

            var stock = new HintStock(_save, 5, bus);
            _flow = new GameFlow(_view, new[] { Hidden, Visible }, new MoveResolver(), bus, new RevealAllHiddenStrategy(EchoSeconds), stock);
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
        public void AHint_PointsAtTheBatThatCanLeave_SpendsOne_AndCostsNoMove()
        {
            StartLevel(1);

            _flow.RequestHint();

            CollectionAssert.AreEqual(new[] { 0 }, _view.HintedBatIds);
            Assert.AreEqual(2, _save.Hints);
            Assert.AreEqual(1, _used.Count);
            Assert.AreEqual(2, _used[0].HintsLeft);
            Assert.AreEqual(0, _used[0].BatId);
            Assert.AreEqual(0, _flow.Session.MovesUsed);
            Assert.AreEqual(0, _refused.Count);
            Assert.AreEqual(0, _view.RejectedCount);
        }

        [Test]
        public void AHintOnAVisibleBat_RevealsNothing()
        {
            StartLevel(1);

            _flow.RequestHint();

            Assert.IsEmpty(_view.Revealed);
        }

        [Test]
        public void AHintOnAHiddenBat_RevealsItsArrow()
        {
            StartLevel(0);

            _flow.RequestHint();

            CollectionAssert.AreEqual(new[] { 2 }, _view.HintedBatIds);
            CollectionAssert.AreEquivalent(new[] { 2 }, _view.Revealed, "the hint must never be a move the player cannot read");
        }

        [Test]
        public void AHintedHiddenBat_StaysRevealed_AfterAnEchoRunsOut()
        {
            StartLevel(0);
            _flow.RequestHint();

            _flow.RequestPing();
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, _view.Revealed);

            _flow.Tick(EchoSeconds + 0.1f);

            CollectionAssert.AreEquivalent(new[] { 2 }, _view.Revealed, "only the hinted bat outlives the echo");
        }

        [Test]
        public void AfterABatLeaves_TheNextHintFollowsTheBoardAsItIsNow()
        {
            StartLevel(0);
            TapAndSettle(3, 0);

            _flow.RequestHint();

            CollectionAssert.AreEqual(new[] { 1 }, _view.HintedBatIds, "bat 1 was blocked by bat 2 and is free now");
            Assert.AreEqual(1, _flow.Session.MovesUsed, "the tap cost its move, the hint did not");
        }

        [Test]
        public void WithNoHintsLeft_TheHintIsRefused_WithFeedback_AndCostsNothing()
        {
            _save.SetHints(0);
            StartLevel(1);

            _flow.RequestHint();

            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(HintRefusal.NoHintsLeft, _refused[0].Reason);
            Assert.AreEqual(1, _view.RejectedCount, "a refusal must be seen: the bats shake");
            Assert.IsEmpty(_view.HintedBatIds);
            Assert.IsEmpty(_used);
            Assert.AreEqual(0, _save.Hints);
        }

        [Test]
        public void WhileAMoveAnimates_TheHintIsRefused_AndWorksOnceItEnds()
        {
            StartLevel(0);
            _flow.HandleTap(Center(3, 0));

            _flow.RequestHint();

            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(HintRefusal.Busy, _refused[0].Reason);
            Assert.AreEqual(3, _save.Hints, "a refused hint is not spent");
            Assert.IsEmpty(_view.HintedBatIds);

            _view.FinishAnimation();
            _flow.Tick(0f);
            _flow.RequestHint();

            CollectionAssert.AreEqual(new[] { 1 }, _view.HintedBatIds);
            Assert.AreEqual(2, _save.Hints);
        }

        [Test]
        public void WhilePaused_TheHintDoesNothing_NotEvenARefusal()
        {
            StartLevel(1);
            _flow.RequestPause();

            _flow.RequestHint();

            Assert.IsEmpty(_used);
            Assert.IsEmpty(_refused);
            Assert.AreEqual(0, _view.RejectedCount);
            Assert.AreEqual(3, _save.Hints);
        }

        [Test]
        public void InTheMenu_TheHintDoesNothing()
        {
            _flow.RequestHint();

            Assert.IsEmpty(_used);
            Assert.IsEmpty(_refused);
            Assert.AreEqual(3, _save.Hints);
        }

        [Test]
        public void EachHint_SpendsOne_UntilNoneAreLeft()
        {
            StartLevel(1);

            _flow.RequestHint();
            _flow.RequestHint();
            _flow.RequestHint();
            _flow.RequestHint();

            Assert.AreEqual(3, _used.Count);
            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(HintRefusal.NoHintsLeft, _refused[0].Reason);
            Assert.AreEqual(0, _save.Hints);
        }

        [Test]
        public void ThePingAndTheHint_AreIndependent()
        {
            StartLevel(0);

            _flow.RequestHint();
            _flow.RequestPing();

            Assert.AreEqual(1, _flow.Echo.PingsLeft);
            Assert.AreEqual(2, _save.Hints);
        }

        [Test]
        public void WithoutAStock_NoHintIsEverGiven()
        {
            var flow = new GameFlow(_view, new[] { Visible }, new MoveResolver(), new EventBus(), new RevealAllHiddenStrategy(EchoSeconds));
            flow.RequestPlay(0);
            flow.Tick(0f);

            flow.RequestHint();

            Assert.IsEmpty(_view.HintedBatIds);
        }
    }
}
