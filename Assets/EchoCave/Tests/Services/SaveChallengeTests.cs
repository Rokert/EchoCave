using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    // Save version 7: the challenge tries and results.
    public class SaveChallengeTests
    {
        private const int StartingHints = 3;
        private const string Id = "challenge-001";

        private static ChallengeResult Result(int moves, float seconds) => new ChallengeResult(moves, seconds, 1, 1700000000L);

        [Test]
        public void ANewSave_HasNoChallengeRecords()
        {
            var service = new SaveService(new InMemorySaveStore(), StartingHints);

            Assert.AreEqual(0, service.Challenges.Get(Id).Attempts);
            Assert.IsEmpty(service.Challenges.Records);
        }

        [Test]
        public void TriesAndResults_SurviveARestart()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            service.AddChallengeAttempt(Id);
            service.AddChallengeAttempt(Id);
            service.RecordChallengeSolve(Id, Result(52, 140.5f));
            service.RecordChallengeSolve(Id, Result(45, 300f));

            ChallengeRecord back = new SaveService(store, StartingHints).Challenges.Get(Id);

            Assert.IsFalse(new SaveService(store, StartingHints).RecoveredFromBadData);
            Assert.AreEqual(2, back.Attempts);
            Assert.AreEqual(2, back.Results.Count);
            Assert.AreEqual(45, back.Best.Moves);
            Assert.AreEqual(300f, back.Best.Seconds, 1e-4f);
            Assert.AreEqual(1, back.Best.Pings);
            Assert.AreEqual(1700000000L, back.Best.SolvedAtUnixSeconds);
            Assert.AreEqual(140.5f, back.Results[1].Seconds, 1e-4f);
        }

        [Test]
        public void AResultThatDoesNotRankAmongTheBest_WritesNothing()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            for (int i = 0; i < ChallengeProgress.MaxResults; i++) service.RecordChallengeSolve(Id, Result(40 + i, 10f));
            store.TryRead(out string before);

            ChallengeSolveOutcome outcome = service.RecordChallengeSolve(Id, Result(99, 10f));

            store.TryRead(out string after);
            Assert.IsFalse(outcome.Stored);
            Assert.AreEqual(before, after);
        }

        [Test]
        public void Reset_ForgetsTheChallengeRecordsToo()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store, StartingHints);
            service.AddChallengeAttempt(Id);
            service.RecordChallengeSolve(Id, Result(52, 140f));

            service.Reset();

            Assert.AreEqual(0, service.Challenges.Get(Id).Attempts);
            Assert.AreEqual(0, new SaveService(store, StartingHints).Challenges.Get(Id).Attempts);
        }

        [Test]
        public void AVersion6Save_ReadsWithNoChallengeRecords_AndKeepsEverythingElse()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":6,\"hints\":2,\"tutorialSeen\":true,\"levels\":[{\"id\":1,\"stars\":3,\"moves\":4}]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.RecoveredFromBadData);
            Assert.IsEmpty(service.Challenges.Records);
            Assert.AreEqual(3, service.Progress.StarsOf(1));
            Assert.AreEqual(2, service.Hints);
            Assert.IsTrue(service.TutorialSeen);
        }

        [Test]
        public void BadChallengeEntries_AreSkipped_AndTheRestIsKept()
        {
            var store = new InMemorySaveStore();
            store.Write("{\"version\":7,\"hints\":2,\"levels\":[{\"id\":1,\"stars\":3,\"moves\":4}],\"challenges\":[" +
                        "{\"id\":\"Bad Id\",\"attempts\":9,\"results\":[]}," +
                        "{\"id\":\"challenge-001\",\"attempts\":4,\"results\":[{\"moves\":0,\"seconds\":5,\"pings\":0,\"solvedAt\":1},{\"moves\":50,\"seconds\":12.5,\"pings\":1,\"solvedAt\":7}]}]}");

            var service = new SaveService(store, StartingHints);

            Assert.IsFalse(service.RecoveredFromBadData);
            Assert.AreEqual(1, service.Challenges.Records.Count);
            ChallengeRecord record = service.Challenges.Get(Id);
            Assert.AreEqual(4, record.Attempts);
            Assert.AreEqual(1, record.Results.Count, "the result with no taps was dropped");
            Assert.AreEqual(50, record.Best.Moves);
            Assert.AreEqual(3, service.Progress.StarsOf(1));
        }

        [Test]
        public void TheSameSave_AlwaysWritesTheSameText()
        {
            var progress = new ChallengeProgress();
            progress.AddAttempt("b-2");
            progress.AddAttempt("a-1");
            progress.RecordSolve("a-1", Result(50, 10f));

            string first = SaveSerializer.Serialize(new PlayerProgress(), true, true, 3, challenges: progress);
            string second = SaveSerializer.Serialize(new PlayerProgress(), true, true, 3, challenges: progress);

            Assert.AreEqual(first, second);
            Assert.Less(first.IndexOf("a-1"), first.IndexOf("b-2"), "ordered by id");
        }
    }
}
