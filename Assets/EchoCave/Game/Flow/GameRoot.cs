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

        private void Awake()
        {
            var echoStrategy = new RevealAllHiddenStrategy(echoSettings.RevealSeconds);
            Flow = new GameFlow(boardView, catalog.ToDefinitions(), new MoveResolver(), EventBus.Global, echoStrategy);
        }

        private void OnEnable()
        {
            tapInput.Tapped += OnTapped;
            EventBus.Global.Subscribe<LevelStarted>(OnLevelStarted);
        }

        private void OnDisable()
        {
            tapInput.Tapped -= OnTapped;
            EventBus.Global.Unsubscribe<LevelStarted>(OnLevelStarted);
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
