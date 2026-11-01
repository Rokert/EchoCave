using System;
using UnityEngine.UIElements;

namespace EchoCave.UI
{
    // Draws the main menu: it finds its elements in the UXML by name and shows state through a USS class.
    public sealed class MenuView : IMenuView
    {
        private const string HiddenClass = "menu-hidden";

        private readonly VisualElement _layer;

        public event Action PlayPressed;

        // The version is shown small at the bottom, so a tester can say which build a comment is about.
        public MenuView(VisualElement root, string version)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _layer = Require<VisualElement>(root, "menu-layer");
            Require<Button>(root, "play-button").clicked += () => PlayPressed?.Invoke();
            Require<Label>(root, "version-label").text = string.IsNullOrEmpty(version) ? string.Empty : "v" + version;
        }

        public void Show() => _layer.RemoveFromClassList(HiddenClass);

        public void Hide() => _layer.AddToClassList(HiddenClass);

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null) throw new InvalidOperationException($"The menu UXML has no {typeof(T).Name} named \"{name}\".");

            return element;
        }
    }
}
