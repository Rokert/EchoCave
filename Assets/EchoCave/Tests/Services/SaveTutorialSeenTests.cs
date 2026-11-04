using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    // Save version 6: the tutorial-seen flag.
    public class SaveTutorialSeenTests
    {
        private const int StartingHints = 3;

        [Test]
        public void ANewSave_HasNotSeenTheTutorial()
        {
            Assert.IsFalse(new SaveService(new InMemorySaveStore(), StartingHints).TutorialSeen);
        }

        [Test]
        public void TheFlag_SurvivesARestart()
        {
            var store = new InMemorySaveStore();
            new SaveService(store, StartingHints).SetTutorialSeen(true);

            Assert.IsTrue(new SaveService(store, StartingHints).TutorialSeen);
        }

        [Test]
        public void SettingTheSameValueAgain_WritesNothing()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            service.SetTutorialSeen(true);
            store.TryRead(out string before);

            service.SetTutorialSeen(true);

            store.TryRead(out string after);
            Assert.AreEqual(before, after);
        }

        [Test]
        public void ASaveFromBeforeTheTutorial_ReadsAsNotSeen_SoPlayersWithProgressSeeItOnce()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":5,\"hints\":2,\"levels\":[{\"id\":1,\"stars\":3,\"moves\":4}]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.RecoveredFromBadData);
            Assert.IsFalse(service.TutorialSeen);
            Assert.AreEqual(3, service.Progress.StarsOf(1), "their progress is untouched");
        }

        [Test]
        public void AFlagInAnOlderVersion_IsIgnored()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":5,\"tutorialSeen\":true,\"levels\":[]}");

            Assert.IsFalse(new SaveService(store, StartingHints).TutorialSeen);
        }

        [Test]
        public void Reset_MakesTheTutorialNewAgain_LikeAFirstRun()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            service.SetTutorialSeen(true);

            service.Reset();

            Assert.IsFalse(service.TutorialSeen);
            Assert.IsFalse(new SaveService(store, StartingHints).TutorialSeen);
        }

        [Test]
        public void TheSerializer_RoundTripsTheFlag()
        {
            string json = SaveSerializer.Serialize(new PlayerProgress(), true, true, 3, 0, null, true);

            Assert.IsTrue(SaveSerializer.TryDeserialize(json, StartingHints, out SaveContent back));
            Assert.IsTrue(back.TutorialSeen);
        }
    }
}
