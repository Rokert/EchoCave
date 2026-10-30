using System;
using EchoCave.Game;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ScreenAspectTests
    {
        [TestCase(0.56f, false)]
        [TestCase(0.99f, false)]
        [TestCase(1f, true)]
        [TestCase(1.78f, true)]
        [TestCase(2.4f, true)]
        public void IsLandscape_FollowsTheAspect(float aspect, bool expected)
        {
            Assert.AreEqual(expected, ScreenAspect.IsLandscape(aspect));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void IsLandscape_RejectsAnAspectThatIsNotPositive(float aspect)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenAspect.IsLandscape(aspect));
        }

        [Test]
        public void IsLandscape_FromWidthAndHeight_AgreesWithTheAspect()
        {
            Assert.IsFalse(ScreenAspect.IsLandscape(1080f, 1920f));
            Assert.IsTrue(ScreenAspect.IsLandscape(1920f, 1080f));
            Assert.IsTrue(ScreenAspect.IsLandscape(800f, 800f), "a square window counts as landscape");
        }

        [TestCase(0f, 100f)]
        [TestCase(100f, 0f)]
        [TestCase(-5f, 100f)]
        public void IsLandscape_FromWidthAndHeight_RejectsASizeThatIsNotPositive(float width, float height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenAspect.IsLandscape(width, height));
        }
    }
}
