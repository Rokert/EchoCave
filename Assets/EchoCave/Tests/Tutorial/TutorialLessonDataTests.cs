using System.Collections.Generic;
using System.Linq;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Tests
{
    // The asset layer: a lesson survives being written into an asset and read back, and the catalog finds the problems a
    // script can have.
    public class TutorialLessonDataTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private TutorialLessonData DataFor(TutorialLesson lesson)
        {
            var data = ScriptableObject.CreateInstance<TutorialLessonData>();
            data.Configure(lesson);
            _created.Add(data);
            return data;
        }

        private TutorialCatalog CatalogOf(params TutorialLessonData[] lessons)
        {
            var catalog = ScriptableObject.CreateInstance<TutorialCatalog>();
            _created.Add(catalog);
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("lessons");
            list.arraySize = lessons.Length;
            for (int i = 0; i < lessons.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = lessons[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        [Test]
        public void EveryDefaultLesson_SurvivesTheTripThroughAnAsset()
        {
            foreach (TutorialLesson original in DefaultTutorial.Lessons())
            {
                TutorialLesson back = DataFor(original).ToLesson();

                Assert.AreEqual(original.Title, back.Title);
                Assert.AreEqual(original.FreeHint, back.FreeHint, original.Title);
                Assert.AreEqual(original.Level.Width, back.Level.Width);
                Assert.AreEqual(original.Level.Height, back.Level.Height);
                Assert.AreEqual(original.Level.MoveLimit, back.Level.MoveLimit);
                Assert.AreEqual(original.Level.PingBudget, back.Level.PingBudget);
                Assert.AreEqual(original.Level.Bats.Count, back.Level.Bats.Count);
                for (int i = 0; i < original.Level.Bats.Count; i++)
                {
                    Assert.AreEqual(original.Level.Bats[i].Cell, back.Level.Bats[i].Cell, original.Title);
                    Assert.AreEqual(original.Level.Bats[i].Direction, back.Level.Bats[i].Direction, original.Title);
                    Assert.AreEqual(original.Level.Bats[i].IsHidden, back.Level.Bats[i].IsHidden, original.Title);
                }

                Assert.AreEqual(original.Steps.Count, back.Steps.Count);
                for (int i = 0; i < original.Steps.Count; i++)
                {
                    Assert.AreEqual(original.Steps[i].Text, back.Steps[i].Text);
                    Assert.AreEqual(original.Steps[i].Advance, back.Steps[i].Advance);
                    Assert.AreEqual(original.Steps[i].Focus, back.Steps[i].Focus);
                    Assert.AreEqual(original.Steps[i].FocusBatId, back.Steps[i].FocusBatId);
                    Assert.AreEqual(original.Steps[i].LimitTaps, back.Steps[i].LimitTaps);
                }
            }
        }

        [Test]
        public void TheDefaultLessons_AsAssets_HaveNoProblems()
        {
            TutorialCatalog catalog = CatalogOf(DefaultTutorial.Lessons().Select(DataFor).ToArray());

            Assert.IsEmpty(catalog.FindProblems(), string.Join("\n", catalog.FindProblems()));
            Assert.AreEqual(5, catalog.ToLessons().Count);
        }

        [Test]
        public void AnEmptyEntry_IsReported()
        {
            TutorialCatalog catalog = CatalogOf(DataFor(DefaultTutorial.Lessons()[0]), null);

            Assert.IsTrue(catalog.FindProblems().Any(p => p.Contains("empty entry")));
        }

        [Test]
        public void ALessonThatCannotBeSolved_IsReported()
        {
            // Two bats facing each other on one row block each other for good.
            var deadlock = new TutorialLesson(
                "Deadlock",
                new LevelDefinition(
                    3, 1,
                    new[] { new BatDefinition(new Cell(0, 0), Direction.Right), new BatDefinition(new Cell(1, 0), Direction.Left) },
                    0, 0, 2, 2),
                new[] { new TutorialStep("Win.", TutorialAdvance.LevelWon) });

            TutorialCatalog catalog = CatalogOf(DataFor(deadlock));

            Assert.IsTrue(catalog.FindProblems().Any(p => p.Contains("cannot be solved")));
        }

        [Test]
        public void ABrokenScript_IsReportedByTheValidator()
        {
            var noWin = new TutorialLesson(
                "No win", DefaultTutorial.Lessons()[0].Level, new[] { new TutorialStep("Tap.", TutorialAdvance.BatTapped) });

            Assert.IsTrue(CatalogOf(DataFor(noWin)).FindProblems().Any(p => p.Contains("level being won")));
        }
    }
}
