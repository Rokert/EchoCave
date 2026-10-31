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
            public LevelMetrics Metrics;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly MoveResolver _resolver = new MoveResolver();
        private int _twoStarSlack = 2;
        private Vector2 _scroll;
        private LevelGenerationPlan _plan;
        private string _planReport = string.Empty;
        private bool _showPlan;

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

            DrawPlan();

            if (_rows.Count == 0)
            {
                EditorGUILayout.HelpBox("No LevelData assets found. Create one with Assets > Create > EchoCave > Level.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (Row row in _rows) DrawRow(row);
            EditorGUILayout.EndScrollView();
        }

        private void DrawPlan()
        {
            _showPlan = EditorGUILayout.Foldout(_showPlan, "Generation plan", true);
            if (!_showPlan) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _plan = (LevelGenerationPlan)EditorGUILayout.ObjectField("Plan", _plan, typeof(LevelGenerationPlan), false);
                if (_plan == null)
                {
                    EditorGUILayout.HelpBox("Create one with Assets > Create > EchoCave > Level Generation Plan, assign the catalog on it, then drop it here.", MessageType.Info);
                    return;
                }

                if (_plan.Catalog == null)
                    EditorGUILayout.HelpBox("The plan has no Level Catalog: levels are generated but the catalog is not filled.", MessageType.Warning);

                if (GUILayout.Button("Generate all (overwrites generated levels)"))
                {
                    if (EditorUtility.DisplayDialog("Generate all levels",
                            "Every entry that is not marked hand-made is rewritten from its seed, and the catalog is replaced with the plan's levels in order.",
                            "Generate", "Cancel"))
                    {
                        _planReport = LevelGenerationRunner.GenerateAll(_plan);
                        Refresh();
                    }
                }

                for (int i = 0; i < _plan.Entries.Count; i++)
                {
                    LevelGenerationPlan.Entry entry = _plan.Entries[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(entry.levelName, entry.keepAsIs ? "hand-made" : $"{entry.bats} bats, {entry.hidden} hidden", GUILayout.Width(200));
                        using (new EditorGUI.DisabledScope(entry.keepAsIs))
                        {
                            EditorGUILayout.LabelField("Seed", GUILayout.Width(34));
                            int seed = EditorGUILayout.IntField(entry.seed, GUILayout.Width(60));
                            if (seed != entry.seed)
                            {
                                Undo.RecordObject(_plan, "Change level seed");
                                entry.seed = seed;
                                EditorUtility.SetDirty(_plan);
                            }

                            if (GUILayout.Button("Regenerate", GUILayout.Width(90)))
                            {
                                _planReport = LevelGenerationRunner.GenerateOne(_plan, i);
                                Refresh();
                            }
                        }
                    }
                }

                if (_planReport.Length > 0) EditorGUILayout.HelpBox(_planReport, MessageType.None);
            }
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
                    $"Min moves {result.MinMoves}   |   Waves {result.Waves.Count}   |   Hidden bats {CountHidden(row.Definition)}   |   " +
                    $"Traps {row.Metrics.BlockedAtStart}   |   Difficulty {row.Metrics.Difficulty}");
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
                    row.Result = new LevelSolver(_resolver).Solve(definition);
                    if (row.Result.IsSolvable)
                        LevelMetrics.TryMeasure(definition, _resolver, out row.Metrics);

                    if (row.Result.IsSolvable)
                        row.Suggested = StarThresholds.Suggest(row.Result.MinMoves, definition.MoveLimit, _twoStarSlack);
                }

                _rows.Add(row);
            }

            Repaint();
        }
    }
}
