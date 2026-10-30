using System;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using UnityEngine;

namespace EchoCave.Tests
{
    public class MovePathPlannerTests
    {
        private const float Tolerance = 1e-4f;
        private const float Overshoot = 1.5f;
        private const float Bump = 0.2f;

        private static void AssertAt(float x, float y, Vector2 actual)
        {
            Assert.AreEqual(x, actual.x, Tolerance, "x");
            Assert.AreEqual(y, actual.y, Tolerance, "y");
        }

        private static Bat At(int id, int x, int y, Direction direction) => new Bat(id, new Cell(x, y), direction);

        [Test]
        public void Exit_FliesPastTheEdgeInTheBatDirection()
        {
            var layout = new GridLayout2D(5, 5, 1f);
            Bat bat = At(1, 2, 2, Direction.Up);

            MovePlan plan = MovePathPlanner.Plan(MoveResult.Exit(bat, new Cell(2, 4)), layout, Overshoot, Bump);

            Assert.AreEqual(MoveOutcome.Exited, plan.Outcome);
            AssertAt(0f, 0f, plan.Start);
            // Last cell inside is (2,4) at y=2; one cell to the edge plus 1.5 of overshoot.
            AssertAt(0f, 4.5f, plan.Target);
            Assert.AreEqual(4.5f, plan.TravelCells, Tolerance);
        }

        [TestCase(Direction.Up, 0f, 1f)]
        [TestCase(Direction.Down, 0f, -1f)]
        [TestCase(Direction.Left, -1f, 0f)]
        [TestCase(Direction.Right, 1f, 0f)]
        public void Exit_TargetLiesAlongTheDirection(Direction direction, float dx, float dy)
        {
            var layout = new GridLayout2D(3, 3, 1f);
            Bat bat = At(1, 1, 1, direction);
            Cell edge = new Cell(1 + (int)dx, 1 + (int)dy);

            MovePlan plan = MovePathPlanner.Plan(MoveResult.Exit(bat, edge), layout, Overshoot, Bump);

            Vector2 travelled = plan.Target - plan.Start;
            Assert.AreEqual(dx * 3.5f, travelled.x, Tolerance);
            Assert.AreEqual(dy * 3.5f, travelled.y, Tolerance);
        }

        [Test]
        public void Exit_ScalesWithCellSize()
        {
            var layout = new GridLayout2D(5, 5, 2f);
            Bat bat = At(1, 2, 2, Direction.Right);

            MovePlan plan = MovePathPlanner.Plan(MoveResult.Exit(bat, new Cell(4, 2)), layout, Overshoot, Bump);

            AssertAt(9f, 0f, plan.Target);
            Assert.AreEqual(4.5f, plan.TravelCells, Tolerance, "Travel is measured in cells, not world units.");
        }

        [Test]
        public void Bounce_StopsInFrontOfTheBlocker_AndBumpsIntoIt()
        {
            var layout = new GridLayout2D(5, 5, 1f);
            Bat bat = At(1, 0, 0, Direction.Right);
            Bat blocker = At(2, 3, 0, Direction.Up);

            MovePlan plan = MovePathPlanner.Plan(MoveResult.Bounce(bat, new Cell(2, 0), blocker), layout, Overshoot, Bump);

            Assert.AreEqual(MoveOutcome.Bounced, plan.Outcome);
            AssertAt(-2f, -2f, plan.Start);
            AssertAt(0f, -2f, plan.Target);
            AssertAt(0.2f, -2f, plan.Bump);
            Assert.AreEqual(2f, plan.TravelCells, Tolerance);
        }

        [Test]
        public void Bounce_WithAnAdjacentBlocker_DoesNotTravel()
        {
            var layout = new GridLayout2D(5, 5, 1f);
            Bat bat = At(1, 1, 1, Direction.Left);
            Bat blocker = At(2, 0, 1, Direction.Up);

            MovePlan plan = MovePathPlanner.Plan(MoveResult.Bounce(bat, new Cell(1, 1), blocker), layout, Overshoot, Bump);

            Assert.AreEqual(plan.Start, plan.Target);
            Assert.AreEqual(0f, plan.TravelCells, Tolerance);
            AssertAt(plan.Start.x - 0.2f, plan.Start.y, plan.Bump);
        }

        [Test]
        public void Plan_RejectsAnEmptyResultAndANullLayout()
        {
            var layout = new GridLayout2D(3, 3, 1f);
            Bat bat = At(1, 0, 0, Direction.Up);

            Assert.Throws<ArgumentException>(() => MovePathPlanner.Plan(default, layout, Overshoot, Bump));
            Assert.Throws<ArgumentNullException>(() => MovePathPlanner.Plan(MoveResult.Exit(bat, new Cell(0, 2)), null, Overshoot, Bump));
        }
    }

    public class CameraFramingTests
    {
        [Test]
        public void OrthographicSize_PortraitScreen_IsLimitedByTheBoardWidth()
        {
            // 5x5 board with 1 of margin: half width 3.5 has to fit in half the screen width.
            float size = CameraFraming.OrthographicSize(new Vector2(5f, 5f), 0.5f, 1f);

            Assert.AreEqual(7f, size, 1e-4f);
        }

        [Test]
        public void OrthographicSize_LandscapeScreen_IsLimitedByTheBoardHeight()
        {
            float size = CameraFraming.OrthographicSize(new Vector2(5f, 5f), 2f, 1f);

            Assert.AreEqual(3.5f, size, 1e-4f);
        }

        [Test]
        public void OrthographicSize_AlwaysShowsTheWholeBoard()
        {
            var board = new Vector2(7f, 4f);

            foreach (float aspect in new[] { 0.45f, 0.75f, 1f, 1.78f, 2.4f })
            {
                float size = CameraFraming.OrthographicSize(board, aspect, 0.5f);

                Assert.GreaterOrEqual(size * 2f, board.y + 1f, $"height at aspect {aspect}");
                Assert.GreaterOrEqual(size * 2f * aspect, board.x + 1f, $"width at aspect {aspect}");
            }
        }

        [Test]
        public void OrthographicSize_RejectsANonPositiveAspect()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CameraFraming.OrthographicSize(Vector2.one, 0f, 0f));
        }

        [Test]
        public void CoverScale_CropsTheLongerSideInsteadOfLeavingGaps()
        {
            Assert.AreEqual(2f, CameraFraming.CoverScale(new Vector2(4f, 2f), new Vector2(8f, 2f)), 1e-4f);
            Assert.AreEqual(4f, CameraFraming.CoverScale(new Vector2(4f, 2f), new Vector2(4f, 8f)), 1e-4f);
        }

        [Test]
        public void CoverScale_RejectsAnEmptySprite()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CameraFraming.CoverScale(Vector2.zero, Vector2.one));
        }
    }
}
