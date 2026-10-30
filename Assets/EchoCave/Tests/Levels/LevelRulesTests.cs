using System;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class LevelRulesTests
    {
        // Row of three cells. Bat 0 is blocked by bat 1; bat 1 leaves the board freely.
        private static LevelDefinition Level(int moveLimit)
        {
            int two = moveLimit == 0 ? 2 : moveLimit;
            return new LevelDefinition(
                3,
                1,
                new[]
                {
                    new BatDefinition(new Cell(0, 0), Direction.Right, false),
                    new BatDefinition(new Cell(1, 0), Direction.Right, false)
                },
                moveLimit,
                0,
                2,
                two);
        }

        private static void Tap(LevelSession session, int batId)
        {
            new MoveCommand(session, new MoveResolver(), batId).Execute();
        }

        [Test]
        public void NewLevel_IsInProgress()
        {
            LevelDefinition level = Level(2);

            Assert.AreEqual(LevelStatus.InProgress, LevelRules.Evaluate(LevelBuilder.Build(level), level));
        }

        [Test]
        public void EmptyBoard_IsWon()
        {
            LevelDefinition level = Level(0);
            LevelSession session = LevelBuilder.Build(level);

            Tap(session, 1);
            Tap(session, 0);

            Assert.AreEqual(0, session.Board.Count);
            Assert.AreEqual(LevelStatus.Won, LevelRules.Evaluate(session, level));
        }

        [Test]
        public void LimitUsedUpWithBatsLeft_IsLost()
        {
            LevelDefinition level = Level(2);
            LevelSession session = LevelBuilder.Build(level);

            Tap(session, 0);   // bounces, wasted
            Tap(session, 1);   // exits, but bat 0 is still there

            Assert.AreEqual(1, session.Board.Count);
            Assert.AreEqual(LevelStatus.Lost, LevelRules.Evaluate(session, level));
        }

        [Test]
        public void MovesLeft_IsStillInProgress()
        {
            LevelDefinition level = Level(2);
            LevelSession session = LevelBuilder.Build(level);

            Tap(session, 0);

            Assert.AreEqual(1, session.MovesUsed);
            Assert.AreEqual(LevelStatus.InProgress, LevelRules.Evaluate(session, level));
        }

        [Test]
        public void ClearingTheBoardOnTheLastAllowedMove_IsWon()
        {
            LevelDefinition level = Level(2);
            LevelSession session = LevelBuilder.Build(level);

            Tap(session, 1);
            Tap(session, 0);

            Assert.AreEqual(2, session.MovesUsed);
            Assert.AreEqual(LevelStatus.Won, LevelRules.Evaluate(session, level));
        }

        [Test]
        public void WithoutALimit_WastedMovesNeverLose()
        {
            LevelDefinition level = Level(0);
            LevelSession session = LevelBuilder.Build(level);

            for (int i = 0; i < 10; i++) Tap(session, 0);   // bounces every time

            Assert.AreEqual(10, session.MovesUsed);
            Assert.AreEqual(LevelStatus.InProgress, LevelRules.Evaluate(session, level));
        }

        [Test]
        public void Evaluate_RejectsNulls()
        {
            LevelDefinition level = Level(2);

            Assert.Throws<ArgumentNullException>(() => LevelRules.Evaluate(null, level));
            Assert.Throws<ArgumentNullException>(() => LevelRules.Evaluate(LevelBuilder.Build(level), null));
        }
    }
}
