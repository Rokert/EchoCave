using System;
using EchoCave.Game;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class ParallaxTests
    {
        // A tall view: its shorter side is 10.
        private static readonly Vector2 View = new Vector2(10f, 20f);
        private const float Pointer = 0.03f;
        private const float Ambient = 0.012f;
        private const float Tolerance = 0.0001f;

        [TestCase(0.56f, false)]
        [TestCase(0.99f, false)]
        [TestCase(1f, true)]
        [TestCase(1.78f, true)]
        [TestCase(2.4f, true)]
        public void IsLandscape_FollowsTheAspect(float aspect, bool expected)
        {
            Assert.AreEqual(expected, Parallax.IsLandscape(aspect));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void IsLandscape_RejectsAnAspectThatIsNotPositive(float aspect)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Parallax.IsLandscape(aspect));
        }

        [Test]
        public void NormalizePointer_PutsTheScreenCentreAtZero()
        {
            Vector2 result = Parallax.NormalizePointer(new Vector2(500f, 250f), new Vector2(1000f, 500f));

            Assert.AreEqual(0f, result.x, Tolerance);
            Assert.AreEqual(0f, result.y, Tolerance);
        }

        [Test]
        public void NormalizePointer_MapsTheCornersToMinusOneAndOne()
        {
            Vector2 size = new Vector2(1000f, 500f);

            Vector2 bottomLeft = Parallax.NormalizePointer(Vector2.zero, size);
            Vector2 topRight = Parallax.NormalizePointer(size, size);

            Assert.AreEqual(-1f, bottomLeft.x, Tolerance);
            Assert.AreEqual(-1f, bottomLeft.y, Tolerance);
            Assert.AreEqual(1f, topRight.x, Tolerance);
            Assert.AreEqual(1f, topRight.y, Tolerance);
        }

        [Test]
        public void NormalizePointer_ClampsPositionsOutsideTheScreen()
        {
            Vector2 result = Parallax.NormalizePointer(new Vector2(-50f, 900f), new Vector2(1000f, 500f));

            Assert.AreEqual(-1f, result.x, Tolerance);
            Assert.AreEqual(1f, result.y, Tolerance);
        }

        [Test]
        public void NormalizePointer_RejectsAnEmptyScreen()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Parallax.NormalizePointer(Vector2.one, Vector2.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => Parallax.NormalizePointer(Vector2.one, new Vector2(100f, 0f)));
        }

        [Test]
        public void Ambient_StartsCentred()
        {
            Vector2 result = Parallax.Ambient(0f, new Vector2(0.31f, 0.23f));

            Assert.AreEqual(0f, result.x, Tolerance);
            Assert.AreEqual(0f, result.y, Tolerance);
        }

        [Test]
        public void Ambient_NeverLeavesTheUnitSquare()
        {
            Vector2 speed = new Vector2(0.31f, 0.23f);

            for (float time = 0f; time < 200f; time += 0.37f)
            {
                Vector2 result = Parallax.Ambient(time, speed);
                Assert.LessOrEqual(Mathf.Abs(result.x), 1f);
                Assert.LessOrEqual(Mathf.Abs(result.y), 1f);
            }
        }

        [Test]
        public void Ambient_AxesRunOnTheirOwnSpeed()
        {
            Vector2 result = Parallax.Ambient(1f, new Vector2(1f, 0f));

            Assert.AreEqual(Mathf.Sin(1f), result.x, Tolerance);
            Assert.AreEqual(0f, result.y, Tolerance, "an axis with speed 0 must not move");
        }

        [Test]
        public void Offset_AtDepthZero_IsZero()
        {
            Vector2 result = Parallax.Offset(Vector2.one, Vector2.one, View, 0f, Pointer, Ambient);

            Assert.AreEqual(0f, result.x, Tolerance);
            Assert.AreEqual(0f, result.y, Tolerance);
        }

        [Test]
        public void Offset_MovesAgainstThePointer()
        {
            Vector2 result = Parallax.Offset(new Vector2(1f, 0f), Vector2.zero, View, 1f, Pointer, Ambient);

            Assert.Less(result.x, 0f);
            Assert.AreEqual(0f, result.y, Tolerance);
        }

        [Test]
        public void Offset_GrowsInProportionToDepth()
        {
            Vector2 half = Parallax.Offset(Vector2.one, Vector2.zero, View, 0.5f, Pointer, Ambient);
            Vector2 full = Parallax.Offset(Vector2.one, Vector2.zero, View, 1f, Pointer, Ambient);

            Assert.AreEqual(full.x, half.x * 2f, Tolerance);
            Assert.AreEqual(full.y, half.y * 2f, Tolerance);
        }

        [Test]
        public void Offset_IsAShareOfTheShorterSide_SoItLooksTheSameAtAnyResolution()
        {
            Vector2 small = Parallax.Offset(Vector2.one, Vector2.zero, new Vector2(10f, 20f), 1f, Pointer, Ambient);
            Vector2 flipped = Parallax.Offset(Vector2.one, Vector2.zero, new Vector2(20f, 10f), 1f, Pointer, Ambient);
            Vector2 triple = Parallax.Offset(Vector2.one, Vector2.zero, new Vector2(30f, 60f), 1f, Pointer, Ambient);

            Assert.AreEqual(small.x, flipped.x, Tolerance, "orientation must not matter, only the shorter side");
            Assert.AreEqual(small.x * 3f, triple.x, Tolerance);
        }

        [Test]
        public void Offset_NeverExceedsMaxOffset()
        {
            foreach (float depth in new[] { 0.3f, 0.8f, 1f })
            {
                float max = Parallax.MaxOffset(View, depth, Pointer, Ambient);

                for (float px = -1f; px <= 1f; px += 0.5f)
                for (float py = -1f; py <= 1f; py += 0.5f)
                for (float ax = -1f; ax <= 1f; ax += 0.5f)
                {
                    Vector2 offset = Parallax.Offset(new Vector2(px, py), new Vector2(ax, -ax), View, depth, Pointer, Ambient);

                    Assert.LessOrEqual(Mathf.Abs(offset.x), max + Tolerance);
                    Assert.LessOrEqual(Mathf.Abs(offset.y), max + Tolerance);
                }
            }
        }

        [Test]
        public void MaxOffset_AtDepthZero_IsZero()
        {
            Assert.AreEqual(0f, Parallax.MaxOffset(View, 0f, Pointer, Ambient), Tolerance);
        }

        [Test]
        public void CoverScale_CoversTheViewPlusTheMarginTheLayerNeedsToMove()
        {
            Vector2 sprite = new Vector2(10.8f, 19.2f);
            float margin = Parallax.MaxOffset(View, 1f, Pointer, Ambient) * 2f;

            float scale = Parallax.CoverScale(sprite, View, 1f, Pointer, Ambient);

            Assert.GreaterOrEqual(sprite.x * scale, View.x + margin - Tolerance);
            Assert.GreaterOrEqual(sprite.y * scale, View.y + margin - Tolerance);
        }

        [Test]
        public void CoverScale_AtDepthZero_NeedsNoMargin()
        {
            Vector2 sprite = new Vector2(10.8f, 19.2f);

            Assert.AreEqual(
                CameraFraming.CoverScale(sprite, View),
                Parallax.CoverScale(sprite, View, 0f, Pointer, Ambient),
                Tolerance);
        }

        [Test]
        public void CoverScale_GrowsWithDepth_BecauseNearLayersMoveFurther()
        {
            Vector2 sprite = View;

            float still = Parallax.CoverScale(sprite, View, 0f, Pointer, Ambient);
            float middle = Parallax.CoverScale(sprite, View, 0.5f, Pointer, Ambient);
            float near = Parallax.CoverScale(sprite, View, 1f, Pointer, Ambient);

            Assert.Less(still, middle);
            Assert.Less(middle, near);
        }
    }
}
