using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using EchoCave.Game;
using EchoCave.UI;
using NUnit.Framework;
using Rokert.Core;

namespace EchoCave.Tests
{
    public class ChallengePopupsPresenterTests
    {
        private sealed class FakeSource : IChallengeSource
        {
            public int Count { get; set; } = 2;
            public bool Unlocked { get; set; } = true;
            public int LevelsLeft { get; set; }
            public readonly Dictionary<int, int> Attempts = new Dictionary<int, int>();
            public readonly Dictionary<int, List<ChallengeResult>> Top = new Dictionary<int, List<ChallengeResult>>();
            public readonly List<int> Played = new List<int>();

            public string IdOf(int index) => "challenge-" + (index + 1).ToString("000");

            public int AttemptsOf(int index) => Attempts.TryGetValue(index, out int n) ? n : 0;

            public IReadOnlyList<ChallengeResult> TopOf(int index, int count)
            {
                return Top.TryGetValue(index, out List<ChallengeResult> list) ? list.Take(count).ToList() : new List<ChallengeResult>();
            }

            public void Play(int index) => Played.Add(index);
        }

        private FakePopupsView _view;
        private FakePopupCommands _commands;
        private FakeSource _source;
        private EventBus _bus;
        private PopupsPresenter _presenter;
        private int _closed;

        [SetUp]
        public void SetUp()
        {
            _view = new FakePopupsView();
            _commands = new FakePopupCommands();
            _source = new FakeSource();
            _bus = new EventBus();
            _closed = 0;
            _presenter = new PopupsPresenter(_view, _bus, _commands, challenges: _source, challengesClosed: () => _closed++);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        private static ChallengeResult Result(int moves, float seconds, int pings = 1) => new ChallengeResult(moves, seconds, pings, 1700000000L);

        [Test]
        public void TheList_ShowsEachChallenge_UnsolvedOrNotTried()
        {
            _source.Attempts[0] = 3;

            _presenter.OpenChallenges();

            Assert.AreEqual(ShownPopup.ChallengeList, _view.Shown);
            Assert.AreEqual(2, _view.Rows.Count);
            Assert.AreEqual("challenge-001", _view.Rows[0].Title);
            Assert.AreEqual("Not solved yet (3 tries)", _view.Rows[0].Summary);
            Assert.AreEqual("Not tried yet", _view.Rows[1].Summary);
            Assert.IsTrue(_view.Rows[0].CanPlay);
            Assert.IsEmpty(_view.Message);
        }

        [Test]
        public void ASolvedChallenge_ShowsItsBest_ItsTries_AndTheTopResultsNumbered()
        {
            _source.Attempts[0] = 1;
            _source.Top[0] = new List<ChallengeResult> { Result(45, 200f), Result(52, 140.5f), Result(60, 90f), Result(70, 10f) };

            _presenter.OpenChallenges();

            ChallengeRowData row = _view.Rows[0];
            Assert.AreEqual("Best: 45 taps in 3:20.0 (1 try)", row.Summary);
            CollectionAssert.AreEqual(new[] { "1. 45 taps in 3:20.0", "2. 52 taps in 2:20.5", "3. 60 taps in 1:30.0" }, row.Lines);
        }

        [Test]
        public void LockedChallenges_SayWhy_AndCannotBePlayed()
        {
            _source.Unlocked = false;
            _source.LevelsLeft = 4;

            _presenter.OpenChallenges();

            Assert.AreEqual("Win 4 more levels to unlock", _view.Message);
            Assert.IsFalse(_view.Rows[0].CanPlay);

            _view.PressChallenge(0);
            Assert.IsEmpty(_source.Played);
            Assert.AreEqual(ShownPopup.ChallengeList, _view.Shown, "the list stays open");
        }

        [Test]
        public void PlayingAChallenge_ClosesThePopup_AndStartsIt()
        {
            _presenter.OpenChallenges();

            _view.PressChallenge(1);

            Assert.AreEqual(ShownPopup.None, _view.Shown);
            CollectionAssert.AreEqual(new[] { 1 }, _source.Played);
        }

        [Test]
        public void APositionOutOfRange_IsIgnored()
        {
            _view.PressChallenge(5);
            _view.PressChallenge(-1);

            Assert.IsEmpty(_source.Played);
        }

        [Test]
        public void TheBackButton_ClosesTheList_AndTellsTheMenuToComeBack()
        {
            _presenter.OpenChallenges();

            _view.Press(PopupAction.CloseChallenges);

            Assert.AreEqual(ShownPopup.None, _view.Shown);
            Assert.AreEqual(1, _closed);
        }

        [Test]
        public void WithoutAChallengeSource_OpeningDoesNothing()
        {
            using (var plain = new PopupsPresenter(_view, _bus, _commands))
            {
                _view.Hide();

                plain.OpenChallenges();

                Assert.AreEqual(ShownPopup.None, _view.Shown);
            }
        }

        [Test]
        public void ASavedResult_ShowsHowItWent_AndWhereItRanks()
        {
            _bus.Publish(new ChallengeResultSaved("challenge-001", 4, Result(52, 140.5f, pings: 1), 2, false));

            Assert.AreEqual(ShownPopup.ChallengeResult, _view.Shown);
            Assert.AreEqual("Challenge solved", _view.ResultTitle);
            CollectionAssert.AreEqual(
                new[] { "52 taps in 2:20.5", "1 ping used", "4 tries so far", "Rank 2 on this device" }, _view.ResultLines);
        }

        [Test]
        public void ANewBest_SaysSo()
        {
            _bus.Publish(new ChallengeResultSaved("challenge-001", 1, Result(52, 140.5f), 1, true));

            Assert.AreEqual("New best!", _view.ResultTitle);
        }

        [Test]
        public void ARunTheReplayRejected_IsExplained_SoThePlayerIsNotLeftOnAnEmptyBoard()
        {
            var attempt = new ChallengeAttempt("challenge-001", new ChallengeMove[0], 1f, 1, 0);
            _bus.Publish(new ChallengeVerified(0, "challenge-001", attempt, ReplayResult.Rejected(ReplayError.ImpossibleTime, "too fast")));

            Assert.AreEqual(ShownPopup.ChallengeResult, _view.Shown);
            Assert.AreEqual("Not counted", _view.ResultTitle);
            StringAssert.Contains("ImpossibleTime", string.Join("\n", _view.ResultLines));
        }

        [Test]
        public void AVerifiedRunThatIsValid_ShowsNothingYet_TheSavedResultDoes()
        {
            var attempt = new ChallengeAttempt("challenge-001", new ChallengeMove[0], 1f, 1, 0);
            _bus.Publish(new ChallengeVerified(0, "challenge-001", attempt, new ReplayResult(ReplayError.None, string.Empty, 5, 1)));

            Assert.AreEqual(ShownPopup.None, _view.Shown);
        }

        [Test]
        public void AChallengeThatRanOutOfMoves_ShowsTheLostPopup_WithItsLimit()
        {
            _bus.Publish(new ChallengeStarted(0, 1, "challenge-001", 12, 1, 1));

            _bus.Publish(new ChallengeFailed(0, "challenge-001", 12));

            Assert.AreEqual(ShownPopup.Lost, _view.Shown);
            Assert.AreEqual(12, _view.MovesUsed);
            Assert.AreEqual(12, _view.MoveLimit);
        }

        [Test]
        public void TheResultAndLostPopups_RetryAndMenu_GoThroughTheUsualCommands()
        {
            _bus.Publish(new ChallengeResultSaved("challenge-001", 1, Result(52, 10f), 1, true));

            _view.Press(PopupAction.Retry);
            _view.Press(PopupAction.Menu);

            Assert.AreEqual(1, _commands.Retries);
            Assert.AreEqual(1, _commands.Menus);
        }

        [Test]
        public void ANewChallenge_ClosesTheResultPopup()
        {
            _bus.Publish(new ChallengeResultSaved("challenge-001", 1, Result(52, 10f), 1, true));

            _bus.Publish(new ChallengeStarted(0, 1, "challenge-001", 0, 1, 2));

            Assert.AreEqual(ShownPopup.None, _view.Shown);
        }

        [Test]
        public void AfterDispose_ChallengeEventsAreIgnored()
        {
            _presenter.Dispose();
            _view.Hide();

            _bus.Publish(new ChallengeResultSaved("challenge-001", 1, Result(52, 10f), 1, true));

            Assert.AreEqual(ShownPopup.None, _view.Shown);
        }
    }
}
