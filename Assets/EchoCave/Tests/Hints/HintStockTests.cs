using System;
using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class HintStockTests
    {
        private EventBus _bus;
        private FakeSaveService _save;
        private readonly List<int> _changes = new List<int>();

        [SetUp]
        public void SetUp()
        {
            _changes.Clear();
            _bus = new EventBus();
            _bus.Subscribe<HintsChanged>(e => _changes.Add(e.Count));
            _save = new FakeSaveService(3);
        }

        private HintStock Stock(int max = 5) => new HintStock(_save, max, _bus);

        [Test]
        public void TheCount_IsWhatTheSaveHolds()
        {
            Assert.AreEqual(3, Stock().Count);

            _save.SetHints(1);
            Assert.AreEqual(1, Stock().Count);
        }

        [Test]
        public void Spending_TakesOne_SavesIt_AndSaysSo()
        {
            HintStock stock = Stock();

            Assert.IsTrue(stock.TrySpend());

            Assert.AreEqual(2, stock.Count);
            Assert.AreEqual(2, _save.Hints);
            CollectionAssert.AreEqual(new[] { 2 }, _changes);
        }

        [Test]
        public void WithNoHints_SpendingFails_AndNothingChanges()
        {
            _save.SetHints(0);
            _save.HintWrites = 0;
            HintStock stock = Stock();

            Assert.IsFalse(stock.TrySpend());

            Assert.AreEqual(0, stock.Count);
            Assert.AreEqual(0, _save.HintWrites);
            Assert.IsEmpty(_changes);
        }

        [Test]
        public void Granting_AddsHints_UpToTheCap()
        {
            HintStock stock = Stock(max: 5);

            Assert.AreEqual(2, stock.Grant(2));
            Assert.AreEqual(5, stock.Count);
            CollectionAssert.AreEqual(new[] { 5 }, _changes);
        }

        [Test]
        public void GrantingPastTheCap_AddsOnlyWhatFits()
        {
            HintStock stock = Stock(max: 5);

            Assert.AreEqual(2, stock.Grant(10), "3 held, a cap of 5: only 2 fit");
            Assert.AreEqual(5, stock.Count);
        }

        [Test]
        public void GrantingWhenFull_AddsNothing_AndSaysNothing()
        {
            _save.SetHints(5);
            HintStock stock = Stock(max: 5);

            Assert.AreEqual(0, stock.Grant(1));

            Assert.AreEqual(5, stock.Count);
            Assert.IsEmpty(_changes);
        }

        [Test]
        public void Spending_ThenGranting_WorksFromTheSavedCount()
        {
            HintStock stock = Stock(max: 5);
            stock.TrySpend();
            stock.TrySpend();
            stock.Grant(1);

            Assert.AreEqual(2, stock.Count);
            CollectionAssert.AreEqual(new[] { 2, 1, 2 }, _changes);
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new HintStock(null, 5, _bus));
            Assert.Throws<ArgumentNullException>(() => new HintStock(_save, 5, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HintStock(_save, -1, _bus));
            Assert.Throws<ArgumentOutOfRangeException>(() => Stock().Grant(0));
        }

        [Test]
        public void TheNoStock_NeverHasHints()
        {
            var none = new NoHintStock();

            Assert.AreEqual(0, none.Count);
            Assert.IsFalse(none.TrySpend());
            Assert.AreEqual(0, none.Grant(3));
        }
    }
}
