using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class ChallengeSelfCheckTests
    {
        private static readonly MoveResolver Resolver = new MoveResolver();

        // Bat 1 leaves, bat 0 is blocked by it.
        private static readonly LevelDefinition Chain = new LevelDefinition(
            3, 1,
            new[] { new BatDefinition(new Cell(0, 0), Direction.Right, true), new BatDefinition(new Cell(1, 0), Direction.Right, true) },
            0, 1, 2, 2);

        private EventBus _bus;
        private readonly List<string> _info = new List<string>();
        private readonly List<string> _errors = new List<string>();
        private ChallengeSelfCheck _check;

        [SetUp]
        public void SetUp()
        {
            _info.Clear();
            _errors.Clear();
            _bus = new EventBus();
            _check = new ChallengeSelfCheck(
                _bus, new[] { new ChallengeEntry("challenge-001", Chain) }, Resolver, 0.25f, _info.Add, _errors.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _check.Dispose();
        }

        private static ChallengeAttempt Attempt(float seconds, params ChallengeMove[] moves)
        {
            return new ChallengeAttempt("challenge-001", moves, seconds, 2, 1700000000L);
        }

        [Test]
        public void AValidAttempt_IsReportedAsAccepted_WithItsNumbers()
        {
            _bus.Publish(new ChallengeSolved(0, "challenge-001", Attempt(10f, ChallengeMove.Ping(), ChallengeMove.Tap(1), ChallengeMove.Tap(0))));

            Assert.IsEmpty(_errors);
            Assert.AreEqual(1, _info.Count);
            StringAssert.Contains("2 taps", _info[0]);
            StringAssert.Contains("1 ping", _info[0]);
            StringAssert.Contains("The replay agrees", _info[0]);
        }

        [Test]
        public void AnAttemptTheReplayRejects_IsReportedAsAnError_WithTheReason()
        {
            _bus.Publish(new ChallengeSolved(0, "challenge-001", Attempt(10f, ChallengeMove.Tap(1))));

            Assert.IsEmpty(_info);
            Assert.AreEqual(1, _errors.Count);
            StringAssert.Contains("REJECTED", _errors[0]);
            StringAssert.Contains("NotSolved", _errors[0]);
        }

        [Test]
        public void ATimeFasterThanThePerTapMinimum_IsRejected()
        {
            _bus.Publish(new ChallengeSolved(0, "challenge-001", Attempt(0.1f, ChallengeMove.Tap(1), ChallengeMove.Tap(0))));

            StringAssert.Contains("ImpossibleTime", _errors[0]);
        }

        [Test]
        public void APositionThatIsNotInTheList_IsReported_NotThrown()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new ChallengeSolved(7, "challenge-007", Attempt(10f))));

            Assert.AreEqual(1, _errors.Count);
            StringAssert.Contains("no challenge at position 7", _errors[0]);
        }

        [Test]
        public void AfterDispose_NothingIsChecked()
        {
            _check.Dispose();

            _bus.Publish(new ChallengeSolved(0, "challenge-001", Attempt(10f, ChallengeMove.Tap(1), ChallengeMove.Tap(0))));

            Assert.IsEmpty(_info);
            Assert.IsEmpty(_errors);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            var entries = new[] { new ChallengeEntry("challenge-001", Chain) };

            Assert.Throws<ArgumentNullException>(() => new ChallengeSelfCheck(null, entries, Resolver, 0f, _info.Add, _errors.Add));
            Assert.Throws<ArgumentNullException>(() => new ChallengeSelfCheck(_bus, null, Resolver, 0f, _info.Add, _errors.Add));
            Assert.Throws<ArgumentNullException>(() => new ChallengeSelfCheck(_bus, entries, null, 0f, _info.Add, _errors.Add));
            Assert.Throws<ArgumentNullException>(() => new ChallengeSelfCheck(_bus, entries, Resolver, 0f, null, _errors.Add));
            Assert.Throws<ArgumentNullException>(() => new ChallengeSelfCheck(_bus, entries, Resolver, 0f, _info.Add, null));
        }
    }
}
