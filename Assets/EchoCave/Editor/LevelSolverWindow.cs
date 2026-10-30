using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Editor
{
    // Lists every LevelData asset with its validation and solver results, and can write the suggested
    // star thresholds back to the asset.
    public sealed class LevelSolverWindow : EditorWindow
    {
        private sealed class Row
        {
            public LevelData Asset;
            public LevelDefinition Definition;
            public IReadOnlyList<string> Errors;
            public SolveResult Result;
            public StarThresholds Suggested;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly LevelSolver _solver = new LevelSolver(new MoveResolver());
        private int _twoStarSlack = 2;
        private Vector2 _scroll;

        [MenuItem("EchoCave/Level Solver")]
        private static void Open()
        {
            GetWindow<LevelSolverWindow>("Level Solver");
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    Refresh();

                GUILayout.Space(10);
                GUILayout.Label("Two-star slack", GUILayout.Width(90));
                int slack = EditorGUILayout.IntField(_twoStarSlack, GUILayout.Width(40));
                if (slack != _twoStarSlack)
                {
                    _twoStarSlack = Mathf.Max(0, slack);
                    Refresh();
                }

                GUILayout.FlexibleSpace();
            }

            if (_rows.Count == 0)
            {
                EditorGUILayout.HelpBox("No LevelData assets found. Create one with Assets > Create > EchoCave > Level.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (Row row in _rows) DrawRow(row);
            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(Row row)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(row.Asset.name, EditorStyles.boldLabel);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = row.Asset;
                        EditorGUIUtility.PingObject(row.Asset);
                    }
                }

                if (row.Errors.Count > 0)
                {
                    foreach (string error in row.Errors)
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    return;
                }

                SolveResult result = row.Result;
                if (!result.IsSolvable)
                {
                    EditorGUILayout.HelpBox(
                        $"Not solvable. Bats that block each other: {string.Join(", ", result.DeadlockedBatIds)}.",
                        MessageType.Error);
                    return;
                }

                EditorGUILayout.LabelField(
                    $"Min moves {result.MinMoves}   |   Waves {result.Waves.Count}   |   Hidden bats {CountHidden(row.Definition)}");
                EditorGUILayout.LabelField($"Solution: {string.Join(" > ", result.Solution)}");

                if (!result.FitsMoveLimit)
                {
                    EditorGUILayout.HelpBox(
                        $"Move limit {row.Definition.MoveLimit} is below the minimum {result.MinMoves}.",
                        MessageType.Error);
                }

                bool matches = row.Definition.ThreeStarMoves == row.Suggested.ThreeStarMoves
                               && row.Definition.TwoStarMoves == row.Suggested.TwoStarMoves;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        $"Stars now {row.Definition.ThreeStarMoves}/{row.Definition.TwoStarMoves}   " +
                        $"suggested {row.Suggested.ThreeStarMoves}/{row.Suggested.TwoStarMoves}");

                    using (new EditorGUI.DisabledScope(matches))
                    {
                        if (GUILayout.Button("Apply", GUILayout.Width(60)))
                            ApplySuggested(row);
                    }
                }
            }
        }

        private static int CountHidden(LevelDefinition definition)
        {
            int count = 0;
            foreach (BatDefinition bat in definition.Bats)
            {
                if (bat.IsHidden) count++;
            }

            return count;
        }

        private void ApplySuggested(Row row)
        {
            LevelDefinition d = row.Definition;
            var updated = new LevelDefinition(
                d.Width, d.Height, d.Bats, d.MoveLimit, d.PingBudget,
                row.Suggested.ThreeStarMoves, row.Suggested.TwoStarMoves);

            Undo.RecordObject(row.Asset, "Apply suggested star thresholds");
            row.Asset.Configure(updated);
            EditorUtility.SetDirty(row.Asset);
            Refresh();
        }

        private void Refresh()
        {
            _rows.Clear();

            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:LevelData"))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(System.StringComparer.Ordinal);

            foreach (string path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (asset == null) continue;

                LevelDefinition definition = asset.ToDefinition();
                IReadOnlyList<string> errors = LevelValidator.Validate(definition);

                var row = new Row { Asset = asset, Definition = definition, Errors = errors };
                if (errors.Count == 0)
                {
                    row.Result = _solver.Solve(definition);
                    if (row.Result.IsSolvable)
                        row.Suggested = StarThresholds.Suggest(row.Result.MinMoves, definition.MoveLimit, _twoStarSlack);
                }

                _rows.Add(row);
            }

            Repaint();
        }
    }
}
