using System;
using System.Collections.Generic;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // Authoring asset for one tutorial lesson: a small level and the steps that explain it. Gameplay code works with the
    // TutorialLesson it produces. Lessons are not in the level catalog, so they have no level id and never touch saved
    // progress.
    [CreateAssetMenu(fileName = "Lesson", menuName = "EchoCave/Tutorial Lesson")]
    public sealed class TutorialLessonData : ScriptableObject
    {
        [Serializable]
        public struct StepEntry
        {
            [TextArea(1, 4)] public string text;
            public TutorialAdvance advance;
            public TutorialFocus focus;
            [Tooltip("The bat to point at: its index in the list of bats. Only used when the focus is Bat.")]
            public int focusBatId;
            [Tooltip("Only the focused bat can be tapped during this step.")]
            public bool limitTaps;
        }

        [SerializeField] private string title = "Lesson";

        [SerializeField, Min(1)] private int width = 3;
        [SerializeField, Min(1)] private int height = 3;
        [SerializeField] private List<LevelData.BatEntry> bats = new List<LevelData.BatEntry>();

        [Tooltip("0 means no move limit. Otherwise at least twice the number of bats, so a lesson never traps the player.")]
        [SerializeField, Min(0)] private int moveLimit;
        [SerializeField, Min(0)] private int pingBudget;
        [SerializeField, Min(1)] private int threeStarMoves = 1;
        [SerializeField, Min(1)] private int twoStarMoves = 1;

        [Tooltip("The hint button works in this lesson and does not take a hint from the player's stock.")]
        [SerializeField] private bool freeHint;

        [SerializeField] private List<StepEntry> steps = new List<StepEntry>();

        public string Title => title;

        public TutorialLesson ToLesson()
        {
            var definitions = new List<BatDefinition>(bats.Count);
            foreach (LevelData.BatEntry bat in bats)
                definitions.Add(new BatDefinition(new Cell(bat.cell.x, bat.cell.y), bat.direction, bat.hidden));

            var level = new LevelDefinition(width, height, definitions, moveLimit, pingBudget, threeStarMoves, twoStarMoves);

            var lessonSteps = new List<TutorialStep>(steps.Count);
            foreach (StepEntry step in steps)
                lessonSteps.Add(new TutorialStep(step.text, step.advance, step.focus, step.focus == TutorialFocus.Bat ? step.focusBatId : -1, step.limitTaps));

            return new TutorialLesson(title, level, lessonSteps, freeHint);
        }

        internal void Configure(TutorialLesson lesson)
        {
            title = lesson.Title;

            LevelDefinition level = lesson.Level;
            width = level.Width;
            height = level.Height;
            moveLimit = level.MoveLimit;
            pingBudget = level.PingBudget;
            threeStarMoves = level.ThreeStarMoves;
            twoStarMoves = level.TwoStarMoves;
            freeHint = lesson.FreeHint;

            bats = new List<LevelData.BatEntry>(level.Bats.Count);
            foreach (BatDefinition bat in level.Bats)
            {
                bats.Add(new LevelData.BatEntry
                {
                    cell = new Vector2Int(bat.Cell.X, bat.Cell.Y),
                    direction = bat.Direction,
                    hidden = bat.IsHidden
                });
            }

            steps = new List<StepEntry>(lesson.Steps.Count);
            foreach (TutorialStep step in lesson.Steps)
            {
                steps.Add(new StepEntry
                {
                    text = step.Text,
                    advance = step.Advance,
                    focus = step.Focus,
                    focusBatId = step.FocusBatId,
                    limitTaps = step.LimitTaps
                });
            }
        }
    }
}
