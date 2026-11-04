using System;
using EchoCave.Game;
using Rokert.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoCave.UI
{
    // Composition root of the UI: builds the HUD on the UIDocument, connects it to the game flow, tells the
    // board when a pointer is on the UI, and keeps the panel responsive (portrait or landscape scaling,
    // safe-area padding). It runs before the UIDocument so the panel settings it hands over are its own copy.
    [DefaultExecutionOrder(-100)]
    public sealed class UiRoot : MonoBehaviour
    {
        private const string PortraitClass = "portrait";
        private const string LandscapeClass = "landscape";

        [SerializeField] private UIDocument document;
        [SerializeField] private GameRoot gameRoot;
        [SerializeField] private TapInput tapInput;

        [Header("Ping shake")]
        [Tooltip("How far the ping button shakes sideways when a ping is refused, in panel units.")]
        [SerializeField, Min(0f)] private float pingShakeAmplitude = 14f;
        [SerializeField, Min(1)] private int pingShakeCount = 3;
        [SerializeField, Min(50)] private int pingShakeMillis = 320;

        [Header("Hint reward")]
        [Tooltip("How big the hint button swells when a free hint arrives, 1 being its normal size.")]
        [SerializeField, Min(1f)] private float hintPulseScale = 1.25f;
        [SerializeField, Min(50)] private int hintPulseMillis = 420;

        [Header("Tutorial ring")]
        [Tooltip("How far the ring reaches beyond the bat or button it points at, in panel units.")]
        [SerializeField, Min(0f)] private float tutorialRingPadding = 14f;
        [Tooltip("How big the ring swells once per pulse, 1 being no swell.")]
        [SerializeField, Min(1f)] private float tutorialRingPulseScale = 1.1f;
        [SerializeField, Min(0.1f)] private float tutorialRingPulseSeconds = 0.9f;

        [Header("Popups")]
        [Tooltip("Milliseconds between one earned star lighting up and the next.")]
        [SerializeField, Min(0)] private int starStaggerMillis = 280;

        private PanelSettings _panel;
        private VisualElement _hud;
        private VisualElement _safeArea;
        private HudPresenter _presenter;
        private VisualElement _menu;
        private VisualElement _root;
        private PopupsView _popupsView;
        private IAudioService _audio;
        private PopupsPresenter _popups;
        private TutorialView _tutorialView;
        private TutorialPresenter _tutorial;
        private VisualElement _tutorialLayer;
        private MenuPresenter _menuPresenter;
        private Vector2Int _appliedScreen;
        private bool _started;

        private void Awake()
        {
            // A copy, so changing the reference resolution at runtime never dirties the asset in the editor.
            _panel = Instantiate(document.panelSettings);
            document.panelSettings = _panel;
        }

        private void Start()
        {
            _started = true;
            Compose();
        }

        private void OnEnable()
        {
            if (_started) Compose();
        }

        private void OnDisable()
        {
            Decompose();
        }

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        private void Update()
        {
            if (_presenter == null) return;

            if (Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y) ApplyLayout();
        }

        private void Compose()
        {
            if (_presenter != null) return;

            VisualElement root = document.rootVisualElement;
            _root = root;
            _hud = root.Q<VisualElement>("hud-root");
            _safeArea = root.Q<VisualElement>("safe-area");
            _menu = root.Q<VisualElement>("menu-layer");

            LetPointerPassThrough(_hud);

            GameFlow flow = gameRoot.Flow;
            var view = new HudView(root, pingShakeAmplitude, pingShakeCount, pingShakeMillis, hintPulseScale, hintPulseMillis);
            _presenter = new HudPresenter(
                view, EventBus.Global, flow.RequestPing, gameRoot.RequestHint, () => TogglePause(flow), flow.LevelCount, gameRoot.Hints.Count, gameRoot.HintsCanBeRefilled,
                gameRoot.Hints.ReserveCount);

            _audio = gameRoot.Audio;
            var popupsView = new PopupsView(root, starStaggerMillis);
            _popupsView = popupsView;
            _popups = new PopupsPresenter(
                popupsView, EventBus.Global, new FlowPopupCommands(flow),
                _audio.IsEnabled(AudioChannel.Sound), _audio.IsEnabled(AudioChannel.Music));

            _tutorialLayer = root.Q<VisualElement>("tutorial-layer");
            _tutorialView = new TutorialView(
                root, new BoardTutorialTargets(gameRoot.Board, gameRoot.WorldCamera, () => root.panel),
                tutorialRingPadding, tutorialRingPulseScale, tutorialRingPulseSeconds);
            _tutorial = new TutorialPresenter(_tutorialView, EventBus.Global, new FlowTutorialCommands(flow));

            var menuView = new MenuView(root, Application.version);
            _menuPresenter = new MenuPresenter(
                menuView, EventBus.Global, gameRoot.RequestPlay, flow.State == GameState.Menu,
                flow.HasTutorial ? (Action)(() => flow.RequestTutorial(thenStartGame: false)) : null);

            _root.RegisterCallback<ClickEvent>(OnClick);
            popupsView.StarLit += OnStarLit;

            tapInput.SetBlocker(new UiPointerBlocker(document));

            ApplyLayout();
        }

        private void Decompose()
        {
            _root?.UnregisterCallback<ClickEvent>(OnClick);
            if (_popupsView != null) _popupsView.StarLit -= OnStarLit;
            _popupsView = null;
            _root = null;

            _presenter?.Dispose();
            _presenter = null;
            _popups?.Dispose();
            _popups = null;
            _menuPresenter?.Dispose();
            _menuPresenter = null;
            _tutorial?.Dispose();
            _tutorial = null;
            _tutorialView?.Dispose();
            _tutorialView = null;
            _tutorialLayer = null;

            if (tapInput != null) tapInput.SetBlocker(null);
        }

        // One handler for every button on the panel: the sound follows the button's classes (UiSoundRules).
        private void OnClick(ClickEvent evt)
        {
            Button button = null;
            for (var element = evt.target as VisualElement; element != null && button == null; element = element.parent)
                button = element as Button;
            if (button == null) return;

            SoundCue? cue = UiSoundRules.CueForButton(button.GetClasses());
            if (cue.HasValue) _audio.Play(cue.Value);
        }

        private void OnStarLit(int index)
        {
            _audio.Play(UiSoundRules.CueForStar(index));
        }

        private static void TogglePause(GameFlow flow)
        {
            if (flow.IsPaused) flow.RequestResume();
            else flow.RequestPause();
        }

        // The UXML marks its layout containers as ignoring the pointer, but the elements Unity wraps around the
        // document (the template container) answer by default and would swallow every tap on the board.
        private static void LetPointerPassThrough(VisualElement element)
        {
            for (VisualElement e = element; e != null; e = e.parent) e.pickingMode = PickingMode.Ignore;
        }

        private void ApplyLayout()
        {
            var screen = new Vector2(Screen.width, Screen.height);
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);

            UiScale scale = UiScaling.For(screen.x, screen.y);
            _panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _panel.referenceResolution = scale.ReferenceResolution;
            _panel.match = scale.Match;

            bool landscape = ScreenAspect.IsLandscape(screen.x, screen.y);
            _hud.EnableInClassList(LandscapeClass, landscape);
            _hud.EnableInClassList(PortraitClass, !landscape);
            _menu.EnableInClassList(LandscapeClass, landscape);
            _menu.EnableInClassList(PortraitClass, !landscape);

            ScreenInsets insets = ScreenInsets.FromSafeArea(Screen.safeArea, screen).InPanelUnits(scale.PixelsPerUnit(screen));
            _safeArea.style.paddingLeft = insets.Left;
            _safeArea.style.paddingTop = insets.Top;
            _safeArea.style.paddingRight = insets.Right;
            _safeArea.style.paddingBottom = insets.Bottom;

            // The tutorial layer sits outside the safe area, so it keeps the notch clear by itself.
            if (_tutorialLayer != null)
            {
                _tutorialLayer.style.paddingLeft = insets.Left;
                _tutorialLayer.style.paddingTop = insets.Top;
                _tutorialLayer.style.paddingRight = insets.Right;
                _tutorialLayer.style.paddingBottom = insets.Bottom;
            }
        }
    }
}
