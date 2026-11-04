using System.Collections.Generic;
using EchoCave.Core;
using UnityEngine;

namespace EchoCave.Game
{
    // The tutorial lessons, in teaching order.
    [CreateAssetMenu(fileName = "TutorialCatalog", menuName = "EchoCave/Tutorial Catalog")]
    public sealed class TutorialCatalog : ScriptableObject
    {
        [SerializeField] private List<TutorialLessonData> lessons = new List<TutorialLessonData>();

        public int Count => lessons.Count;

        // One message per problem: structure first (TutorialValidator), then that every lesson can be won within its limit.
        // Used by the editor menu and by the test of the shipped assets.
        public IReadOnlyList<string> FindProblems()
        {
            var problems = new List<string>();
            IReadOnlyList<TutorialLesson> built = ToLessons();

            if (built.Count != lessons.Count) problems.Add("The catalog has an empty entry.");
            problems.AddRange(TutorialValidator.Validate(built));

            var solver = new LevelSolver(new MoveResolver());
            for (int i = 0; i < built.Count; i++)
            {
                if (LevelValidator.Validate(built[i].Level).Count > 0) continue;

                SolveResult result = solver.Solve(built[i].Level);
                if (!result.IsSolvable) problems.Add($"Lesson {i + 1} cannot be solved.");
                else if (!result.FitsMoveLimit) problems.Add($"Lesson {i + 1} needs more moves than its limit allows.");
            }

            return problems;
        }

        // A missing entry in the list is skipped here and reported by FindProblems and the shipped test.
        public IReadOnlyList<TutorialLesson> ToLessons()
        {
            var result = new List<TutorialLesson>(lessons.Count);
            foreach (TutorialLessonData lesson in lessons)
            {
                if (lesson != null) result.Add(lesson.ToLesson());
            }

            return result;
        }
    }
}
