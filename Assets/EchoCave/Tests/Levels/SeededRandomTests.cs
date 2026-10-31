using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void TheSameSeed_GivesTheSameSequence()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);

            for (int i = 0; i < 50; i++) Assert.AreEqual(a.Next(1000), b.Next(1000));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);

            bool differs = false;
            for (int i = 0; i < 20; i++)
            {
                if (a.Next(1000) != b.Next(1000)) differs = true;
            }

            Assert.IsTrue(differs);
        }

        [Test]
        public void Next_StaysInRange_AndReachesEveryValue()
        {
            var random = new SeededRandom(7);
            var seen = new bool[4];

            for (int i = 0; i < 400; i++)
            {
                int value = random.Next(4);
                Assert.That(value, Is.InRange(0, 3));
                seen[value] = true;
            }

            CollectionAssert.AreEqual(new[] { true, true, true, true }, seen);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Next_RejectsANonPositiveBound(int bound)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SeededRandom(1).Next(bound));
        }
    }
}
