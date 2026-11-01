using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LevelIdRulesTests
    {
        [Test]
        public void UniquePositiveIds_AreValid_InAnyOrder()
        {
            Assert.IsEmpty(LevelIdRules.Validate(new[] { 3, 1, 10, 2 }));
            Assert.IsEmpty(LevelIdRules.Validate(new int[0]));
        }

        [Test]
        public void AnUnassignedOrNegativeId_IsReported()
        {
            var errors = LevelIdRules.Validate(new[] { 1, 0, -4 });

            Assert.AreEqual(2, errors.Count);
            StringAssert.Contains("position 2", errors[0]);
            StringAssert.Contains("position 3", errors[1]);
        }

        [Test]
        public void ARepeatedId_IsReported_AtTheSecondUse()
        {
            var errors = LevelIdRules.Validate(new[] { 4, 5, 4 });

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("position 3", errors[0]);
            StringAssert.Contains("4", errors[0]);
        }

        [Test]
        public void TheNextFreeId_IsOneAboveTheHighest_EvenWithGaps()
        {
            Assert.AreEqual(1, LevelIdRules.NextFree(new int[0]));
            Assert.AreEqual(4, LevelIdRules.NextFree(new[] { 1, 2, 3 }));
            Assert.AreEqual(11, LevelIdRules.NextFree(new[] { 1, 10, 4 }));
            Assert.AreEqual(1, LevelIdRules.NextFree(new[] { 0, 0 }));
        }
    }
}
