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
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardCamera boardCamera;
        [SerializeField] private TapInput tapInput;
        [Tooltip("Starts the first level at once. Turn off when a menu exists.")]
        [SerializeField] private bool autoPlay = true;

        public GameFlow Flow { get; private set; }

        private void Awake()
        {
            Flow = new GameFlow(boardView, catalog.ToDefinitions(), new MoveResolver(), EventBus.Global);
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

        private void Start()
        {
            if (autoPlay) Flow.RequestPlay();
        }

        private void Update()
        {
            Flow.Tick();
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
