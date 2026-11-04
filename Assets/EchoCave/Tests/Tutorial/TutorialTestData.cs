using System.Collections.Generic;
using EchoCave.Core;

namespace EchoCave.Tests
{
    // Small valid lessons for the tutorial tests.
    internal static class TutorialTestData
    {
        // Two bats on a 3x3 board: bat 0 flies off to the left, bat 1 sits below it and points up into it.
        public static LevelDefinition TwoBatLevel(int moveLimit = 0, int pingBudget = 0, bool secondHidden = false) =>
            new LevelDefinition(
                3, 3,
                new[]
                {
                    new BatDefinition(new Cell(1, 1), Direction.Left),
                    new BatDefinition(new Cell(1, 0), Direction.Up, secondHidden)
                },
                moveLimit, pingBudget, 2, 3);

        public static TutorialLesson Lesson(
            string title = "Lesson", LevelDefinition level = null, IEnumerable<TutorialStep> steps = null, bool freeHint = false) =>
            new TutorialLesson(
                title,
                level ?? TwoBatLevel(),
                steps ?? new[]
                {
                    new TutorialStep("Tap the glowing bat.", TutorialAdvance.BatTapped, TutorialFocus.Bat, 0, limitTaps: true),
                    new TutorialStep("Well done.", TutorialAdvance.LevelWon)
                },
                freeHint);

        public static TutorialRunner Runner(int lessons = 2)
        {
            var list = new List<TutorialLesson>();
            for (int i = 0; i < lessons; i++) list.Add(Lesson($"Lesson {i + 1}"));
            return new TutorialRunner(list);
        }
    }
}
