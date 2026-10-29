using System;
using System.Collections.Generic;

namespace EchoCave.Core
{
    public sealed class InvalidLevelException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public InvalidLevelException(IReadOnlyList<string> errors)
            : base("Invalid level:" + Environment.NewLine + string.Join(Environment.NewLine, errors))
        {
            Errors = errors;
        }
    }

    public static class LevelBuilder
    {
        // Bat ids are the indices of the definition's bat list.
        public static LevelSession Build(LevelDefinition level)
        {
            IReadOnlyList<string> errors = LevelValidator.Validate(level);
            if (errors.Count > 0) throw new InvalidLevelException(errors);

            var board = new Board(level.Width, level.Height);
            for (int i = 0; i < level.Bats.Count; i++)
            {
                BatDefinition bat = level.Bats[i];
                board.Add(new Bat(i, bat.Cell, bat.Direction, bat.IsHidden));
            }

            return new LevelSession(board);
        }
    }
}
