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

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _save = new SaveService(new InMemorySaveStore());
            _audio = new NullAudioService();
            _recorder = new ProgressRecorder(_bus, _save, _audio);
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

            Assert.AreEqual(2, _save.Progress.StarsOf(4));
            Assert.AreEqual(8, _save.Progress.BestMovesOf(4));
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
