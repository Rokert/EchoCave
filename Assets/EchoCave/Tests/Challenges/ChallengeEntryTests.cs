using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeEntryTests
    {
        private static LevelDefinition Level() => ChallengeLevels.Build(ChallengeLevels.Challenge003Id, new MoveResolver());

        [Test]
        public void TheThreshold_DefaultsToZero_AndKeepsWhatItIsGiven()
        {
            Assert.AreEqual(0, new ChallengeEntry("challenge-001", Level()).LevelsToUnlock);
            Assert.AreEqual(5, new ChallengeEntry("challenge-002", Level(), 5).LevelsToUnlock);
        }

        [Test]
        public void ANegativeThreshold_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChallengeEntry("challenge-001", Level(), -1));
        }

        [Test]
        public void TheDefaultThresholds_AreFourSevenAndTen_AndEveryChallengeHasOne()
        {
            CollectionAssert.AreEqual(new[] { 4, 7, 10 }, new List<int>
            {
                ChallengeLevels.DefaultLevelsToUnlock(ChallengeLevels.Challenge001Id),
                ChallengeLevels.DefaultLevelsToUnlock(ChallengeLevels.Challenge002Id),
                ChallengeLevels.DefaultLevelsToUnlock(ChallengeLevels.Challenge003Id)
            });

            foreach (string id in ChallengeLevels.AllIds) Assert.GreaterOrEqual(ChallengeLevels.DefaultLevelsToUnlock(id), 1, id);
            Assert.Throws<ArgumentException>(() => ChallengeLevels.DefaultLevelsToUnlock("challenge-999"));
        }
    }
}
