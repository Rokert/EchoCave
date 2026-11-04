using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    public enum TutorialPhase
    {
        NotStarted,

        // A step is showing.
        InStep,

        // The level of the lesson was won; the player presses Continue for the next lesson.
        LessonComplete,

        // All lessons done, or skipped.
        Finished
    }

    // Follows the player through the lessons. The game tells it what the player did (a tap, a move, a ping, a hint, a
    // win) and it says which step is current. Pure and deterministic; GameFlow and the overlay do the rest.
    public sealed class TutorialRunner
    {
        private readonly IReadOnlyList<TutorialLesson> _lessons;

        public TutorialPhase Phase { get; private set; } = TutorialPhase.NotStarted;
        public int LessonIndex { get; private set; }
        public int StepIndex { get; private set; }

        // True when the tutorial ended through Skip and not by finishing the last lesson.
        public bool WasSkipped { get; private set; }

        public int LessonCount => _lessons.Count;

        public bool IsActive => Phase == TutorialPhase.InStep || Phase == TutorialPhase.LessonComplete;

        // Null unless a lesson is running.
        public TutorialLesson CurrentLesson => IsActive ? _lessons[LessonIndex] : null;

        // Null unless a step is showing.
        public TutorialStep CurrentStep => Phase == TutorialPhase.InStep ? _lessons[LessonIndex].Steps[StepIndex] : null;

        // Raised after every change of lesson, step or phase.
        public event Action Changed;

        public TutorialRunner(IReadOnlyList<TutorialLesson> lessons)
        {
            _lessons = lessons ?? throw new ArgumentNullException(nameof(lessons));
            if (lessons.Count == 0) throw new ArgumentException("A tutorial needs at least one lesson.", nameof(lessons));
        }

        // Begins again from the first step of the first lesson, whatever happened before.
        public void Start()
        {
            LessonIndex = 0;
            StepIndex = 0;
            WasSkipped = false;
            Phase = TutorialPhase.InStep;
            Changed?.Invoke();
        }

        // Whether a tap on this bat may reach the board. A step that waits for something other than a move blocks every
        // tap: Continue is for reading, and a ping or a hint has to be asked for with its button first.
        public bool CanTap(int batId)
        {
            TutorialStep step = CurrentStep;
            if (step == null || WaitsForAButton(step.Advance)) return false;

            return !step.LimitTaps || batId == step.FocusBatId;
        }

        private static bool WaitsForAButton(TutorialAdvance advance)
        {
            return advance == TutorialAdvance.Continue || advance == TutorialAdvance.Pinged || advance == TutorialAdvance.HintUsed;
        }

        // The player tapped a bat. False when the tap must be ignored.
        public bool OnBatTapped(int batId)
        {
            if (!CanTap(batId)) return false;

            if (CurrentStep.Advance == TutorialAdvance.BatTapped) Next();
            return true;
        }

        // A tap was resolved: the bat flew off or bounced.
        public void OnMove(MoveOutcome outcome)
        {
            TutorialStep step = CurrentStep;
            if (step == null) return;

            if ((step.Advance == TutorialAdvance.Exited && outcome == MoveOutcome.Exited)
                || (step.Advance == TutorialAdvance.Bounced && outcome == MoveOutcome.Bounced))
                Next();
        }

        public void OnPinged()
        {
            if (CurrentStep?.Advance == TutorialAdvance.Pinged) Next();
        }

        public void OnHintUsed()
        {
            if (CurrentStep?.Advance == TutorialAdvance.HintUsed) Next();
        }

        // The level of the lesson was won: the lesson is over, whichever step was showing.
        public void OnLevelWon()
        {
            if (Phase != TutorialPhase.InStep) return;

            Phase = TutorialPhase.LessonComplete;
            Changed?.Invoke();
        }

        // The level was lost or restarted: the same lesson starts again from its first step.
        public void OnLevelRestarted()
        {
            if (!IsActive) return;

            StepIndex = 0;
            Phase = TutorialPhase.InStep;
            Changed?.Invoke();
        }

        // The Continue button. On a Continue step it moves on; after a won lesson it opens the next one, or ends the
        // tutorial after the last.
        public void Continue()
        {
            if (Phase == TutorialPhase.InStep)
            {
                if (CurrentStep.Advance == TutorialAdvance.Continue) Next();
                return;
            }

            if (Phase != TutorialPhase.LessonComplete) return;

            if (LessonIndex + 1 < _lessons.Count)
            {
                LessonIndex++;
                StepIndex = 0;
                Phase = TutorialPhase.InStep;
            }
            else
            {
                Phase = TutorialPhase.Finished;
            }

            Changed?.Invoke();
        }

        // Ends the tutorial at once, from any step.
        public void Skip()
        {
            if (!IsActive) return;

            WasSkipped = true;
            Phase = TutorialPhase.Finished;
            Changed?.Invoke();
        }

        private void Next()
        {
            StepIndex++;
            if (StepIndex >= _lessons[LessonIndex].Steps.Count) Phase = TutorialPhase.LessonComplete;

            Changed?.Invoke();
        }
    }
}
