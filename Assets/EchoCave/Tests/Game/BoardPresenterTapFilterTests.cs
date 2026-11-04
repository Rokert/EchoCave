using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Tests
{
    // The tap filter and the tap events the tutorial uses.
    public class BoardPresenterTapFilterTests
    {
        // Two bats in a row facing right: bat 0 at (0, 0), bat 1 at (1, 0).
        private static readonly LevelDefinition Row = new LevelDefinition(
            4, 1,
            new[] { new BatDefinition(new Cell(0, 0), Direction.Right, false), new BatDefinition(new Cell(1, 0), Direction.Right, false) },
            0, 0, 2, 2);

        private FakeBoardView _view;
        private BoardPresenter _presenter;
        private readonly List<string> _log = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _view = new FakeBoardView();
            _presenter = new BoardPresenter(LevelBuilder.Build(Row), new MoveResolver(), new CommandHistory(), _view);
            _presenter.BatTapped += id => _log.Add($"tapped {id}");
            _presenter.TapRefused += () => _log.Add("refused");
            _presenter.MoveExecuted += result => _log.Add("moved");
            _presenter.Start();
        }

        private static Vector2 Center(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);

        [Test]
        public void WithoutAFilter_EveryTapOnABatGoesThrough_AndIsAnnouncedBeforeTheMove()
        {
            _presenter.HandleTap(Center(1, 0));

            CollectionAssert.AreEqual(new[] { "tapped 1", "moved" }, _log);
        }

        [Test]
        public void ARefusedTap_MovesNothing_AndIsAnnounced()
        {
            _presenter.CanTap = id => id == 1;

            _presenter.HandleTap(Center(0, 0));

            CollectionAssert.AreEqual(new[] { "refused" }, _log);
            Assert.IsEmpty(_view.Played);
            Assert.IsFalse(_presenter.IsBusy);
        }

        [Test]
        public void AnAllowedTap_GoesThrough()
        {
            _presenter.CanTap = id => id == 1;

            _presenter.HandleTap(Center(1, 0));

            CollectionAssert.AreEqual(new[] { "tapped 1", "moved" }, _log);
            Assert.AreEqual(1, _view.Played.Count);
        }

        [Test]
        public void ATapOnAnEmptyCell_IsNeitherRefusedNorAnnounced()
        {
            _presenter.CanTap = id => false;

            _presenter.HandleTap(Center(3, 0));

            Assert.IsEmpty(_log, "nothing was tapped, so nothing was refused");
        }
    }
}
