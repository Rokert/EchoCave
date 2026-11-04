using System;
using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class HintStockReserveTests
    {
        private EventBus _bus;
        private FakeSaveService _save;
        private readonly List<int> _stockChanges = new List<int>();
        private readonly List<int> _reserveChanges = new List<int>();

        [SetUp]
        public void SetUp()
        {
            _stockChanges.Clear();
            _reserveChanges.Clear();
            _bus = new EventBus();
            _bus.Subscribe<HintsChanged>(e => _stockChanges.Add(e.Count));
            _bus.Subscribe<HintReserveChanged>(e => _reserveChanges.Add(e.Count));
            _save = new FakeSaveService(3);
        }

        private HintStock Stock(int max = 5, int reserve = 1) => new HintStock(_save, max, _bus, reserve);

        [Test]
        public void AFreeHint_GoesToTheStock_WhileThereIsRoom()
        {
            HintGrant grant = Stock().GrantFree(1);

            Assert.AreEqual(1, grant.ToStock);
            Assert.AreEqual(0, grant.ToReserve);
            Assert.AreEqual(4, _save.Hints);
            Assert.AreEqual(0, _save.ReserveHints);
            CollectionAssert.AreEqual(new[] { 4 }, _stockChanges);
            Assert.IsEmpty(_reserveChanges);
        }

        [Test]
        public void WithAFullStock_TheFreeHintGoesToTheReserve()
        {
            _save.SetHints(5);

            HintGrant grant = Stock().GrantFree(1);

            Assert.AreEqual(0, grant.ToStock);
            Assert.AreEqual(1, grant.ToReserve);
            Assert.AreEqual(5, _save.Hints);
            Assert.AreEqual(1, _save.ReserveHints);
            CollectionAssert.AreEqual(new[] { 1 }, _reserveChanges);
            Assert.IsEmpty(_stockChanges);
        }

        [Test]
        public void WithAFullStockAndAFullReserve_TheFreeHintIsLost_AndNothingIsSaid()
        {
            _save.SetHints(5);
            _save.SetReserveHints(1);

            HintGrant grant = Stock().GrantFree(1);

            Assert.AreEqual(0, grant.Granted);
            Assert.AreEqual(1, _save.ReserveHints);
            Assert.IsEmpty(_stockChanges);
            Assert.IsEmpty(_reserveChanges);
        }

        [Test]
        public void AGrantThatDoesNotFitEntirely_SplitsBetweenStockAndReserve_AndLosesTheRest()
        {
            _save.SetHints(4);

            HintGrant grant = Stock(reserve: 1).GrantFree(5);

            Assert.AreEqual(1, grant.ToStock);
            Assert.AreEqual(1, grant.ToReserve, "the reserve holds one spare, the other three are lost");
            Assert.AreEqual(5, _save.Hints);
            Assert.AreEqual(1, _save.ReserveHints);
        }

        [Test]
        public void SpendingWithASpareInTheReserve_KeepsTheCount_AndEmptiesTheReserve()
        {
            _save.SetHints(5);
            _save.SetReserveHints(1);
            HintStock stock = Stock();

            Assert.IsTrue(stock.TrySpend());

            Assert.AreEqual(5, stock.Count, "the spare took the place of the spent hint");
            Assert.AreEqual(0, stock.ReserveCount);
            CollectionAssert.AreEqual(new[] { 0 }, _reserveChanges);
            Assert.IsEmpty(_stockChanges, "the count the player sees did not move");
        }

        [Test]
        public void SpendingAgain_AfterTheReserveIsUsed_LowersTheStock()
        {
            _save.SetHints(5);
            _save.SetReserveHints(1);
            HintStock stock = Stock();

            stock.TrySpend();
            stock.TrySpend();

            Assert.AreEqual(4, stock.Count);
            Assert.AreEqual(0, stock.ReserveCount);
        }

        [Test]
        public void WithTheReserveTurnedOff_AFreeHintBeyondTheCap_IsLost()
        {
            _save.SetHints(5);

            HintGrant grant = Stock(reserve: 0).GrantFree(1);

            Assert.AreEqual(0, grant.Granted);
            Assert.AreEqual(0, _save.ReserveHints);
        }

        [Test]
        public void TheAdGrant_NeverTouchesTheReserve()
        {
            _save.SetHints(5);

            Assert.AreEqual(0, Stock().Grant(1));

            Assert.AreEqual(0, _save.ReserveHints);
        }

        [Test]
        public void TheStock_IsFull_OnlyWhenNeitherTheStockNorTheReserveHasRoom()
        {
            _save.SetHints(5);
            Assert.IsFalse(Stock().IsFull, "the reserve is still empty");

            _save.SetReserveHints(1);
            Assert.IsTrue(Stock().IsFull);

            _save.SetHints(4);
            Assert.IsFalse(Stock().IsFull);
        }

        [Test]
        public void WithTheReserveTurnedOff_TheStock_IsFullAtItsCap()
        {
            _save.SetHints(5);

            Assert.IsTrue(Stock(reserve: 0).IsFull);
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Stock().GrantFree(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HintStock(_save, 5, _bus, -1));
        }

        [Test]
        public void TheNoStock_HasNoReserveAndKeepsNothing()
        {
            var none = new NoHintStock();

            Assert.AreEqual(0, none.ReserveCount);
            Assert.AreEqual(0, none.GrantFree(2).Granted);
        }
    }
}
