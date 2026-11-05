using System;
using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using NUnit.Framework;

namespace EchoCave.Tests
{
    public class ChallengeCatalogRulesTests
    {
        private static readonly MoveResolver Resolver = new MoveResolver();

        private static LevelDefinition Good(int moveLimit = 0)
        {
            return new LevelDefinition(3, 1, new[] { new BatDefinition(new Cell(1, 0), Direction.Right, true) }, moveLimit, 1, 1, 1);
        }

        private static IReadOnlyList<string> Validate(string[] ids, params LevelDefinition[] levels)
        {
            return ChallengeCatalogRules.Validate(ids, levels, Resolver);
        }

        [Test]
        public void AGoodCatalog_HasNoProblems()
        {
            Assert.IsEmpty(Validate(new[] { "challenge-001", "weekly_2" }, Good(), Good(5)));
        }

        [Test]
        public void IdsAreLowercaseLettersDigitsDashAndUnderscore_UpTo64_NotStartingWithADashOrUnderscore()
        {
            foreach (string good in new[] { "a", "challenge-001", "w_2", "0", new string('a', 64) })
                Assert.IsTrue(ChallengeCatalogRules.IsValidId(good), good);

            foreach (string bad in new[] { null, "", "A", "has space", "a/b", "../x", "-x", "_x", "x.y", "ñ", new string('a', 65) })
                Assert.IsFalse(ChallengeCatalogRules.IsValidId(bad), bad ?? "null");
        }

        [Test]
        public void ABadOrRepeatedId_IsReported_WithItsPosition()
        {
            IReadOnlyList<string> errors = Validate(new[] { "Bad Id", "a", "a" }, Good(), Good(), Good());

            Assert.AreEqual(2, errors.Count);
            StringAssert.Contains("Challenge 1", errors[0]);
            StringAssert.Contains("not valid", errors[0]);
            StringAssert.Contains("Challenge 3", errors[1]);
            StringAssert.Contains("repeats", errors[1]);
        }

        [Test]
        public void ALevelThatIsMissing_Invalid_Unsolvable_OrBelowItsMinimum_IsReported()
        {
            var empty = new LevelDefinition(3, 1, new BatDefinition[0], 0, 0, 1, 1);
            var deadlock = new LevelDefinition(
                2, 1, new[] { new BatDefinition(new Cell(0, 0), Direction.Right), new BatDefinition(new Cell(1, 0), Direction.Left) }, 0, 0, 1, 1);
            var twoBatsOneMove = new LevelDefinition(
                3, 1, new[] { new BatDefinition(new Cell(0, 0), Direction.Left), new BatDefinition(new Cell(2, 0), Direction.Right) }, 1, 0, 1, 1);

            IReadOnlyList<string> errors = Validate(new[] { "a", "b", "c", "d" }, null, empty, deadlock, twoBatsOneMove);

            Assert.AreEqual(4, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("no level", errors[0]);
            StringAssert.Contains("invalid level", errors[1]);
            StringAssert.Contains("cannot be solved", errors[2]);
            StringAssert.Contains("move limit below its minimum of 2", errors[3]);
        }

        [Test]
        public void ADifferentNumberOfIdsAndLevels_IsReported()
        {
            Assert.AreEqual(1, Validate(new[] { "a", "b" }, Good()).Count);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ChallengeCatalogRules.Validate(null, new List<LevelDefinition>(), Resolver));
            Assert.Throws<ArgumentNullException>(() => ChallengeCatalogRules.Validate(new List<string>(), null, Resolver));
            Assert.Throws<ArgumentNullException>(() => ChallengeCatalogRules.Validate(new List<string>(), new List<LevelDefinition>(), null));
        }

        [Test]
        public void TheShippedRecipe_PassesTheRules()
        {
            LevelDefinition level = ChallengeLevels.BuildChallenge001(Resolver);

            Assert.IsEmpty(Validate(new[] { ChallengeLevels.Challenge001Id }, level));
        }
    }
}
