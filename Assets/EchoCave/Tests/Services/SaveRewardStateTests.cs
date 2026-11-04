using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    // Save version 5: the reserve hints and the state of the hint rewards.
    public class SaveRewardStateTests
    {
        private const int StartingHints = 3;

        [Test]
        public void TheReserveAndTheRewardState_SurviveARestart()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            service.SetReserveHints(1);
            service.SetRewardState("streak", 2);
            service.SetRewardState("dailyDay", 2500);

            var restarted = new SaveService(store, StartingHints);

            Assert.AreEqual(1, restarted.ReserveHints);
            Assert.AreEqual(2, restarted.GetRewardState("streak"));
            Assert.AreEqual(2500, restarted.GetRewardState("dailyDay"));
        }

        [Test]
        public void ARewardNeverSaved_ReadsAsZero()
        {
            var service = new SaveService(new InMemorySaveStore(), StartingHints);

            Assert.AreEqual(0, service.GetRewardState("streak"));
            Assert.AreEqual(0, service.ReserveHints);
        }

        [Test]
        public void SettingTheSameValueAgain_WritesNothing()
        {
            var store = new CountingStore();
            var service = new SaveService(store, StartingHints);
            service.SetRewardState("streak", 2);
            service.SetReserveHints(1);
            int writes = store.Writes;

            service.SetRewardState("streak", 2);
            service.SetReserveHints(1);

            Assert.AreEqual(writes, store.Writes);
        }

        [Test]
        public void ResettingTheProgress_ClearsTheReserveAndTheRewardState()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            service.SetReserveHints(1);
            service.SetRewardState("streak", 2);

            service.Reset();

            var restarted = new SaveService(store, StartingHints);
            Assert.AreEqual(0, restarted.ReserveHints);
            Assert.AreEqual(0, restarted.GetRewardState("streak"));
            Assert.AreEqual(StartingHints, restarted.Hints);
        }

        [Test]
        public void AVersion4Save_IsRead_WithNoReserveAndNoRewardState_AndItsHintsKept()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":4,\"hints\":2,\"levels\":[{\"id\":1,\"stars\":3,\"moves\":4}]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.RecoveredFromBadData);
            Assert.AreEqual(2, service.Hints);
            Assert.AreEqual(0, service.ReserveHints);
            Assert.AreEqual(0, service.GetRewardState("dailyDay"));
            Assert.AreEqual(3, service.Progress.StarsOf(1));
        }

        [Test]
        public void ANegativeReserve_AndBlankKeys_AreIgnored()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":5,\"hints\":2,\"reserveHints\":-4,\"rewardState\":["
                + "{\"key\":\"\",\"value\":9},{\"key\":\"streak\",\"value\":1}],\"levels\":[]}");

            var service = new SaveService(store, StartingHints);

            Assert.AreEqual(0, service.ReserveHints);
            Assert.AreEqual(1, service.GetRewardState("streak"));
        }

        [Test]
        public void TheSerializer_RoundTripsTheNewFields_AndWritesKeysInOrder()
        {
            var state = new Dictionary<string, int> { { "streak", 2 }, { "dailyDay", 2500 } };

            string json = SaveSerializer.Serialize(new PlayerProgress(), true, true, 4, 1, state);

            Assert.Less(json.IndexOf("dailyDay"), json.IndexOf("streak"), "keys are written sorted, so a save is stable text");
            Assert.IsTrue(SaveSerializer.TryDeserialize(json, StartingHints, out SaveContent back));
            Assert.AreEqual(4, back.Hints);
            Assert.AreEqual(1, back.ReserveHints);
            Assert.AreEqual(2, back.RewardState["streak"]);
            Assert.AreEqual(2500, back.RewardState["dailyDay"]);
        }

        private sealed class CountingStore : ISaveStore
        {
            public int Writes;
            private string _text;

            public bool TryRead(out string text)
            {
                text = _text;
                return _text != null;
            }

            public void Write(string text)
            {
                _text = text;
                Writes++;
            }

            public void Delete() => _text = null;
        }
    }
}
