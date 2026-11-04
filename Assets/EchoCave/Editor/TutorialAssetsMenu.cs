using System.Collections.Generic;
using System.Text;
using EchoCave.Core;
using EchoCave.Game;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Editor
{
    // Writes the default lessons into assets once, and checks the catalog. Existing lesson assets are never overwritten:
    // once they exist they are the source, and any tuning of the text or the layout is kept.
    public static class TutorialAssetsMenu
    {
        private const string Folder = "Assets/EchoCave/Data/Tutorial";
        private const string CatalogPath = "Assets/EchoCave/Data/TutorialCatalog.asset";

        [MenuItem("EchoCave/Tutorial/Create default lessons")]
        private static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/EchoCave/Data", "Tutorial");

            IReadOnlyList<TutorialLesson> defaults = DefaultTutorial.Lessons();
            var assets = new List<TutorialLessonData>(defaults.Count);
            var report = new StringBuilder();

            for (int i = 0; i < defaults.Count; i++)
            {
                string path = $"{Folder}/Lesson_{i + 1:00}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<TutorialLessonData>(path);
                if (existing != null)
                {
                    assets.Add(existing);
                    report.AppendLine($"kept {path}");
                    continue;
                }

                var data = ScriptableObject.CreateInstance<TutorialLessonData>();
                data.Configure(defaults[i]);
                AssetDatabase.CreateAsset(data, path);
                assets.Add(data);
                report.AppendLine($"created {path}");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<TutorialCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TutorialCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                report.AppendLine($"created {CatalogPath}");
            }

            // Only fill an empty catalog: a list somebody arranged by hand stays as it is.
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("lessons");
            if (list.arraySize == 0)
            {
                list.arraySize = assets.Count;
                for (int i = 0; i < assets.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                report.AppendLine("filled the catalog");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Tutorial] Default lessons:\n" + report);
        }

        [MenuItem("EchoCave/Tutorial/Validate lessons")]
        private static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TutorialCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[Tutorial] No catalog at {CatalogPath}. Run EchoCave > Tutorial > Create default lessons first.");
                return;
            }

            IReadOnlyList<string> errors = catalog.FindProblems();
            if (errors.Count == 0) Debug.Log($"[Tutorial] {catalog.Count} lessons, all valid and solvable.");
            else Debug.LogError("[Tutorial] Problems:\n" + string.Join("\n", errors));
        }
    }
}
