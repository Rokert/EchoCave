using System;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LevelSolverTests
    {
        private LevelSolver _solver;

        [SetUp]
        public void SetUp()
        {
            _solver = new LevelSolver(new MoveResolver());
        }

        private static LevelDefinition Level(int moveLimit, params BatDefinition[] bats)
        {
            return new LevelDefinition(5, 5, bats, moveLimit, pingBudget: 0, threeStarMoves: 1, twoStarMoves: 1);
        }

        private static BatDefinition Bat(int x, int y, Direction direction, bool hidden = false)
        {
            return new BatDefinition(new Cell(x, y), direction, hidden);
        }

        // Replays a solution with the real move machinery: every tap must exit and the board must end empty.
        private static void AssertSolutionClearsTheBoard(LevelDefinition level, SolveResult result)
        {
            LevelSession session = LevelBuilder.Build(level);
            var resolver = new MoveResolver();

            foreach (int id in result.Solution)
            {
                var command = new MoveCommand(session, resolver, id);
                command.Execute();
                Assert.AreEqual(MoveOutcome.Exited, command.Result.Outcome, $"Bat {id} did not exit.");
            }

            Assert.AreEqual(0, session.Board.Count);
            Assert.AreEqual(result.Solution.Count, session.MovesUsed);
        }

        [Test]
        public void IndependentBats_AreOneWave_AndMinMovesIsTheBatCount()
        {
            var level = Level(0, Bat(0, 0, Direction.Left), Bat(2, 2, Direction.Up), Bat(4, 4, Direction.Right));

            SolveResult result = _solver.Solve(level);

            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(3, result.MinMoves);
            Assert.AreEqual(1, result.Waves.Count);
            Assert.IsEmpty(result.DeadlockedBatIds);
            AssertSolutionClearsTheBoard(level, result);
        }

        [Test]
        public void BlockingChain_ProducesOneWavePerLink()
        {
            // All on row 2: bat 0 is blocked by bat 1, bat 1 by bat 2, and bat 2 flies up freely.
            var level = Level(0,
                Bat(0, 2, Direction.Right),
                Bat(2, 2, Direction.Right),
                Bat(4, 2, Direction.Up));

            SolveResult result = _solver.Solve(level);

            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(new[] { 2, 1, 0 }, result.Solution.ToArray());
            Assert.AreEqual(3, result.Waves.Count);
            AssertSolutionClearsTheBoard(level, result);
        }

        [Test]
        public void FacingBats_BlockEachOther_AndAreReportedAsDeadlocked()
        {
            var level = Level(0, Bat(1, 0, Direction.Right), Bat(2, 0, Direction.Left), Bat(4, 4, Direction.Up));

            SolveResult result = _solver.Solve(level);

            Assert.IsFalse(result.IsSolvable);
            Assert.AreEqual(-1, result.MinMoves);
            Assert.AreEqual(new[] { 0, 1 }, result.DeadlockedBatIds.ToArray());
            Assert.AreEqual(new[] { 2 }, result.Solution.ToArray());
            Assert.IsFalse(result.FitsMoveLimit);
        }

        [Test]
        public void HiddenBats_AreSolvedLikeVisibleOnes()
        {
            var level = Level(0, Bat(0, 2, Direction.Right, hidden: true), Bat(3, 2, Direction.Up, hidden: true));

            SolveResult result = _solver.Solve(level);

            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(new[] { 1, 0 }, result.Solution.ToArray());
            AssertSolutionClearsTheBoard(level, result);
        }

        [Test]
        public void MoveLimit_BelowMinimum_DoesNotFit()
        {
            var level = Level(1, Bat(0, 0, Direction.Left), Bat(2, 2, Direction.Up));

            SolveResult result = _solver.Solve(level);

            Assert.IsTrue(result.IsSolvable);
            Assert.IsFalse(result.FitsMoveLimit);
        }

        [Test]
        public void MoveLimit_AtOrAboveMinimum_Fits()
        {
            var level = Level(2, Bat(0, 0, Direction.Left), Bat(2, 2, Direction.Up));

            Assert.IsTrue(_solver.Solve(level).FitsMoveLimit);
        }

        [Test]
        public void NoMoveLimit_AlwaysFitsWhenSolvable()
        {
            Assert.IsTrue(_solver.Solve(Level(0, Bat(0, 0, Direction.Left))).FitsMoveLimit);
        }

        [Test]
        public void Solve_IsRepeatableOnTheSameDefinition()
        {
            var level = Level(0, Bat(0, 2, Direction.Right), Bat(3, 2, Direction.Up));

            SolveResult first = _solver.Solve(level);
            SolveResult second = _solver.Solve(level);

            Assert.AreEqual(first.Solution.ToArray(), second.Solution.ToArray());
        }

        [Test]
        public void InvalidLevel_Throws()
        {
            Assert.Throws<InvalidLevelException>(() => _solver.Solve(Level(0)));
        }

        [Test]
        public void Constructor_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new LevelSolver(null));
        }
    }

    public class StarThresholdsTests
    {
        [Test]
        public void Suggest_ThreeStarsAtMinimum_TwoStarsWithSlack()
        {
            StarThresholds result = StarThresholds.Suggest(minMoves: 5, moveLimit: 0, twoStarSlack: 2);

            Assert.AreEqual((5, 7), (result.ThreeStarMoves, result.TwoStarMoves));
        }

        [Test]
        public void Suggest_NeverExceedsTheMoveLimit()
        {
            StarThresholds result = StarThresholds.Suggest(minMoves: 5, moveLimit: 6, twoStarSlack: 3);

            Assert.AreEqual((5, 6), (result.ThreeStarMoves, result.TwoStarMoves));
        }

        [Test]
        public void Suggest_KeepsTwoStarsAtOrAboveThreeStars_EvenWithTinyLimit()
        {
            StarThresholds result = StarThresholds.Suggest(minMoves: 5, moveLimit: 3, twoStarSlack: 2);

            Assert.GreaterOrEqual(result.TwoStarMoves, result.ThreeStarMoves);
        }

        [Test]
        public void Suggest_ClampsNegativeSlackAndZeroMinimum()
        {
            StarThresholds result = StarThresholds.Suggest(minMoves: 0, moveLimit: 0, twoStarSlack: -4);

            Assert.AreEqual((1, 1), (result.ThreeStarMoves, result.TwoStarMoves));
        }
    }
}
