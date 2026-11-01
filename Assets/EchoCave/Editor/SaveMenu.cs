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
            var service = new SaveService(new PlayerPrefsSaveStore());
            PlayerProgress progress = service.Progress;

            var text = new StringBuilder();
            text.Append("[Save] ").Append(progress.CompletedCount).Append(" levels won, ").Append(progress.TotalStars).Append(" stars.");
            text.Append("\n  sound ").Append(service.SoundOn ? "on" : "off").Append(", music ").Append(service.MusicOn ? "on" : "off");
            foreach (LevelRecord record in progress.Records)
                text.Append("\n  level id ").Append(record.LevelId).Append(": ").Append(record.Stars).Append(" stars, best ").Append(record.BestMoves).Append(" moves");
            if (service.RecoveredFromBadData) text.Append("\n  The stored save could not be read.");

            Debug.Log(text.ToString());
        }

        [MenuItem("EchoCave/Save/Delete progress")]
        private static void DeleteProgress()
        {
            if (!EditorUtility.DisplayDialog("Delete progress", "Delete the saved progress of this editor?", "Delete", "Cancel")) return;

            new SaveService(new PlayerPrefsSaveStore()).Reset();
            Debug.Log("[Save] Progress deleted.");
        }
    }
}
