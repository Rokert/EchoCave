using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class TutorialEntryTests
    {
        [Test]
        public void ANewPlayer_OnABuildWithTheTutorialOn_StartsWithIt()
        {
            Assert.IsTrue(TutorialEntry.ShouldAutoStart(hasTutorial: true, autoStartEnabled: true, seen: false));
        }

        [Test]
        public void APlayerWhoSawIt_GoesStraightToTheFirstLevel()
        {
            Assert.IsFalse(TutorialEntry.ShouldAutoStart(true, true, seen: true));
        }

        [Test]
        public void ABuildWithTheAutoStartOff_NeverStartsItByItself()
        {
            Assert.IsFalse(TutorialEntry.ShouldAutoStart(true, autoStartEnabled: false, seen: false));
        }

        [Test]
        public void AGameWithoutLessons_HasNothingToStart()
        {
            Assert.IsFalse(TutorialEntry.ShouldAutoStart(hasTutorial: false, true, false));
        }
    }
}
