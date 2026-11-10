using System;
using System.Collections.Generic;
using EchoCave.Core;
using EchoCave.Game;

namespace EchoCave.UI
{
    // Lets the challenge list talk to the game without depending on it.
    public sealed class GameChallengeSource : IChallengeSource
    {
        private readonly GameRoot _game;

        public GameChallengeSource(GameRoot game)
        {
            _game = game != null ? game : throw new ArgumentNullException(nameof(game));
        }

        public int Count => _game.Challenges.Count;

        public string IdOf(int index) => _game.Challenges[index].Id;

        public bool IsUnlocked(int index) => _game.IsChallengeUnlocked(index);

        public int LevelsLeftOf(int index) => _game.ChallengeLevelsLeft(index);

        public int UnlockedCount => _game.UnlockedChallengeCount;

        public int AttemptsOf(int index) => _game.ChallengeAttempts(IdOf(index));

        public IReadOnlyList<ChallengeResult> TopOf(int index, int count) => _game.Leaderboard.Top(IdOf(index), count);

        public void Play(int index) => _game.RequestChallenge(index);
    }
}
