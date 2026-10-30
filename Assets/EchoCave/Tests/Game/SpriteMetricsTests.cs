using System.Collections.Generic;
using EchoCave.Game;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class SpriteMetricsTests
    {
        private const float Tolerance = 0.0001f;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created) Object.DestroyImmediate(created);
            _created.Clear();
        }

        private Sprite MakeSprite(int width, int height, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(
                texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);

            _created.Add(texture);
            _created.Add(sprite);
            return sprite;
        }

        [Test]
        public void Size_IsTheWholeImageInWorldUnits()
        {
            Sprite sprite = MakeSprite(100, 200, 100f);

            Vector2 size = SpriteMetrics.Size(sprite);

            Assert.AreEqual(1f, size.x, Tolerance);
            Assert.AreEqual(2f, size.y, Tolerance);
        }

        [Test]
        public void Size_DividesByThePixelsPerUnit()
        {
            Assert.AreEqual(2.56f, SpriteMetrics.Size(MakeSprite(256, 256, 100f)).x, Tolerance);
            Assert.AreEqual(1f, SpriteMetrics.Size(MakeSprite(256, 256, 256f)).x, Tolerance);
        }

        [Test]
        public void Size_FollowsTheSpriteRect_NotTheWholeTexture()
        {
            var texture = new Texture2D(200, 200, TextureFormat.RGBA32, false);
            Sprite slice = Sprite.Create(
                texture, new Rect(0f, 0f, 50f, 100f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _created.Add(texture);
            _created.Add(slice);

            Vector2 size = SpriteMetrics.Size(slice);

            Assert.AreEqual(0.5f, size.x, Tolerance);
            Assert.AreEqual(1f, size.y, Tolerance);
        }
    }
}
