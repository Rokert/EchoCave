using System;
using System.Collections.Generic;
using System.Text;
using EchoCave.Game;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Editor
{
    // Fills a SoundBank from the clips in Audio/Sfx, matching each cue to its file by name (SoundCueNames).
    // Volumes and intervals you already tuned are kept; only new cues get the defaults below.
    public static class SoundBankMenu
    {
        private const string SfxFolder = "Assets/EchoCave/Audio/Sfx";

        private static float DefaultVolume(SoundCue cue) => 1f;

        // Short cues that can fire several times in the same instant are limited so they do not smear.
        private static float DefaultInterval(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Reveal: return 0.06f;
                case SoundCue.Click: return 0.04f;
                case SoundCue.Toggle: return 0.04f;
                default: return 0f;
            }
        }

        [MenuItem("EchoCave/Audio/Fill sound bank")]
        private static void Fill()
        {
            SoundBank bank = FindBank();
            if (bank == null) return;

            var serialized = new SerializedObject(bank);
            SerializedProperty entries = serialized.FindProperty("entries");

            // What was already tuned, by cue.
            var kept = new Dictionary<SoundCue, (float volume, float interval)>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                var cue = (SoundCue)entry.FindPropertyRelative("cue").intValue;
                if (!kept.ContainsKey(cue))
                    kept[cue] = (entry.FindPropertyRelative("volume").floatValue, entry.FindPropertyRelative("minIntervalSeconds").floatValue);
            }

            var cues = (SoundCue[])Enum.GetValues(typeof(SoundCue));
            entries.arraySize = cues.Length;
            var report = new StringBuilder();
            int missing = 0;

            for (int i = 0; i < cues.Length; i++)
            {
                SoundCue cue = cues[i];
                string path = $"{SfxFolder}/{SoundCueNames.FileName(cue)}.wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    missing++;
                    report.Append("\n  ").Append(cue).Append(": no clip at ").Append(path);
                }

                (float volume, float interval) values = kept.TryGetValue(cue, out var existing)
                    ? existing
                    : (DefaultVolume(cue), DefaultInterval(cue));

                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("cue").intValue = (int)cue;
                entry.FindPropertyRelative("clip").objectReferenceValue = clip;
                entry.FindPropertyRelative("volume").floatValue = values.volume;
                entry.FindPropertyRelative("minIntervalSeconds").floatValue = values.interval;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();

            if (missing == 0) Debug.Log($"[Sound bank] Filled {cues.Length} cues from {SfxFolder}.", bank);
            else Debug.LogError($"[Sound bank] Filled the bank, but {missing} cue(s) have no clip:{report}", bank);
        }

        // The selected bank, or the only one in the project.
        private static SoundBank FindBank()
        {
            if (Selection.activeObject is SoundBank selected) return selected;

            string[] guids = AssetDatabase.FindAssets("t:SoundBank");
            if (guids.Length == 1) return AssetDatabase.LoadAssetAtPath<SoundBank>(AssetDatabase.GUIDToAssetPath(guids[0]));

            Debug.LogError(guids.Length == 0
                ? "[Sound bank] There is no SoundBank asset. Create one with Assets > Create > EchoCave > Sound Bank."
                : "[Sound bank] There are several SoundBank assets: select the one to fill.");
            return null;
        }
    }
}
