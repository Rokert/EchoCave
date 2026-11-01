using System.Collections.Generic;

namespace EchoCave.Core
{
    // The rules for the ids that identify levels in saved progress. An id is assigned once, never changes
    // when a level is moved in the catalog, and is never reused.
    public static class LevelIdRules
    {
        // One message per problem; empty when the ids are usable.
        public static IReadOnlyList<string> Validate(IReadOnlyList<int> ids)
        {
            var errors = new List<string>();
            var seen = new HashSet<int>();

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] < 1) errors.Add($"Level at position {i + 1} has id {ids[i]}; ids start at 1.");
                else if (!seen.Add(ids[i])) errors.Add($"Level at position {i + 1} repeats id {ids[i]}.");
            }

            return errors;
        }

        // One above the highest id in use, so a new level never takes an id a saved game may still know.
        public static int NextFree(IEnumerable<int> idsInUse)
        {
            int highest = 0;
            foreach (int id in idsInUse)
                if (id > highest) highest = id;

            return highest + 1;
        }
    }
}
