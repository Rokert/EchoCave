using System;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class SaveServiceTests
    {
        private const int StartingHints = 3;

        // Counts writes so a test can tell "saved" from "did nothing".
        private sealed class CountingStore : ISaveStore
        {
            private readonly InMemorySaveStore _inner = new InMemorySaveStore();
            public int Writes { get; private set; }
            public int Deletes { get; private set; }

            public bool TryRead(out string content) => _inner.TryRead(out content);

            public void Write(string content)
            {
                Writes++;
                _inner.Write(content);
            }

            public void Delete()
            {
                Deletes++;
                _inner.Delete();
            }
        }

        [Test]
        public void AnEmptyStore_StartsFresh_WithoutComplaint()
        {
            var service = new SaveService(new InMemorySaveStore(), StartingHints);

            Assert.AreEqual(0, service.Progress.CompletedCount);
            Assert.IsFalse(service.RecoveredFromBadData);
        }

        [Test]
        public void Progress_SurvivesAReload_FromTheSameStore()
        {
            var store = new InMemorySaveStore();
            var first = new SaveService(store, StartingHints);
            first.RecordWin(1, 6, 3);
            first.RecordWin(4, 11, 1);
            first.RecordWin(1, 5, 3);

            var second = new SaveService(store, StartingHints);

            Assert.AreEqual(2, second.Progress.CompletedCount);
            Assert.AreEqual(3, second.Progress.StarsOf(1));
            Assert.AreEqual(5, second.Progress.BestMovesOf(1));
            Assert.AreEqual(1, second.Progress.StarsOf(4));
            Assert.AreEqual(11, second.Progress.BestMovesOf(4));
            Assert.IsFalse(second.RecoveredFromBadData);
        }

        [Test]
        public void ASaveIsWritten_OnlyWhenAWinImprovesSomething()
        {
            var store = new CountingStore();
            var service = new SaveService(store, StartingHints);

            service.RecordWin(1, 6, 3);
            service.RecordWin(1, 9, 1);

            Assert.AreEqual(1, store.Writes);
        }

        [Test]
        public void Reset_ForgetsTheProgress_ButKeepsTheAudioSwitches()
        {
            var store = new CountingStore();
            var service = new SaveService(store, StartingHints);
            service.RecordWin(1, 6, 3);
            service.SetAudio(AudioChannel.Sound, false);

            service.Reset();

            Assert.AreEqual(0, service.Progress.CompletedCount);
            Assert.IsFalse(service.SoundOn);

            var reloaded = new SaveService(store, StartingHints);
            Assert.AreEqual(0, reloaded.Progress.CompletedCount);
            Assert.IsFalse(reloaded.SoundOn, "a preference is not an achievement: Reset must not undo it");
        }

        [TestCase("not json at all")]
        [TestCase("{\"version\":99,\"levels\":[]}")]
        [TestCase("{\"levels\":[{\"id\":1,\"stars\":3,\"moves\":4}]}")]
        [TestCase("{\"version\":0,\"levels\":[]}")]
        public void ASaveTheGameCannotRead_StartsFresh_AndSaysSo(string content)
        {
            var store = new InMemorySaveStore();
            store.Write(content);

            var service = new SaveService(store, StartingHints);

            Assert.AreEqual(0, service.Progress.CompletedCount);
            Assert.IsTrue(service.RecoveredFromBadData);
        }

        [Test]
        public void EntriesThatMakeNoSense_AreSkipped_AndTheRestIsKept()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":2,\"levels\":["
                + "{\"id\":1,\"stars\":3,\"moves\":4},"
                + "{\"id\":2,\"stars\":9,\"moves\":4},"
                + "{\"id\":0,\"stars\":2,\"moves\":4},"
                + "{\"id\":3,\"stars\":2,\"moves\":7}]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.RecoveredFromBadData);
            Assert.AreEqual(2, service.Progress.CompletedCount);
            Assert.AreEqual(3, service.Progress.StarsOf(1));
            Assert.AreEqual(2, service.Progress.StarsOf(3));
            Assert.IsFalse(service.Progress.IsCompleted(2));
        }

        [Test]
        public void TheSerializer_RoundTripsEveryRecord()
        {
            var progress = new PlayerProgress();
            progress.RecordWin(1, 3, 6);
            progress.RecordWin(8, 1, 14);

            Assert.IsTrue(SaveSerializer.TryDeserialize(SaveSerializer.Serialize(progress, true, false, 2), StartingHints, out SaveContent back));

            Assert.AreEqual(2, back.Progress.CompletedCount);
            Assert.AreEqual(3, back.Progress.StarsOf(1));
            Assert.AreEqual(6, back.Progress.BestMovesOf(1));
            Assert.AreEqual(1, back.Progress.StarsOf(8));
            Assert.AreEqual(14, back.Progress.BestMovesOf(8));
            Assert.IsTrue(back.SoundOn);
            Assert.IsFalse(back.MusicOn);
            Assert.AreEqual(2, back.Hints);
        }

        [Test]
        public void AVersion1Save_IsConverted_BecauseTheFirstTenIdsFollowedCatalogOrder()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":1,\"levels\":["
                + "{\"index\":0,\"stars\":3,\"moves\":4},"
                + "{\"index\":2,\"stars\":2,\"moves\":7},"
                + "{\"index\":-1,\"stars\":2,\"moves\":7}]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.RecoveredFromBadData);
            Assert.AreEqual(2, service.Progress.CompletedCount);
            Assert.AreEqual(3, service.Progress.StarsOf(1));
            Assert.AreEqual(4, service.Progress.BestMovesOf(1));
            Assert.AreEqual(2, service.Progress.StarsOf(3));
            Assert.AreEqual(7, service.Progress.BestMovesOf(3));
        }

        [Test]
        public void AfterTheNextWin_AConvertedSaveIsStoredAsTheCurrentVersion()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":1,\"levels\":[{\"index\":0,\"stars\":1,\"moves\":9}]}");
            var service = new SaveService(store, StartingHints);

            service.RecordWin(1, 5, 3);

            store.TryRead(out string stored);
            StringAssert.Contains("\"version\":6", stored);
            Assert.AreEqual(3, new SaveService(store, StartingHints).Progress.StarsOf(1));
        }

        [Test]
        public void BothSwitches_StartOn_WhenThereIsNoSave()
        {
            var service = new SaveService(new InMemorySaveStore(), StartingHints);

            Assert.IsTrue(service.SoundOn);
            Assert.IsTrue(service.MusicOn);
        }

        [Test]
        public void ASwitchTurnedOff_SurvivesAReload_AndTheOtherStaysOn()
        {
            var store = new InMemorySaveStore();
            new SaveService(store, StartingHints).SetAudio(AudioChannel.Music, false);

            var reloaded = new SaveService(store, StartingHints);

            Assert.IsFalse(reloaded.MusicOn);
            Assert.IsTrue(reloaded.SoundOn);
        }

        [Test]
        public void ASwitchTurnedBackOn_IsSavedToo()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            service.SetAudio(AudioChannel.Sound, false);
            service.SetAudio(AudioChannel.Sound, true);

            Assert.IsTrue(new SaveService(store, StartingHints).SoundOn);
        }

        [Test]
        public void ASwitchSetToWhatItAlreadyIs_WritesNothing()
        {
            var store = new CountingStore();
            var service = new SaveService(store, StartingHints);

            service.SetAudio(AudioChannel.Sound, true);
            service.SetAudio(AudioChannel.Music, true);

            Assert.AreEqual(0, store.Writes);

            service.SetAudio(AudioChannel.Sound, false);
            service.SetAudio(AudioChannel.Sound, false);

            Assert.AreEqual(1, store.Writes);
        }

        [Test]
        public void AVersion2Save_LoadsWithBothSwitchesOn_AndKeepsItsProgress()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":2,\"levels\":[{\"id\":4,\"stars\":2,\"moves\":9}]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.RecoveredFromBadData);
            Assert.IsTrue(service.SoundOn, "a save made before the switches existed must not come back muted");
            Assert.IsTrue(service.MusicOn);
            Assert.AreEqual(2, service.Progress.StarsOf(4));
        }

        [Test]
        public void AVersion3Save_ReadsBothSwitches()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":3,\"soundOff\":true,\"musicOff\":false,\"levels\":[]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.SoundOn);
            Assert.IsTrue(service.MusicOn);
        }

        [Test]
        public void AVersion1Save_LoadsWithBothSwitchesOn()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":1,\"levels\":[{\"index\":0,\"stars\":3,\"moves\":4}]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsTrue(service.SoundOn);
            Assert.IsTrue(service.MusicOn);
        }

        [Test]
        public void ASaveTheGameCannotRead_StartsWithBothSwitchesOn()
        {
            var store = new InMemorySaveStore();
            store.Write("not json at all");

            var service = new SaveService(store, StartingHints);

            Assert.IsTrue(service.RecoveredFromBadData);
            Assert.IsTrue(service.SoundOn);
            Assert.IsTrue(service.MusicOn);
        }

        [Test]
        public void ANewPlayer_StartsWithTheStartingHints()
        {
            Assert.AreEqual(StartingHints, new SaveService(new InMemorySaveStore(), StartingHints).Hints);
        }

        [Test]
        public void TheHintCount_SurvivesAReload()
        {
            var store = new InMemorySaveStore();
            new SaveService(store, StartingHints).SetHints(1);

            Assert.AreEqual(1, new SaveService(store, StartingHints).Hints);
        }

        [Test]
        public void AHintCountOfZero_IsSaved_NotMistakenForMissing()
        {
            var store = new InMemorySaveStore();
            new SaveService(store, StartingHints).SetHints(0);

            Assert.AreEqual(0, new SaveService(store, StartingHints).Hints, "zero hints must not come back as the starting count");
        }

        [Test]
        public void SettingTheHintsToWhatTheyAlreadyAre_WritesNothing()
        {
            var store = new CountingStore();
            var service = new SaveService(store, StartingHints);

            service.SetHints(StartingHints);
            Assert.AreEqual(0, store.Writes);

            service.SetHints(1);
            service.SetHints(1);
            Assert.AreEqual(1, store.Writes);
        }

        [Test]
        public void ANegativeHintCount_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SaveService(new InMemorySaveStore(), StartingHints).SetHints(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SaveService(new InMemorySaveStore(), -1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ASaveFromBeforeHintsExisted_GetsTheStartingHints(int version)
        {
            var store = new InMemorySaveStore();
            string levels = version == 1
                ? "[{\"index\":0,\"stars\":3,\"moves\":4}]"
                : "[{\"id\":1,\"stars\":3,\"moves\":4}]";
            store.Write("{\"version\":" + version + ",\"levels\":" + levels + "}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.RecoveredFromBadData);
            Assert.AreEqual(StartingHints, service.Hints);
            Assert.AreEqual(3, service.Progress.StarsOf(1));
        }

        [Test]
        public void AVersion4Save_ReadsItsHintCount_AndIgnoresANegativeOne()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":4,\"hints\":2,\"levels\":[]}");
            Assert.AreEqual(2, new SaveService(store, StartingHints).Hints);

            store.Write("{\"version\":4,\"hints\":-7,\"levels\":[]}");
            Assert.AreEqual(0, new SaveService(store, StartingHints).Hints);
        }

        [Test]
        public void Reset_GivesBackTheStartingHints()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            service.SetHints(0);

            service.Reset();

            Assert.AreEqual(StartingHints, service.Hints);
            Assert.AreEqual(StartingHints, new SaveService(store, StartingHints).Hints);
        }

        [Test]
        public void ASaveTheGameCannotRead_StartsWithTheStartingHints()
        {
            var store = new InMemorySaveStore();
            store.Write("garbage");

            Assert.AreEqual(StartingHints, new SaveService(store, StartingHints).Hints);
        }

        [Test]
        public void ANullStore_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveService(null, StartingHints));
        }
    }
}
