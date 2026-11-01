using EchoCave.Ads;
using EchoCave.Core;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Composition root of the Game scene: builds the flow, hands it the taps, ticks it, and frames the
    // camera whenever a level starts (boards can differ in size from level to level).
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private EchoSettings echoSettings;
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardCamera boardCamera;
        [SerializeField] private TapInput tapInput;

        public GameFlow Flow { get; private set; }

        private IAdService _ads;
        private ISaveService _save;
        private IAudioService _audio;
        private IAnalyticsService _analytics;
        private ProgressRecorder _recorder;

        private void Awake()
        {
            // Replace, not Register: with Reload Domain off the registry outlives a Play session.
            _ads = new NullAdService();
            _save = new SaveService(new PlayerPrefsSaveStore());
            _audio = new NullAudioService();
            _analytics = new NullAnalyticsService();
            ServiceLocator.Replace<IAdService>(_ads);
            ServiceLocator.Replace<ISaveService>(_save);
            ServiceLocator.Replace<IAudioService>(_audio);
            ServiceLocator.Replace<IAnalyticsService>(_analytics);

            var echoStrategy = new RevealAllHiddenStrategy(echoSettings.RevealSeconds);
            Flow = new GameFlow(boardView, catalog.ToDefinitions(), new MoveResolver(), EventBus.Global, echoStrategy);
        }

        private void OnEnable()
        {
            tapInput.Tapped += OnTapped;
            _recorder = new ProgressRecorder(EventBus.Global, _save, _audio);
            EventBus.Global.Subscribe<LevelStarted>(OnLevelStarted);
        }

        private void OnDisable()
        {
            tapInput.Tapped -= OnTapped;
            _recorder.Dispose();
            EventBus.Global.Unsubscribe<LevelStarted>(OnLevelStarted);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(_ads);
            ServiceLocator.Unregister(_save);
            ServiceLocator.Unregister(_audio);
            ServiceLocator.Unregister(_analytics);
        }

        private void Update()
        {
            Flow.Tick(Time.deltaTime);
        }

        private void OnTapped(Vector2 world)
        {
            Flow.HandleTap(world);
        }

        private void OnLevelStarted(LevelStarted started)
        {
            boardCamera.Frame(boardView);
        }
    }
}
