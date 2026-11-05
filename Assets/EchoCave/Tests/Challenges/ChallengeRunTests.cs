using System;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeRunTests
    {
        [Test]
        public void TheRun_KeepsTapsAndPingsInOrder_AndCountsThem()
        {
            var run = new ChallengeRun("challenge-001", 2);

            run.RecordPing();
            run.RecordTap(5);
            run.RecordTap(5);
            run.RecordTap(1);

            Assert.AreEqual(3, run.Taps);
            Assert.AreEqual(1, run.Pings);
            Assert.AreEqual(
                new[] { ChallengeMoveKind.Ping, ChallengeMoveKind.Tap, ChallengeMoveKind.Tap, ChallengeMoveKind.Tap },
                run.Moves.Select(m => m.Kind).ToArray());
            Assert.AreEqual(new[] { 5, 5, 1 }, run.Moves.Where(m => m.Kind == ChallengeMoveKind.Tap).Select(m => m.BatId).ToArray());
        }

        [Test]
        public void Seconds_AddUp_AndNeverGoBackwards()
        {
            var run = new ChallengeRun("challenge-001", 1);

            run.AddSeconds(1.5f);
            run.AddSeconds(0f);
            run.AddSeconds(2f);

            Assert.AreEqual(3.5f, run.ActiveSeconds, 1e-6f);
            Assert.Throws<ArgumentOutOfRangeException>(() => run.AddSeconds(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.AddSeconds(float.NaN));
        }

        [Test]
        public void Build_GivesTheAttemptThatWasPlayed()
        {
            var run = new ChallengeRun("challenge-001", 3);
            run.RecordTap(2);
            run.RecordPing();
            run.AddSeconds(12.5f);

            ChallengeAttempt attempt = run.Build(1700000000L);

            Assert.AreEqual("challenge-001", attempt.ChallengeId);
            Assert.AreEqual(3, attempt.Attempts);
            Assert.AreEqual(12.5f, attempt.ActiveSeconds, 1e-6f);
            Assert.AreEqual(1700000000L, attempt.SolvedAtUnixSeconds);
            Assert.AreEqual(2, attempt.Moves.Count);
        }

        [Test]
        public void AnAttempt_IsASnapshot_NotAViewOfTheRun()
        {
            var run = new ChallengeRun("challenge-001", 1);
            run.RecordTap(0);

            ChallengeAttempt attempt = run.Build(0);
            run.RecordTap(1);

            Assert.AreEqual(1, attempt.Moves.Count);
        }

        [Test]
        public void BadArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new ChallengeRun(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChallengeRun("x", 0));
        }
    }
}
