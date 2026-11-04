using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    // Finishing or skipping the tutorial is remembered; leaving it half way is not.
    public class ProgressRecorderTutorialTests
    {
        private EventBus _bus;
        private FakeSaveService _save;
        private ProgressRecorder _recorder;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _save = new FakeSaveService(3);
            _recorder = new ProgressRecorder(_bus, _save, new NullAudioService(), new[] { 1 });
        }

        [TearDown]
        public void TearDown()
        {
            _recorder.Dispose();
        }

        [TestCase(TutorialEnd.Completed)]
        [TestCase(TutorialEnd.Skipped)]
        public void FinishingOrSkipping_MarksTheTutorialAsSeen(TutorialEnd reason)
        {
            _bus.Publish(new TutorialEnded(reason));

            Assert.IsTrue(_save.TutorialSeen);
        }

        [Test]
        public void LeavingInTheMiddle_DoesNotMarkItAsSeen_SoItStartsAgain()
        {
            _bus.Publish(new TutorialEnded(TutorialEnd.Left));

            Assert.IsFalse(_save.TutorialSeen);
        }

        [Test]
        public void AfterDispose_TheEndingIsNotListenedTo()
        {
            _recorder.Dispose();

            _bus.Publish(new TutorialEnded(TutorialEnd.Completed));

            Assert.IsFalse(_save.TutorialSeen);
        }
    }
}
