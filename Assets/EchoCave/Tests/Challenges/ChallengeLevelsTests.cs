using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    // Pins what challenge-001 is. If the generator changes and the recipe now gives another level, these fail on
    // purpose: a changed level would silently reset the meaning of every result on its leaderboard.
    public class ChallengeLevelsTests
    {
        private static readonly MoveResolver Resolver = new MoveResolver();

        [Test]
        public void Challenge001_IsA9x9Board_With40HiddenBats_OnePing_AndNoMoveLimit()
        {
            LevelDefinition level = ChallengeLevels.BuildChallenge001(Resolver);

            Assert.AreEqual(9, level.Width);
            Assert.AreEqual(9, level.Height);
            Assert.AreEqual(40, level.Bats.Count);
            Assert.IsTrue(level.Bats.All(b => b.IsHidden), "every bat is hidden");
            Assert.AreEqual(1, level.PingBudget);
            Assert.IsFalse(level.HasMoveLimit);
            Assert.IsEmpty(LevelValidator.Validate(level));
        }

        [Test]
        public void Challenge001_IsSolvable_InExactlyAsManyTapsAsBats_WithADeepChain()
        {
            LevelDefinition level = ChallengeLevels.BuildChallenge001(Resolver);

            SolveResult solved = new LevelSolver(Resolver).Solve(level);
            Assert.IsTrue(solved.IsSolvable);
            Assert.AreEqual(40, solved.MinMoves);
            Assert.AreEqual(20, solved.Waves.Count);

            Assert.IsTrue(LevelMetrics.TryMeasure(level, Resolver, out LevelMetrics metrics));
            Assert.AreEqual(32, metrics.BlockedAtStart);
        }

        [Test]
        public void Challenge001_IsVeryHardToPlayBlind()
        {
            LevelDefinition level = ChallengeLevels.BuildChallenge001(Resolver);

            Assert.IsTrue(ChallengeDifficulty.TryMeasure(level, Resolver, 3000, 5, out ChallengeDifficulty d));

            Assert.Greater(d.ExpectedBlindBounces, 20.0, "a blind player wastes more than 20 taps");
            Assert.Less(d.ChanceOfFlawlessBlindRun, 0.001, "a flawless run by luck is under 0.1 %");
            Assert.Greater(d.Score, 100.0);
        }

        [Test]
        public void TheRecipe_GivesTheSameLevelEveryTime()
        {
            LevelDefinition a = ChallengeLevels.BuildChallenge001(Resolver);
            LevelDefinition b = ChallengeLevels.BuildChallenge001(Resolver);

            CollectionAssert.AreEqual(
                a.Bats.Select(x => (x.Cell.X, x.Cell.Y, x.Direction, x.IsHidden)).ToArray(),
                b.Bats.Select(x => (x.Cell.X, x.Cell.Y, x.Direction, x.IsHidden)).ToArray());
        }

        [Test]
        public void TheLevel_DoesNotDependOnHowManyAttemptsTheGeneratorMayMake()
        {
            LevelDefinition capped = new LevelGenerator(Resolver).Generate(
                ChallengeLevels.Challenge001Spec, ChallengeLevels.Challenge001Seed, 0, 2000);
            LevelDefinition recipe = ChallengeLevels.BuildChallenge001(Resolver);

            CollectionAssert.AreEqual(
                capped.Bats.Select(x => (x.Cell.X, x.Cell.Y, x.Direction)).ToArray(),
                recipe.Bats.Select(x => (x.Cell.X, x.Cell.Y, x.Direction)).ToArray());
        }

        [Test]
        public void SameLevel_SeesAChangedBat_AChangedLimit_AndIgnoresTheStars()
        {
            LevelDefinition level = ChallengeLevels.BuildChallenge001(Resolver);
            var bats = level.Bats.ToList();

            var sameWithOtherStars = new LevelDefinition(9, 9, bats, 0, 1, 7, 9);
            Assert.IsTrue(ChallengeLevels.SameLevel(level, sameWithOtherStars));

            var turned = bats.ToList();
            turned[3] = new BatDefinition(turned[3].Cell, turned[3].Direction == Direction.Up ? Direction.Down : Direction.Up, turned[3].IsHidden);
            Assert.IsFalse(ChallengeLevels.SameLevel(level, new LevelDefinition(9, 9, turned, 0, 1, 40, 42)));

            var shown = bats.ToList();
            shown[0] = new BatDefinition(shown[0].Cell, shown[0].Direction, false);
            Assert.IsFalse(ChallengeLevels.SameLevel(level, new LevelDefinition(9, 9, shown, 0, 1, 40, 42)));

            Assert.IsFalse(ChallengeLevels.SameLevel(level, new LevelDefinition(9, 9, bats, 50, 1, 40, 42)), "move limit");
            Assert.IsFalse(ChallengeLevels.SameLevel(level, new LevelDefinition(9, 9, bats, 0, 2, 40, 42)), "pings");

            var swapped = bats.ToList();
            (swapped[0], swapped[1]) = (swapped[1], swapped[0]);
            Assert.IsFalse(ChallengeLevels.SameLevel(level, new LevelDefinition(9, 9, swapped, 0, 1, 40, 42)), "bat ids are positions");
        }

        [Test]
        public void ThePerfectOrder_Replays_AsAValidAttempt_OfExactlyTheMinimum()
        {
            LevelDefinition level = ChallengeLevels.BuildChallenge001(Resolver);
            SolveResult solved = new LevelSolver(Resolver).Solve(level);
            var moves = new System.Collections.Generic.List<ChallengeMove> { ChallengeMove.Ping() };
            moves.AddRange(solved.Solution.Select(ChallengeMove.Tap));

            ReplayResult result = ChallengeReplay.Verify(
                level, new ChallengeAttempt(ChallengeLevels.Challenge001Id, moves, 60f, 1, 0), Resolver, 0.25f);

            Assert.IsTrue(result.IsValid, result.Message);
            Assert.AreEqual(40, result.Moves);
            Assert.AreEqual(1, result.Pings);
        }

        // The numbers of challenge-002 and challenge-003 as they were found (docs/CHALLENGES.md). A different generator
        // would give different levels under the same ids, so these fail on purpose.
        [TestCase(ChallengeLevels.Challenge002Id, 9, 48, 1, 18, 39, 25.0)]
        [TestCase(ChallengeLevels.Challenge003Id, 8, 36, 0, 20, 30, 20.0)]
        public void TheOtherChallenges_AreThePinnedLevels_AndVeryHardToPlayBlind(
            string id, int size, int bats, int pings, int waves, int blockedAtStart, double minBlindBounces)
        {
            LevelDefinition level = ChallengeLevels.Build(id, Resolver);

            Assert.AreEqual(size, level.Width);
            Assert.AreEqual(size, level.Height);
            Assert.AreEqual(bats, level.Bats.Count);
            Assert.IsTrue(level.Bats.All(b => b.IsHidden), "every bat is hidden");
            Assert.AreEqual(pings, level.PingBudget);
            Assert.IsFalse(level.HasMoveLimit);
            Assert.IsEmpty(LevelValidator.Validate(level));

            SolveResult solved = new LevelSolver(Resolver).Solve(level);
            Assert.IsTrue(solved.IsSolvable);
            Assert.AreEqual(bats, solved.MinMoves);
            Assert.AreEqual(waves, solved.Waves.Count);
            Assert.IsTrue(LevelMetrics.TryMeasure(level, Resolver, out LevelMetrics metrics));
            Assert.AreEqual(blockedAtStart, metrics.BlockedAtStart);

            Assert.IsTrue(ChallengeDifficulty.TryMeasure(level, Resolver, 3000, 5, out ChallengeDifficulty d));
            Assert.Greater(d.ExpectedBlindBounces, minBlindBounces);
            Assert.Less(d.ChanceOfFlawlessBlindRun, 0.001);
        }

        [Test]
        public void EveryChallenge_IsADifferentLevel()
        {
            var levels = ChallengeLevels.AllIds.Select(id => ChallengeLevels.Build(id, Resolver)).ToArray();

            for (int i = 0; i < levels.Length; i++)
            for (int j = i + 1; j < levels.Length; j++)
                Assert.IsFalse(ChallengeLevels.SameLevel(levels[i], levels[j]), $"{ChallengeLevels.AllIds[i]} and {ChallengeLevels.AllIds[j]}");

        }

        [Test]
        public void AnUnknownChallengeId_IsRejected()
        {
            Assert.Throws<System.ArgumentException>(() => ChallengeLevels.Build("challenge-999", Resolver));
            Assert.Throws<System.ArgumentException>(() => ChallengeLevels.SpecOf("challenge-999"));
            Assert.Throws<System.ArgumentException>(() => ChallengeLevels.SeedOf("challenge-999"));
        }

        [Test]
        public void ThePerfectOrder_OfEveryChallenge_ReplaysAsAValidAttempt()
        {
            foreach (string id in ChallengeLevels.AllIds)
            {
                LevelDefinition level = ChallengeLevels.Build(id, Resolver);
                SolveResult solved = new LevelSolver(Resolver).Solve(level);
                var moves = new System.Collections.Generic.List<ChallengeMove>();
                if (level.PingBudget > 0) moves.Add(ChallengeMove.Ping());
                moves.AddRange(solved.Solution.Select(ChallengeMove.Tap));

                ReplayResult result = ChallengeReplay.Verify(
                    level, new ChallengeAttempt(id, moves, 90f, 1, 0), Resolver, 0.25f);

                Assert.IsTrue(result.IsValid, id + ": " + result.Message);
                Assert.AreEqual(level.Bats.Count, result.Moves, id);
            }
        }
    }
}
