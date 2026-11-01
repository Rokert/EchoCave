using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class ProgressRecorderTests
    {
        private EventBus _bus;
        private SaveService _save;
        private NullAudioService _audio;
        private ProgressRecorder _recorder;

        // Catalog positions 0 to 4 carry these ids, not 1 to 5: a level is saved under its id, never its position.
        private static readonly int[] LevelIds = { 10, 20, 30, 40, 50 };

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _save = new SaveService(new InMemorySaveStore());
            _audio = new NullAudioService();
            _recorder = new ProgressRecorder(_bus, _save, _audio, LevelIds);
        }

        [TearDown]
        public void TearDown()
        {
            _recorder.Dispose();
        }

        [Test]
        public void AWin_IsSaved()
        {
            _bus.Publish(new LevelWon(4, 8, 2));

            Assert.AreEqual(2, _save.Progress.StarsOf(50));
            Assert.AreEqual(8, _save.Progress.BestMovesOf(50));
            Assert.IsFalse(_save.Progress.IsCompleted(5));
        }

        [Test]
        public void TheSameLevel_KeepsItsSave_WhenItsPositionChanges()
        {
            _bus.Publish(new LevelWon(1, 6, 3));
            _recorder.Dispose();
            var reordered = new ProgressRecorder(_bus, _save, _audio, new[] { 99, 30, 20, 10, 50 });

            _bus.Publish(new LevelWon(2, 5, 3));
            reordered.Dispose();

            Assert.AreEqual(5, _save.Progress.BestMovesOf(20));
            Assert.IsFalse(_save.Progress.IsCompleted(30));
        }

        [Test]
        public void ALoss_SavesNothing()
        {
            _bus.Publish(new LevelLost(1, 9));

            Assert.AreEqual(0, _save.Progress.CompletedCount);
        }

        [Test]
        public void AToggle_ReachesTheAudioService()
        {
            _bus.Publish(new AudioToggled(AudioChannel.Music, false));

            Assert.IsFalse(_audio.IsEnabled(AudioChannel.Music));
            Assert.IsTrue(_audio.IsEnabled(AudioChannel.Sound));
        }

        [Test]
        public void AToggle_IsSaved_SoItSurvivesARestart()
        {
            var store = new InMemorySaveStore();
            var save = new SaveService(store);
            using (new ProgressRecorder(_bus, save, new NullAudioService(), LevelIds))
            {
                _bus.Publish(new AudioToggled(AudioChannel.Sound, false));
            }

            var restarted = new SaveService(store);

            Assert.IsFalse(restarted.SoundOn);
            Assert.IsTrue(restarted.MusicOn);
        }

        [Test]
        public void AfterDispose_NothingIsListenedTo()
        {
            _recorder.Dispose();

            _bus.Publish(new LevelWon(0, 5, 3));
            _bus.Publish(new AudioToggled(AudioChannel.Sound, false));

            Assert.AreEqual(0, _save.Progress.CompletedCount);
            Assert.IsTrue(_audio.IsEnabled(AudioChannel.Sound));
        }
    }
}
