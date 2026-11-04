using System;
using System.Collections.Generic;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class TutorialRunnerTests
    {
        private int _changes;

        private TutorialRunner Started(params TutorialLesson[] lessons)
        {
            var runner = new TutorialRunner(lessons.Length > 0 ? lessons : new[] { TutorialTestData.Lesson() });
            runner.Changed += () => _changes++;
            runner.Start();
            _changes = 0;
            return runner;
        }

        private static TutorialLesson WithSteps(params TutorialStep[] steps) => TutorialTestData.Lesson(steps: steps, freeHint: true);

        [SetUp]
        public void SetUp()
        {
            _changes = 0;
        }

        [Test]
        public void BeforeStart_NothingIsShowing_AndNoTapGetsThrough()
        {
            var runner = TutorialTestData.Runner();

            Assert.AreEqual(TutorialPhase.NotStarted, runner.Phase);
            Assert.IsFalse(runner.IsActive);
            Assert.IsNull(runner.CurrentStep);
            Assert.IsNull(runner.CurrentLesson);
            Assert.IsFalse(runner.CanTap(0));
        }

        [Test]
        public void Start_ShowsTheFirstStepOfTheFirstLesson_AndSaysSo()
        {
            var runner = TutorialTestData.Runner();
            int changes = 0;
            runner.Changed += () => changes++;

            runner.Start();

            Assert.AreEqual(TutorialPhase.InStep, runner.Phase);
            Assert.AreEqual(0, runner.LessonIndex);
            Assert.AreEqual(0, runner.StepIndex);
            Assert.AreEqual("Tap the glowing bat.", runner.CurrentStep.Text);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void ALimitedStep_OnlyLetsTheFocusedBatThrough()
        {
            TutorialRunner runner = Started();

            Assert.IsFalse(runner.CanTap(1));
            Assert.IsFalse(runner.OnBatTapped(1), "a tap on another bat is ignored");
            Assert.AreEqual(0, runner.StepIndex);
            Assert.AreEqual(0, _changes);

            Assert.IsTrue(runner.CanTap(0));
            Assert.IsTrue(runner.OnBatTapped(0));
            Assert.AreEqual(1, runner.StepIndex, "tapping the focused bat moves on");
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void AnUnlimitedStep_LetsAnyBatThrough()
        {
            TutorialRunner runner = Started(WithSteps(
                new TutorialStep("Tap any bat.", TutorialAdvance.BatTapped),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)));

            Assert.IsTrue(runner.CanTap(1));
            Assert.IsTrue(runner.OnBatTapped(1));
            Assert.AreEqual(1, runner.StepIndex);
        }

        [Test]
        public void AContinueStep_BlocksTheBoard_UntilContinueIsPressed()
        {
            TutorialRunner runner = Started(WithSteps(
                new TutorialStep("Read this.", TutorialAdvance.Continue),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)));

            Assert.IsFalse(runner.CanTap(0));
            Assert.IsFalse(runner.OnBatTapped(0));
            Assert.AreEqual(0, runner.StepIndex);

            runner.Continue();

            Assert.AreEqual(1, runner.StepIndex);
            Assert.IsTrue(runner.CanTap(0), "the next step lets the player play");
        }

        [TestCase(TutorialAdvance.Pinged)]
        [TestCase(TutorialAdvance.HintUsed)]
        public void AStepThatWaitsForAButton_BlocksTheBoard_SoTheBatsCannotBeTappedInstead(TutorialAdvance advance)
        {
            TutorialRunner runner = Started(WithSteps(
                new TutorialStep("Use the button.", advance),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)));

            Assert.IsFalse(runner.CanTap(0));
            Assert.IsFalse(runner.CanTap(1));
            Assert.IsFalse(runner.OnBatTapped(0));
            Assert.AreEqual(0, runner.StepIndex);
        }

        [Test]
        public void TheBoardOpensAgain_OnceTheButtonWasUsed()
        {
            TutorialRunner runner = Started(WithSteps(
                new TutorialStep("Ping.", TutorialAdvance.Pinged, TutorialFocus.PingButton),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)));

            runner.OnPinged();

            Assert.IsTrue(runner.CanTap(0));
            Assert.IsTrue(runner.CanTap(1));
        }

        [Test]
        public void ContinueOnAStepThatWaitsForSomethingElse_DoesNothing()
        {
            TutorialRunner runner = Started();

            runner.Continue();

            Assert.AreEqual(0, runner.StepIndex);
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void MovesAdvanceOnlyTheStepThatWaitsForThatOutcome()
        {
            TutorialRunner runner = Started(WithSteps(
                new TutorialStep("It flies.", TutorialAdvance.Exited),
                new TutorialStep("It bounces.", TutorialAdvance.Bounced),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)));

            runner.OnMove(MoveOutcome.Bounced);
            Assert.AreEqual(0, runner.StepIndex, "a bounce does not satisfy a step waiting for a bat to fly off");

            runner.OnMove(MoveOutcome.Exited);
            Assert.AreEqual(1, runner.StepIndex);

            runner.OnMove(MoveOutcome.Exited);
            Assert.AreEqual(1, runner.StepIndex, "and a fly-off does not satisfy a step waiting for a bounce");

            runner.OnMove(MoveOutcome.Bounced);
            Assert.AreEqual(2, runner.StepIndex);
        }

        [Test]
        public void APingAndAHint_AdvanceOnlyTheirOwnSteps()
        {
            TutorialRunner runner = Started(WithSteps(
                new TutorialStep("Ping.", TutorialAdvance.Pinged),
                new TutorialStep("Hint.", TutorialAdvance.HintUsed),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)));

            runner.OnHintUsed();
            Assert.AreEqual(0, runner.StepIndex);

            runner.OnPinged();
            Assert.AreEqual(1, runner.StepIndex);

            runner.OnPinged();
            Assert.AreEqual(1, runner.StepIndex);

            runner.OnHintUsed();
            Assert.AreEqual(2, runner.StepIndex);
        }

        [Test]
        public void WinningTheLevel_EndsTheLesson_WhateverStepWasShowing()
        {
            TutorialRunner runner = Started(WithSteps(
                new TutorialStep("Ping.", TutorialAdvance.Pinged),
                new TutorialStep("Done.", TutorialAdvance.LevelWon)));

            runner.OnLevelWon();

            Assert.AreEqual(TutorialPhase.LessonComplete, runner.Phase);
            Assert.IsNull(runner.CurrentStep);
            Assert.IsNotNull(runner.CurrentLesson, "the lesson is still current until Continue");
            Assert.IsFalse(runner.CanTap(0));
        }

        [Test]
        public void ContinueAfterALesson_OpensTheNextOne_AndAfterTheLastEndsTheTutorial()
        {
            var runner = TutorialTestData.Runner(2);
            runner.Start();
            runner.OnLevelWon();

            runner.Continue();

            Assert.AreEqual(TutorialPhase.InStep, runner.Phase);
            Assert.AreEqual(1, runner.LessonIndex);
            Assert.AreEqual(0, runner.StepIndex);

            runner.OnLevelWon();
            runner.Continue();

            Assert.AreEqual(TutorialPhase.Finished, runner.Phase);
            Assert.IsFalse(runner.WasSkipped, "finishing the last lesson is not a skip");
            Assert.IsFalse(runner.IsActive);
        }

        [Test]
        public void ALostOrRestartedLevel_StartsTheSameLessonAgainFromItsFirstStep()
        {
            TutorialRunner runner = Started();
            runner.OnBatTapped(0);
            Assert.AreEqual(1, runner.StepIndex);

            runner.OnLevelRestarted();

            Assert.AreEqual(0, runner.StepIndex);
            Assert.AreEqual(0, runner.LessonIndex);
            Assert.AreEqual(TutorialPhase.InStep, runner.Phase);
        }

        [Test]
        public void Restarting_AfterTheLessonWasWon_OpensItAgain()
        {
            TutorialRunner runner = Started();
            runner.OnLevelWon();

            runner.OnLevelRestarted();

            Assert.AreEqual(TutorialPhase.InStep, runner.Phase);
            Assert.AreEqual(0, runner.StepIndex);
        }

        [Test]
        public void Skip_EndsTheTutorialAtOnce_AndRemembersItWasSkipped()
        {
            var runner = TutorialTestData.Runner(3);
            runner.Changed += () => _changes++;
            runner.Start();
            _changes = 0;

            runner.Skip();

            Assert.AreEqual(TutorialPhase.Finished, runner.Phase);
            Assert.IsTrue(runner.WasSkipped);
            Assert.IsNull(runner.CurrentStep);
            Assert.IsFalse(runner.CanTap(0));
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void AfterFinishing_NothingMovesAnyMore_AndSkipSaysNothing()
        {
            TutorialRunner runner = Started();
            runner.Skip();
            _changes = 0;

            runner.Skip();
            runner.Continue();
            runner.OnLevelWon();
            runner.OnLevelRestarted();
            runner.OnBatTapped(0);
            runner.OnMove(MoveOutcome.Exited);

            Assert.AreEqual(TutorialPhase.Finished, runner.Phase);
            Assert.AreEqual(0, _changes);
        }

        [Test]
        public void StartAgain_AfterASkip_BeginsFromTheTop_ForAReplayFromTheMenu()
        {
            var runner = TutorialTestData.Runner(2);
            runner.Start();
            runner.Skip();

            runner.Start();

            Assert.AreEqual(TutorialPhase.InStep, runner.Phase);
            Assert.AreEqual(0, runner.LessonIndex);
            Assert.IsFalse(runner.WasSkipped);
        }

        [Test]
        public void ARunnerWithoutLessons_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new TutorialRunner(null));
            Assert.Throws<ArgumentException>(() => new TutorialRunner(new List<TutorialLesson>()));
        }
    }
}
