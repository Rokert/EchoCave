using System;
using UnityEngine.UIElements;

namespace EchoCave.UI
{
    // Draws the main menu: it finds its elements in the UXML by name and shows state through a USS class.
    public sealed class MenuView : IMenuView
    {
        private const string HiddenClass = "menu-hidden";

        private const string ButtonHiddenClass = "menu-button-hidden";

        private readonly VisualElement _layer;
        private readonly Button _howToPlay;
        private readonly Button _challenges;
        private readonly Label _challengesBadge;

        public event Action PlayPressed;
        public event Action HowToPlayPressed;
        public event Action ChallengesPressed;

        // The version is shown small at the bottom, so a tester can say which build a comment is about.
        public MenuView(VisualElement root, string version)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _layer = Require<VisualElement>(root, "menu-layer");
            Require<Button>(root, "play-button").clicked += () => PlayPressed?.Invoke();

            // Optional: a UXML without the button simply has no way to replay the tutorial.
            _howToPlay = root.Q<Button>("howto-button");
            if (_howToPlay != null) _howToPlay.clicked += () => HowToPlayPressed?.Invoke();

            // Optional too: a UXML without the button simply has no way into the challenges.
            _challenges = root.Q<Button>("challenges-button");
            if (_challenges != null) _challenges.clicked += () => ChallengesPressed?.Invoke();

            // The NEW tag is optional too.
            _challengesBadge = root.Q<Label>("challenges-badge");

            Require<Label>(root, "version-label").text = string.IsNullOrEmpty(version) ? string.Empty : "v" + version;
        }

        public void SetHowToPlayVisible(bool visible)
        {
            _howToPlay?.EnableInClassList(ButtonHiddenClass, !visible);
        }

        public void SetChallengesVisible(bool visible)
        {
            _challenges?.EnableInClassList(ButtonHiddenClass, !visible);
        }

        public void SetChallengesNew(bool isNew)
        {
            _challengesBadge?.EnableInClassList(ButtonHiddenClass, !isNew);
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
