using System.Collections.Generic;

namespace EchoCave.Core
{
    // The five lessons the game ships with, as plain code. The editor writes them into assets once
    // (EchoCave > Tutorial > Create default lessons); after that the assets are the source and can be tuned. Keeping the
    // script here means it is tested like any other level: valid, solvable, and playable through the real flow.
    //
    // A step that advances on a tap or a move must not be followed by a step that waits for the same move: both would
    // fire from the one tap, because the tap and the move happen together.
    public static class DefaultTutorial
    {
        public static IReadOnlyList<TutorialLesson> Lessons() => new[] { Fly(), Blocked(), Order(), Echo(), Hint() };

        // Three free bats, each pointing off the board: the arrow is the direction.
        private static TutorialLesson Fly() => new TutorialLesson(
            "Fly",
            new LevelDefinition(
                3, 3,
                new[]
                {
                    new BatDefinition(new Cell(1, 1), Direction.Up),
                    new BatDefinition(new Cell(0, 0), Direction.Left),
                    new BatDefinition(new Cell(2, 0), Direction.Right)
                },
                0, 0, 3, 4),
            new[]
            {
                new TutorialStep("Bats fly the way their arrow points. Tap this one!", TutorialAdvance.Exited, TutorialFocus.Bat, 0, limitTaps: true),
                new TutorialStep("Now send the others off the board.", TutorialAdvance.LevelWon)
            });

        // The left bat is blocked by the right one: tap it, watch it bounce, then free the path.
        private static TutorialLesson Blocked() => new TutorialLesson(
            "Blocked",
            new LevelDefinition(
                4, 3,
                new[]
                {
                    new BatDefinition(new Cell(0, 1), Direction.Right),
                    new BatDefinition(new Cell(2, 1), Direction.Right)
                },
                4, 0, 2, 3),
            new[]
            {
                new TutorialStep("A bat in the way blocks it. Tap the left bat.", TutorialAdvance.Bounced, TutorialFocus.Bat, 0, limitTaps: true),
                new TutorialStep(
                    "It bounced back and used a move. Moves are limited! Free the path first: tap the other bat.",
                    TutorialAdvance.Exited, TutorialFocus.Bat, 1, limitTaps: true),
                new TutorialStep("Now the way is clear. Tap the left bat again.", TutorialAdvance.LevelWon, TutorialFocus.Bat, 0, limitTaps: true)
            });

        // Three bats in a row, all pointing right: only the last one can leave first.
        private static TutorialLesson Order() => new TutorialLesson(
            "Order",
            new LevelDefinition(
                4, 3,
                new[]
                {
                    new BatDefinition(new Cell(0, 1), Direction.Right),
                    new BatDefinition(new Cell(1, 1), Direction.Right),
                    new BatDefinition(new Cell(2, 1), Direction.Right)
                },
                6, 0, 3, 4),
            new[]
            {
                new TutorialStep("Order matters. Find the bat that can fly off first.", TutorialAdvance.Exited),
                new TutorialStep("Free the path one bat at a time.", TutorialAdvance.LevelWon)
            });

        // Two dark bats and one visible: the echo shows the arrows for a few seconds. Pings are per level.
        private static TutorialLesson Echo() => new TutorialLesson(
            "Echo",
            new LevelDefinition(
                3, 3,
                new[]
                {
                    new BatDefinition(new Cell(0, 1), Direction.Left, true),
                    new BatDefinition(new Cell(2, 1), Direction.Right, true),
                    new BatDefinition(new Cell(1, 0), Direction.Down)
                },
                0, 2, 3, 4),
            new[]
            {
                new TutorialStep("Dark bats hide their arrows. Press the echo button to reveal them.", TutorialAdvance.Pinged, TutorialFocus.PingButton),
                new TutorialStep(
                    "Quick, the arrows fade after a few seconds. Pings are limited, but they come back on every level.",
                    TutorialAdvance.LevelWon)
            });

        // A column of three bats pointing up: only the top one is free. The hint in this lesson is free.
        private static TutorialLesson Hint() => new TutorialLesson(
            "Hint",
            new LevelDefinition(
                3, 3,
                new[]
                {
                    new BatDefinition(new Cell(1, 0), Direction.Up),
                    new BatDefinition(new Cell(1, 1), Direction.Up),
                    new BatDefinition(new Cell(1, 2), Direction.Up)
                },
                6, 0, 3, 4),
            new[]
            {
                new TutorialStep("Stuck? Press the light bulb to see a bat that can fly now.", TutorialAdvance.HintUsed, TutorialFocus.HintButton),
                new TutorialStep(
                    "Follow the pulsing bat. This hint was free, but hints are limited: keep them for hard levels.",
                    TutorialAdvance.LevelWon)
            },
            freeHint: true);
    }
}
