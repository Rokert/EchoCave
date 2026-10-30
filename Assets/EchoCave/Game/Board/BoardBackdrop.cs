using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoCave.Game
{
    // The layered night-cave background. It has a portrait set and a landscape set and shows the one that
    // matches the camera's aspect, so a narrow browser window gets the tall art and a wide one the wide art
    // (the same on WebGL, WebGPU and mobile). Every layer covers the view with a margin and drifts by depth,
    // following the pointer and a slow idle wander. It only reads the camera, so it works in the menu too.
    public sealed class BoardBackdrop : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BackdropSettings settings;
        [Tooltip("Back to front: base, far, motes, near, crystals. Textures, not sprites: a layer is always drawn whole, "
            + "however Unity sliced it on import.")]
        [SerializeField] private Texture2D[] portraitLayers;
        [Tooltip("Back to front, in the same order as the portrait layers.")]
        [SerializeField] private Texture2D[] landscapeLayers;

        // Only sets how big a texture is in world units. The scale is derived from it, so the value does not matter.
        private const float PixelsPerUnit = 100f;

        private readonly List<Sprite> _createdSprites = new List<Sprite>();
        private GameObject _portraitRoot;
        private GameObject _landscapeRoot;
        private SpriteRenderer[] _portrait;
        private SpriteRenderer[] _landscape;
        private SpriteRenderer[] _active;

        private Vector2 _viewSize;
        private float _appliedAspect;
        private float _appliedSize;
        private Vector2 _pointer;
        private Vector2 _pointerVelocity;

        private void Awake()
        {
            _portrait = CreateRenderers("Portrait", portraitLayers, out _portraitRoot);
            _landscape = CreateRenderers("Landscape", landscapeLayers, out _landscapeRoot);
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(targetCamera.aspect, _appliedAspect)
                || !Mathf.Approximately(targetCamera.orthographicSize, _appliedSize))
            {
                Refit();
            }

            Animate();
        }

        private void OnDestroy()
        {
            foreach (Sprite sprite in _createdSprites) Destroy(sprite);
        }

        private SpriteRenderer[] CreateRenderers(string setName, Texture2D[] textures, out GameObject root)
        {
            textures ??= new Texture2D[0];

            root = new GameObject(setName);
            root.transform.SetParent(transform, false);
            root.SetActive(false);

            var renderers = new SpriteRenderer[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                var layer = new GameObject(textures[i] != null ? textures[i].name : "Layer " + i);
                layer.transform.SetParent(root.transform, false);

                SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
                renderer.sprite = WholeSprite(textures[i]);
                renderer.sortingOrder = settings.BaseSortingOrder + i;
                renderers[i] = renderer;
            }

            return renderers;
        }

        // A sprite over the whole texture with a full-rect mesh, so it never depends on the import slicing.
        private Sprite WholeSprite(Texture2D texture)
        {
            if (texture == null) return null;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name;
            _createdSprites.Add(sprite);
            return sprite;
        }

        // Picks the set for the current aspect and scales each layer to cover the view plus its drift margin.
        private void Refit()
        {
            _appliedAspect = targetCamera.aspect;
            _appliedSize = targetCamera.orthographicSize;
            _viewSize = new Vector2(_appliedSize * 2f * _appliedAspect, _appliedSize * 2f);

            bool landscape = Parallax.IsLandscape(_appliedAspect);
            _portraitRoot.SetActive(!landscape);
            _landscapeRoot.SetActive(landscape);
            _active = landscape ? _landscape : _portrait;

            for (int i = 0; i < _active.Length; i++)
            {
                Sprite sprite = _active[i].sprite;
                if (sprite == null) continue;

                float scale = Parallax.CoverScale(
                    SpriteMetrics.Size(sprite), _viewSize, settings.DepthFor(i), settings.PointerStrength, settings.AmbientStrength);
                _active[i].transform.localScale = Vector3.one * scale;
            }
        }

        private void Animate()
        {
            Vector2 target = settings.ReduceMotion ? Vector2.zero : ReadPointerTarget();
            _pointer = Vector2.SmoothDamp(_pointer, target, ref _pointerVelocity, settings.PointerSmoothTime);
            Vector2 ambient = settings.ReduceMotion ? Vector2.zero : Parallax.Ambient(Time.time, settings.AmbientSpeed);

            Vector3 camera = targetCamera.transform.position;
            for (int i = 0; i < _active.Length; i++)
            {
                Vector2 offset = Parallax.Offset(
                    _pointer, ambient, _viewSize, settings.DepthFor(i), settings.PointerStrength, settings.AmbientStrength);
                _active[i].transform.position = new Vector3(camera.x + offset.x, camera.y + offset.y, 0f);
            }
        }

        // A mouse hovers, so it always steers the layers; touch and pen only while pressed, then they ease
        // back to the centre.
        private static Vector2 ReadPointerTarget()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return Vector2.zero;
            if (!(pointer is Mouse) && !pointer.press.isPressed) return Vector2.zero;

            return Parallax.NormalizePointer(pointer.position.ReadValue(), new Vector2(Screen.width, Screen.height));
        }
    }
}
