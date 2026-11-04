using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    // What a tutorial step points at, so the overlay can draw a ring on it.
    public enum TutorialFocus
    {
        None,
        Bat,
        PingButton,
        HintButton
    }

    // What the player does to leave a step.
    public enum TutorialAdvance
    {
        // Presses Continue. Taps on the board are ignored meanwhile: the step is something to read.
        Continue,

        // Taps a bat (the focused one, when the step limits taps).
        BatTapped,

        // A tapped bat flew off the board.
        Exited,

        // A tapped bat hit another one and came back.
        Bounced,

        // Uses an echo ping. Taps on the board are ignored until the ping button is pressed.
        Pinged,

        // Uses the hint button. Taps on the board are ignored until the hint button is pressed.
        HintUsed,

        // Wins the level. Every lesson ends with a step like this.
        LevelWon
    }

    // One prompt of a lesson: a short English text, what to point at and what moves on to the next one.
    public sealed class TutorialStep
    {
        public string Text { get; }
        public TutorialAdvance Advance { get; }
        public TutorialFocus Focus { get; }

        // The bat to point at: its index in the level's Bats. -1 unless Focus is Bat.
        public int FocusBatId { get; }

        // Only the focused bat can be tapped, so the player cannot get lost in the very first steps.
        public bool LimitTaps { get; }

        public TutorialStep(string text, TutorialAdvance advance, TutorialFocus focus = TutorialFocus.None, int focusBatId = -1, bool limitTaps = false)
        {
            Text = text;
            Advance = advance;
            Focus = focus;
            FocusBatId = focusBatId;
            LimitTaps = limitTaps;
        }
    }

    // A short level that teaches one thing, with the steps that explain it. Lessons live outside the level catalog:
    // no stars, no progress, no streak, no ads.
    public sealed class TutorialLesson
    {
        public string Title { get; }
        public LevelDefinition Level { get; }
        public IReadOnlyList<TutorialStep> Steps { get; }

        // The hint button works in this lesson and does not take a hint from the player's stock.
        public bool FreeHint { get; }

        public TutorialLesson(string title, LevelDefinition level, IEnumerable<TutorialStep> steps, bool freeHint = false)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (steps == null) throw new ArgumentNullException(nameof(steps));

            Title = title;
            Level = level;
            Steps = new List<TutorialStep>(steps).AsReadOnly();
            FreeHint = freeHint;
        }
    }
}
