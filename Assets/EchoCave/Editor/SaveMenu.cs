using System.Text;
using EchoCave.Core;
using EchoCave.Game;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Editor
{
    // Lets you look at and wipe the real save without a screen that shows it (nothing in the game reads it yet).
    public static class SaveMenu
    {
        [MenuItem("EchoCave/Save/Log progress")]
        private static void LogProgress()
        {
            var service = new SaveService(new PlayerPrefsSaveStore(), StartingHints());
            PlayerProgress progress = service.Progress;

            var text = new StringBuilder();
            text.Append("[Save] ").Append(progress.CompletedCount).Append(" levels won, ").Append(progress.TotalStars).Append(" stars.");
            text.Append("\n  hints ").Append(service.Hints);
            text.Append("\n  sound ").Append(service.SoundOn ? "on" : "off").Append(", music ").Append(service.MusicOn ? "on" : "off");
            foreach (LevelRecord record in progress.Records)
                text.Append("\n  level id ").Append(record.LevelId).Append(": ").Append(record.Stars).Append(" stars, best ").Append(record.BestMoves).Append(" moves");
            if (service.RecoveredFromBadData) text.Append("\n  The stored save could not be read.");

            Debug.Log(text.ToString());
        }

        // What a save with no hint count starts with, read from the project's HintSettings.
        private static int StartingHints()
        {
            string[] guids = AssetDatabase.FindAssets("t:HintSettings");
            if (guids.Length == 1)
                return AssetDatabase.LoadAssetAtPath<HintSettings>(AssetDatabase.GUIDToAssetPath(guids[0])).StartingHints;

            Debug.LogError("[Save] Expected exactly one HintSettings asset; the starting hints are taken as 0.");
            return 0;
        }

        [MenuItem("EchoCave/Save/Delete progress")]
        private static void DeleteProgress()
        {
            if (!EditorUtility.DisplayDialog("Delete progress", "Delete the saved progress of this editor?", "Delete", "Cancel")) return;

            new SaveService(new PlayerPrefsSaveStore(), StartingHints()).Reset();
            Debug.Log("[Save] Progress deleted.");
        }
    }
}
