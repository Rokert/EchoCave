using System;
using Rokert.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoCave.Game
{
    // Temporary debug aid until the main menu exists (ROK-151). The keys (P play, R retry, N next, M menu,
    // E ping) go through the Input System and always work. The IMGUI panel is off by default because the UI
    // Toolkit HUD and popups now cover the same buttons; tick Show Panel to bring it back.
    public sealed class GameDebugOverlay : MonoBehaviour
    {
        [SerializeField] private GameRoot root;
        [SerializeField] private bool showPanel;
        [Tooltip("Multiplies the panel size, for phone screens.")]
        [SerializeField, Min(1f)] private float scale = 2f;

        private string _notice = string.Empty;

        private void OnEnable()
        {
            EventBus.Global.Subscribe<LevelStarted>(OnLevelStarted);
            EventBus.Global.Subscribe<PingRefused>(OnPingRefused);
        }

        private void OnDisable()
        {
            EventBus.Global.Unsubscribe<LevelStarted>(OnLevelStarted);
            EventBus.Global.Unsubscribe<PingRefused>(OnPingRefused);
        }

        private void Update()
        {
            GameFlow flow = root.Flow;
            Keyboard keyboard = Keyboard.current;
            if (flow == null || keyboard == null) return;

            if (keyboard.pKey.wasPressedThisFrame) Press("P", flow, () => flow.RequestPlay());
            if (keyboard.rKey.wasPressedThisFrame) Press("R", flow, flow.RequestRetry);
            if (keyboard.nKey.wasPressedThisFrame) Press("N", flow, flow.RequestNext);
            if (keyboard.mKey.wasPressedThisFrame) Press("M", flow, flow.RequestMenu);
            if (keyboard.eKey.wasPressedThisFrame) Press("E", flow, flow.RequestPing);
        }

        private void OnGUI()
        {
            GameFlow flow = root.Flow;
            if (!showPanel || flow == null) return;

            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(8f, 8f, 260f, 260f));

            GUILayout.Label($"State: {flow.State}");
            if (flow.State != GameState.Menu)
            {
                string limit = flow.CurrentLevel.HasMoveLimit ? flow.CurrentLevel.MoveLimit.ToString() : "-";
                GUILayout.Label($"Level {flow.LevelIndex + 1}/{flow.LevelCount}  Moves {flow.Session.MovesUsed}/{limit}");
                GUILayout.Label($"Pings {flow.Echo.PingsLeft}  Reveal {flow.Echo.RemainingSeconds:0.0}s");
            }

            if (_notice.Length > 0) GUILayout.Label(_notice);

            switch (flow.State)
            {
                case GameState.Menu:
                    if (GUILayout.Button("Play (P)")) Press("Play", flow, () => flow.RequestPlay());
                    break;
                case GameState.Playing:
                    if (GUILayout.Button("Ping (E)")) Press("Ping", flow, flow.RequestPing);
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

        private void OnLevelStarted(LevelStarted started)
        {
            _notice = string.Empty;
        }

        private void OnPingRefused(PingRefused refused)
        {
            _notice = $"Ping refused: {refused.Reason}";
        }

        private static void Press(string label, GameFlow flow, Action intent)
        {
            Debug.Log($"[GameDebugOverlay] {label} pressed in {flow.State}, level {flow.LevelIndex + 1}/{flow.LevelCount}");
            intent();
        }
    }
}
