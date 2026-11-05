using System;
using System.Collections.Generic;
using System.Text;
using EchoCave.Game;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Editor
{
    // Fills a MusicBank from the clips in Audio/Music, matching each track to its file by name (MusicTrackNames).
    // Volumes you already tuned are kept; only new tracks get the default below.
    public static class MusicBankMenu
    {
        private const string MusicFolder = "Assets/EchoCave/Audio/Music";

        // The loops peak at -6 dB, so this leaves the music well under the sound effects.
        private const float DefaultVolume = 0.6f;

        [MenuItem("EchoCave/Audio/Fill music bank")]
        private static void Fill()
        {
            MusicBank bank = FindBank();
            if (bank == null) return;

            var serialized = new SerializedObject(bank);
            SerializedProperty entries = serialized.FindProperty("entries");

            var kept = new Dictionary<MusicTrack, float>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                var track = (MusicTrack)entry.FindPropertyRelative("track").intValue;
                if (!kept.ContainsKey(track)) kept[track] = entry.FindPropertyRelative("volume").floatValue;
            }

            var tracks = (MusicTrack[])Enum.GetValues(typeof(MusicTrack));
            entries.arraySize = tracks.Length;
            var report = new StringBuilder();
            int missing = 0;

            for (int i = 0; i < tracks.Length; i++)
            {
                MusicTrack track = tracks[i];
                string path = $"{MusicFolder}/{MusicTrackNames.FileName(track)}.wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    missing++;
                    report.Append("\n  ").Append(track).Append(": no clip at ").Append(path);
                }

                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("track").intValue = (int)track;
                entry.FindPropertyRelative("clip").objectReferenceValue = clip;
                entry.FindPropertyRelative("volume").floatValue = kept.TryGetValue(track, out float volume) ? volume : DefaultVolume;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();

            if (missing == 0) Debug.Log($"[Music bank] Filled {tracks.Length} tracks from {MusicFolder}.", bank);
            else Debug.LogError($"[Music bank] Filled the bank, but {missing} track(s) have no clip:{report}", bank);
        }

        // The selected bank, or the only one in the project.
        private static MusicBank FindBank()
        {
            if (Selection.activeObject is MusicBank selected) return selected;

            string[] guids = AssetDatabase.FindAssets("t:MusicBank");
            if (guids.Length == 1) return AssetDatabase.LoadAssetAtPath<MusicBank>(AssetDatabase.GUIDToAssetPath(guids[0]));

            Debug.LogError(guids.Length == 0
                ? "[Music bank] There is no MusicBank asset. Create one with Assets > Create > EchoCave > Music Bank."
                : "[Music bank] There are several MusicBank assets: select the one to fill.");
            return null;
        }
    }
}
