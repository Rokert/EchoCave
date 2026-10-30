using UnityEngine;

namespace EchoCave.Game
{
    internal static class SpriteMetrics
    {
        // The size the sprite's rect takes in world units: pixels over pixels per unit. It does not depend on
        // the mesh type or on any geometry the sprite carries.
        public static Vector2 Size(Sprite sprite)
        {
            return sprite.rect.size / sprite.pixelsPerUnit;
        }
    }
}
