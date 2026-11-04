using System;
using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class TutorialValidatorTests
    {
        private static IReadOnlyList<string> Check(params TutorialLesson[] lessons) => TutorialValidator.Validate(lessons);

        private static void AssertOneProblem(IReadOnlyList<string> errors, string contains)
        {
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains(contains, errors[0]);
        }

        [Test]
        public void AValidScript_HasNoProblems()
        {
            Assert.IsEmpty(Check(TutorialTestData.Lesson(), TutorialTestData.Lesson()));
        }

        [Test]
        public void NoLessons_IsAProblem()
        {
            AssertOneProblem(Check(), "no lessons");
        }

        [Test]
        public void ALessonWithoutATitleOrSteps_IsReported_WithItsNumber()
        {
            IReadOnlyList<string> errors = Check(TutorialTestData.Lesson(), TutorialTestData.Lesson(title: " ", steps: new TutorialStep[0]));

            Assert.AreEqual(2, errors.Count);
            Assert.IsTrue(errors.All(e => e.StartsWith("Lesson 2")), string.Join("\n", errors));
        }

        [Test]
        public void ABrokenLevel_IsReported_WithTheLevelValidatorsMessage()
        {
            var outside = new LevelDefinition(2, 2, new[] { new BatDefinition(new Cell(5, 5), Direction.Up) }, 0, 0, 1, 2);

            AssertOneProblem(Check(TutorialTestData.Lesson(level: outside)), "outside");
        }

        [Test]
        public void AMoveLimitBelowTwiceTheBats_IsATrap_AndIsReported()
        {
            AssertOneProblem(Check(TutorialTestData.Lesson(level: TutorialTestData.TwoBatLevel(moveLimit: 3))), "twice the bats");
            Assert.IsEmpty(Check(TutorialTestData.Lesson(level: TutorialTestData.TwoBatLevel(moveLimit: 4))));
        }

        [Test]
        public void AStepWithNoText_IsReported()
        {
            var lesson = TutorialTestData.Lesson(steps: new[]
            {
                new TutorialStep("", TutorialAdvance.BatTapped),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)
            });

            AssertOneProblem(Check(lesson), "step 1 has no text");
        }

        [Test]
        public void PointingAtABatTheLevelDoesNotHave_IsReported()
        {
            var lesson = TutorialTestData.Lesson(steps: new[]
            {
                new TutorialStep("Tap it.", TutorialAdvance.BatTapped, TutorialFocus.Bat, 7),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)
            });

            AssertOneProblem(Check(lesson), "points at bat 7");
        }

        [Test]
        public void LimitingTapsWithoutAFocusedBat_IsReported()
        {
            var lesson = TutorialTestData.Lesson(steps: new[]
            {
                new TutorialStep("Tap.", TutorialAdvance.BatTapped, TutorialFocus.None, -1, limitTaps: true),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)
            });

            AssertOneProblem(Check(lesson), "limits taps");
        }

        [Test]
        public void APingStepInALevelWithoutPings_IsReported_ForTheFocusAndTheAdvance()
        {
            var lesson = TutorialTestData.Lesson(steps: new[]
            {
                new TutorialStep("Ping.", TutorialAdvance.Pinged, TutorialFocus.PingButton),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)
            });

            Assert.AreEqual(2, Check(lesson).Count);
            Assert.IsEmpty(Check(TutorialTestData.Lesson(level: TutorialTestData.TwoBatLevel(pingBudget: 1, secondHidden: true), steps: lesson.Steps)));
        }

        [Test]
        public void AHintStepNeedsAFreeHint()
        {
            var steps = new[]
            {
                new TutorialStep("Hint.", TutorialAdvance.HintUsed, TutorialFocus.HintButton),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)
            };

            AssertOneProblem(Check(TutorialTestData.Lesson(steps: steps)), "no free hint");
            Assert.IsEmpty(Check(TutorialTestData.Lesson(steps: steps, freeHint: true)));
        }

        [Test]
        public void TheLastStepMustWaitForTheWin_AndNoEarlierStepMay()
        {
            var noWin = TutorialTestData.Lesson(steps: new[] { new TutorialStep("Tap.", TutorialAdvance.BatTapped) });
            var earlyWin = TutorialTestData.Lesson(steps: new[]
            {
                new TutorialStep("Win.", TutorialAdvance.LevelWon),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)
            });

            AssertOneProblem(Check(noWin), "must advance on the level being won");
            AssertOneProblem(Check(earlyWin), "not the last step");
        }

        [Test]
        public void ANullLessonOrList_IsHandled()
        {
            AssertOneProblem(Check(new TutorialLesson[] { null }), "Lesson 1 is missing");
            Assert.Throws<ArgumentNullException>(() => TutorialValidator.Validate(null));
        }

        [Test]
        public void TheModelRejectsMissingParts()
        {
            Assert.Throws<ArgumentNullException>(() => new TutorialLesson("x", null, new TutorialStep[0]));
            Assert.Throws<ArgumentNullException>(() => new TutorialLesson("x", TutorialTestData.TwoBatLevel(), null));
        }
    }
}
