using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // Checks a tutorial script before it ships, like LevelValidator does for a level. One message per problem, each
    // starting with the lesson's number so it can be found.
    public static class TutorialValidator
    {
        public static IReadOnlyList<string> Validate(IReadOnlyList<TutorialLesson> lessons)
        {
            if (lessons == null) throw new ArgumentNullException(nameof(lessons));

            var errors = new List<string>();
            if (lessons.Count == 0) errors.Add("The tutorial has no lessons.");

            for (int i = 0; i < lessons.Count; i++)
                ValidateLesson(lessons[i], $"Lesson {i + 1}", errors);

            return errors;
        }

        private static void ValidateLesson(TutorialLesson lesson, string name, List<string> errors)
        {
            if (lesson == null)
            {
                errors.Add($"{name} is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(lesson.Title)) errors.Add($"{name} has no title.");

            foreach (string problem in LevelValidator.Validate(lesson.Level))
                errors.Add($"{name}: the level is invalid: {problem}");

            LevelDefinition level = lesson.Level;

            // A lesson must never trap the player: a limit is allowed, but a generous one.
            if (level.HasMoveLimit && level.MoveLimit < level.Bats.Count * 2)
                errors.Add($"{name}: the move limit ({level.MoveLimit}) must be 0 or at least twice the bats ({level.Bats.Count * 2}).");

            if (lesson.Steps.Count == 0)
            {
                errors.Add($"{name} has no steps.");
                return;
            }

            for (int s = 0; s < lesson.Steps.Count; s++)
                ValidateStep(lesson, lesson.Steps[s], $"{name}, step {s + 1}", s == lesson.Steps.Count - 1, errors);
        }

        private static void ValidateStep(TutorialLesson lesson, TutorialStep step, string name, bool isLast, List<string> errors)
        {
            if (step == null)
            {
                errors.Add($"{name} is missing.");
                return;
            }

            LevelDefinition level = lesson.Level;

            if (string.IsNullOrWhiteSpace(step.Text)) errors.Add($"{name} has no text.");

            if (step.Focus == TutorialFocus.Bat && (step.FocusBatId < 0 || step.FocusBatId >= level.Bats.Count))
                errors.Add($"{name} points at bat {step.FocusBatId}, but the level has {level.Bats.Count} bats.");

            if (step.LimitTaps && step.Focus != TutorialFocus.Bat)
                errors.Add($"{name} limits taps but points at no bat.");

            if (step.Focus == TutorialFocus.PingButton && level.PingBudget < 1)
                errors.Add($"{name} points at the ping button, but the level has no pings.");

            if (step.Advance == TutorialAdvance.Pinged && level.PingBudget < 1)
                errors.Add($"{name} waits for a ping, but the level has no pings.");

            if ((step.Focus == TutorialFocus.HintButton || step.Advance == TutorialAdvance.HintUsed) && !lesson.FreeHint)
                errors.Add($"{name} uses the hint button, but the lesson has no free hint.");

            if (isLast && step.Advance != TutorialAdvance.LevelWon)
                errors.Add($"{name} is the last step, so it must advance on the level being won.");

            if (!isLast && step.Advance == TutorialAdvance.LevelWon)
                errors.Add($"{name} advances on the level being won, but it is not the last step.");
        }
    }
}
