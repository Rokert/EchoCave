using System;
using EchoCave.UI;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class UiScalingTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Portrait_UsesTheTallCanvas_MatchedToTheWidth()
        {
            UiScale scale = UiScaling.For(1080f, 2400f);

            Assert.AreEqual(new Vector2Int(1080, 1920), scale.ReferenceResolution);
            Assert.AreEqual(0f, scale.Match);
        }

        [Test]
        public void Landscape_UsesTheWideCanvas_MatchedToTheHeight()
        {
            UiScale scale = UiScaling.For(2560f, 1440f);

            Assert.AreEqual(new Vector2Int(1920, 1080), scale.ReferenceResolution);
            Assert.AreEqual(1f, scale.Match);
        }

        [Test]
        public void ASquareWindow_CountsAsLandscape()
        {
            Assert.AreEqual(new Vector2Int(1920, 1080), UiScaling.For(900f, 900f).ReferenceResolution);
        }

        [TestCase(0f, 100f)]
        [TestCase(100f, 0f)]
        public void ASizeThatIsNotPositive_IsRejected(float width, float height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => UiScaling.For(width, height));
        }

        [Test]
        public void Portrait_PixelsPerUnit_IsTheWidthRatio_WhateverTheHeight()
        {
            UiScale scale = UiScaling.For(1080f, 2400f);

            Assert.AreEqual(1f, scale.PixelsPerUnit(new Vector2(1080f, 2400f)), Tolerance);
            Assert.AreEqual(2f, scale.PixelsPerUnit(new Vector2(2160f, 2400f)), Tolerance);
            Assert.AreEqual(2f, scale.PixelsPerUnit(new Vector2(2160f, 9999f)), Tolerance, "the height must not matter");
        }

        [Test]
        public void Landscape_PixelsPerUnit_IsTheHeightRatio_WhateverTheWidth()
        {
            UiScale scale = UiScaling.For(1920f, 1080f);

            Assert.AreEqual(1f, scale.PixelsPerUnit(new Vector2(1920f, 1080f)), Tolerance);
            Assert.AreEqual(2f, scale.PixelsPerUnit(new Vector2(2560f, 2160f)), Tolerance);
            Assert.AreEqual(2f, scale.PixelsPerUnit(new Vector2(9999f, 2160f)), Tolerance, "the width must not matter");
        }

        [Test]
        public void ThePanelAlwaysSpans_TheMatchedReferenceSide()
        {
            // Panel units across the matched side = screen pixels / pixels per unit = the reference side.
            Vector2 portraitScreen = new Vector2(1170f, 2532f);
            UiScale portrait = UiScaling.For(portraitScreen.x, portraitScreen.y);
            Assert.AreEqual(1080f, portraitScreen.x / portrait.PixelsPerUnit(portraitScreen), 0.01f);

            Vector2 landscapeScreen = new Vector2(2560f, 1440f);
            UiScale landscape = UiScaling.For(landscapeScreen.x, landscapeScreen.y);
            Assert.AreEqual(1080f, landscapeScreen.y / landscape.PixelsPerUnit(landscapeScreen), 0.01f);
        }
    }
}
