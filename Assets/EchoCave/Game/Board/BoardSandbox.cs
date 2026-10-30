using EchoCave.Core;
using Rokert.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Temporary entry point that makes the Game scene playable: one level, no flow, no HUD.
    // The composition root of ROK-133 replaces it.
    public sealed class BoardSandbox : MonoBehaviour
    {
        [SerializeField] private LevelData level;
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardCamera boardCamera;
        [SerializeField] private TapInput tapInput;

        private BoardPresenter _presenter;

        private void Start()
        {
            LevelSession session = LevelBuilder.Build(level.ToDefinition());
            _presenter = new BoardPresenter(session, new MoveResolver(), new CommandHistory(), boardView);
            _presenter.Start();
            boardCamera.Frame(boardView);
        }

        private void OnEnable()
        {
            tapInput.Tapped += OnTapped;
        }

        private void OnDisable()
        {
            tapInput.Tapped -= OnTapped;
        }

        private void OnTapped(Vector2 world)
        {
            _presenter?.HandleTap(world);
        }
    }
}
