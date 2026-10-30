using System;
using EchoCave.UI;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class ScreenInsetsTests
    {
        private const float Tolerance = 0.0001f;
        private static readonly Vector2 ScreenSize = new Vector2(1170f, 2532f);

        [Test]
        public void AFullScreenSafeArea_HasNoInsets()
        {
            ScreenInsets insets = ScreenInsets.FromSafeArea(new Rect(0f, 0f, ScreenSize.x, ScreenSize.y), ScreenSize);

            Assert.AreEqual(0f, insets.Left, Tolerance);
            Assert.AreEqual(0f, insets.Top, Tolerance);
            Assert.AreEqual(0f, insets.Right, Tolerance);
            Assert.AreEqual(0f, insets.Bottom, Tolerance);
        }

        [Test]
        public void ANotchAtTheTop_BecomesATopInset_NotABottomOne()
        {
            // Unity's origin is the bottom left, so a notch is missing from the top of the rect.
            var safeArea = new Rect(0f, 102f, ScreenSize.x, ScreenSize.y - 102f - 141f);

            ScreenInsets insets = ScreenInsets.FromSafeArea(safeArea, ScreenSize);

            Assert.AreEqual(141f, insets.Top, Tolerance);
            Assert.AreEqual(102f, insets.Bottom, Tolerance);
            Assert.AreEqual(0f, insets.Left, Tolerance);
            Assert.AreEqual(0f, insets.Right, Tolerance);
        }

        [Test]
        public void ALandscapeNotch_BecomesASideInset()
        {
            Vector2 landscape = new Vector2(2532f, 1170f);
            var safeArea = new Rect(132f, 63f, landscape.x - 132f - 132f, landscape.y - 63f);

            ScreenInsets insets = ScreenInsets.FromSafeArea(safeArea, landscape);

            Assert.AreEqual(132f, insets.Left, Tolerance);
            Assert.AreEqual(132f, insets.Right, Tolerance);
            Assert.AreEqual(63f, insets.Bottom, Tolerance);
            Assert.AreEqual(0f, insets.Top, Tolerance);
        }

        [Test]
        public void ASafeAreaLargerThanTheScreen_NeverGivesANegativeInset()
        {
            ScreenInsets insets = ScreenInsets.FromSafeArea(new Rect(-10f, -10f, ScreenSize.x + 30f, ScreenSize.y + 30f), ScreenSize);

            Assert.AreEqual(0f, insets.Left, Tolerance);
            Assert.AreEqual(0f, insets.Top, Tolerance);
            Assert.AreEqual(0f, insets.Right, Tolerance);
            Assert.AreEqual(0f, insets.Bottom, Tolerance);
        }

        [Test]
        public void InPanelUnits_DividesByThePixelsEachUnitTakes()
        {
            var insets = new ScreenInsets(20f, 40f, 60f, 80f);

            ScreenInsets scaled = insets.InPanelUnits(2f);

            Assert.AreEqual(10f, scaled.Left, Tolerance);
            Assert.AreEqual(20f, scaled.Top, Tolerance);
            Assert.AreEqual(30f, scaled.Right, Tolerance);
            Assert.AreEqual(40f, scaled.Bottom, Tolerance);
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScreenInsets.FromSafeArea(new Rect(0f, 0f, 1f, 1f), Vector2.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ScreenInsets(1f, 1f, 1f, 1f).InPanelUnits(0f));
        }
    }
}
