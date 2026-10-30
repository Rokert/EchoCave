using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoCave.Game
{
    // Temporary panel to drive and watch the flow until the UI Toolkit HUD exists (ROK-135).
    // Buttons are IMGUI; the keys (P play, R retry, N next, M menu) go through the Input System, in case
    // IMGUI does not receive the mouse when the project uses only the new input backend.
    public sealed class GameDebugOverlay : MonoBehaviour
    {
        [SerializeField] private GameRoot root;
        [Tooltip("Multiplies the panel size, for phone screens.")]
        [SerializeField, Min(1f)] private float scale = 2f;

        private void Update()
        {
            GameFlow flow = root.Flow;
            Keyboard keyboard = Keyboard.current;
            if (flow == null || keyboard == null) return;

            if (keyboard.pKey.wasPressedThisFrame) Press("P", flow, () => flow.RequestPlay());
            if (keyboard.rKey.wasPressedThisFrame) Press("R", flow, flow.RequestRetry);
            if (keyboard.nKey.wasPressedThisFrame) Press("N", flow, flow.RequestNext);
            if (keyboard.mKey.wasPressedThisFrame) Press("M", flow, flow.RequestMenu);
        }

        private void OnGUI()
        {
            GameFlow flow = root.Flow;
            if (flow == null) return;

            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(8f, 8f, 220f, 200f));

            GUILayout.Label($"State: {flow.State}");
            if (flow.State != GameState.Menu)
            {
                string limit = flow.CurrentLevel.HasMoveLimit ? flow.CurrentLevel.MoveLimit.ToString() : "-";
                GUILayout.Label($"Level {flow.LevelIndex + 1}/{flow.LevelCount}  Moves {flow.Session.MovesUsed}/{limit}");
            }

            switch (flow.State)
            {
                case GameState.Menu:
                    if (GUILayout.Button("Play (P)")) Press("Play", flow, () => flow.RequestPlay());
                    break;
                case GameState.Playing:
                    if (GUILayout.Button("Restart (R)")) Press("Restart", flow, flow.RequestRetry);
                    if (GUILayout.Button("Menu (M)")) Press("Menu", flow, flow.RequestMenu);
                    break;
                case GameState.Won:
                    if (GUILayout.Button("Next (N)")) Press("Next", flow, flow.RequestNext);
                    if (GUILayout.Button("Retry (R)")) Press("Retry", flow, flow.RequestRetry);
                    if (GUILayout.Button("Menu (M)")) Press("Menu", flow, flow.RequestMenu);
                    break;
                case GameState.Lost:
                    if (GUILayout.Button("Retry (R)")) Press("Retry", flow, flow.RequestRetry);
                    if (GUILayout.Button("Menu (M)")) Press("Menu", flow, flow.RequestMenu);
                    break;
            }

            GUILayout.EndArea();
        }

        private static void Press(string label, GameFlow flow, Action intent)
        {
            Debug.Log($"[GameDebugOverlay] {label} pressed in {flow.State}, level {flow.LevelIndex + 1}/{flow.LevelCount}");
            intent();
        }
    }
}
