using System;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    public class BoardPresenterTests
    {
        private LevelSession _session;
        private FakeBoardView _view;
        private BoardPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            var board = new Board(5, 5);
            board.Add(new Bat(1, new Cell(0, 0), Direction.Up));       // clear path up
            board.Add(new Bat(2, new Cell(2, 2), Direction.Right));    // blocked by 3
            board.Add(new Bat(3, new Cell(4, 2), Direction.Up));       // clear path up
            _session = new LevelSession(board);
            _view = new FakeBoardView();
            _presenter = new BoardPresenter(_session, new MoveResolver(), new CommandHistory(), _view);
            _presenter.Start();
        }

        private static Vector2 Center(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);

        [Test]
        public void Start_BuildsTheViewFromTheSessionBoard()
        {
            Assert.AreSame(_session.Board, _view.Built);
            Assert.AreEqual(1, _view.BuildCount);
        }

        [Test]
        public void TapOnFreeBat_PlaysAnExit_AndRemovesItFromTheBoard()
        {
            _presenter.HandleTap(Center(0, 0));

            Assert.AreEqual(1, _view.Played.Count);
            Assert.AreEqual(MoveOutcome.Exited, _view.Played[0].Outcome);
            Assert.AreEqual(1, _view.Played[0].Bat.Id);
            Assert.IsFalse(_session.Board.TryGetBat(1, out _));
            Assert.AreEqual(1, _session.MovesUsed);
        }

        [Test]
        public void TapOnBlockedBat_PlaysABounce_AndKeepsTheBoard()
        {
            _presenter.HandleTap(Center(2, 2));

            Assert.AreEqual(1, _view.Played.Count);
            Assert.AreEqual(MoveOutcome.Bounced, _view.Played[0].Outcome);
            Assert.AreEqual(3, _view.Played[0].BlockingBat.Id);
            Assert.AreEqual(3, _session.Board.Count);
            Assert.AreEqual(1, _session.MovesUsed);
        }

        [Test]
        public void TapOnEmptyCell_DoesNothing()
        {
            _presenter.HandleTap(Center(1, 3));

            Assert.AreEqual(0, _view.Played.Count);
            Assert.AreEqual(0, _session.MovesUsed);
            Assert.IsFalse(_presenter.IsBusy);
        }

        [Test]
        public void TapOutsideTheBoard_DoesNothing()
        {
            _presenter.HandleTap(new Vector2(-3f, 9f));

            Assert.AreEqual(0, _view.Played.Count);
            Assert.AreEqual(0, _session.MovesUsed);
        }

        // The point above lands on no bat even if the miss were ignored, so it cannot tell. This one can:
        // the view reports a miss but the cell it hands back holds bat 1.
        [Test]
        public void ViewReportsAMiss_TheCellItReturnsIsNotUsed()
        {
            _view.ForceMiss = true;

            _presenter.HandleTap(Center(0, 0));

            Assert.AreEqual(0, _view.Played.Count);
            Assert.IsTrue(_session.Board.TryGetBat(1, out _));
            Assert.AreEqual(0, _session.MovesUsed);
        }

        [Test]
        public void TapWhileAnimating_IsIgnored()
        {
            _presenter.HandleTap(Center(0, 0));

            _presenter.HandleTap(Center(4, 2));

            Assert.IsTrue(_presenter.IsBusy);
            Assert.AreEqual(1, _view.Played.Count);
            Assert.AreEqual(1, _session.MovesUsed);
            Assert.IsTrue(_session.Board.TryGetBat(3, out _));
        }

        [Test]
        public void TapAfterTheAnimationEnds_IsAccepted()
        {
            _presenter.HandleTap(Center(0, 0));
            _view.FinishAnimation();

            _presenter.HandleTap(Center(4, 2));

            Assert.IsFalse(_session.Board.TryGetBat(3, out _));
            Assert.AreEqual(2, _view.Played.Count);
            Assert.AreEqual(2, _session.MovesUsed);
        }

        [Test]
        public void TapOnABatThatAlreadyLeft_DoesNothing()
        {
            _presenter.HandleTap(Center(0, 0));
            _view.FinishAnimation();

            _presenter.HandleTap(Center(0, 0));

            Assert.AreEqual(1, _view.Played.Count);
            Assert.AreEqual(1, _session.MovesUsed);
        }

        [Test]
        public void Start_ClearsBusy_BecauseARebuildNeverCallsBack()
        {
            _presenter.HandleTap(Center(0, 0));
            Assert.IsTrue(_presenter.IsBusy);

            _presenter.Start();

            Assert.IsFalse(_presenter.IsBusy);
            Assert.AreEqual(2, _view.BuildCount);
        }

        [Test]
        public void Constructor_RejectsNulls()
        {
            var resolver = new MoveResolver();
            var history = new CommandHistory();

            Assert.Throws<ArgumentNullException>(() => new BoardPresenter(null, resolver, history, _view));
            Assert.Throws<ArgumentNullException>(() => new BoardPresenter(_session, null, history, _view));
            Assert.Throws<ArgumentNullException>(() => new BoardPresenter(_session, resolver, null, _view));
            Assert.Throws<ArgumentNullException>(() => new BoardPresenter(_session, resolver, history, null));
        }
    }
}
