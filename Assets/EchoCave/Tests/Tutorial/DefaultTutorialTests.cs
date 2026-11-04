using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class DefaultTutorialTests
    {
        private static IReadOnlyList<TutorialLesson> Lessons => DefaultTutorial.Lessons();

        [Test]
        public void TheScript_HasFiveLessons_AndNoProblems()
        {
            Assert.AreEqual(5, Lessons.Count);
            Assert.IsEmpty(TutorialValidator.Validate(Lessons), string.Join("\n", TutorialValidator.Validate(Lessons)));
        }

        [Test]
        public void EveryLesson_CanBeSolved_WithinItsMoveLimit()
        {
            var solver = new LevelSolver(new MoveResolver());

            foreach (TutorialLesson lesson in Lessons)
            {
                SolveResult result = solver.Solve(lesson.Level);

                Assert.IsTrue(result.IsSolvable, lesson.Title);
                Assert.IsTrue(result.FitsMoveLimit, lesson.Title);
                Assert.AreEqual(lesson.Level.Bats.Count, result.MinMoves, $"{lesson.Title}: the solver's minimum is the bat count (D-010)");
            }
        }

        [Test]
        public void TheLessonsTeach_InTheOrderOfTheTicket()
        {
            string[] titles = Lessons.Select(l => l.Title).ToArray();

            CollectionAssert.AreEqual(new[] { "Fly", "Blocked", "Order", "Echo", "Hint" }, titles);
        }

        [Test]
        public void OnlyTheHintLesson_HasAFreeHint_AndOnlyTheEchoLessonHidesBats()
        {
            Assert.AreEqual(new[] { false, false, false, false, true }, Lessons.Select(l => l.FreeHint).ToArray());
            Assert.AreEqual(new[] { false, false, false, true, false }, Lessons.Select(l => l.Level.Bats.Any(b => b.IsHidden)).ToArray());
        }

        [Test]
        public void EveryText_IsShortEnoughForABubble()
        {
            foreach (TutorialLesson lesson in Lessons)
            foreach (TutorialStep step in lesson.Steps)
                Assert.LessOrEqual(step.Text.Length, 110, $"{lesson.Title}: \"{step.Text}\"");
        }
    }
}
