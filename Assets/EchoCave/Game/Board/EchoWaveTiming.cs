using System;

namespace EchoCave.Game
{
    // The arithmetic of the ping wave, kept out of the views so it can be tested. Distances are in cells and the
    // wave starts at the centre of the board, so a bat is revealed when the ring reaches it.
    public static class EchoWaveTiming
    {
        // Seconds until a wave moving at speedCellsPerSecond covers distanceCells.
        public static float DelayFor(float distanceCells, float speedCellsPerSecond)
        {
            if (speedCellsPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speedCellsPerSecond), speedCellsPerSecond, "Speed must be positive.");
            if (distanceCells < 0f)
                throw new ArgumentOutOfRangeException(nameof(distanceCells), distanceCells, "Distance must not be negative.");

            return distanceCells / speedCellsPerSecond;
        }

        // How far the ring has to travel to cover the whole board: from the centre to the farthest corner cell,
        // plus half a cell so the ring has passed the bat sitting on that corner.
        public static float ReachCells(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");

            float halfWidth = (width - 1) * 0.5f;
            float halfHeight = (height - 1) * 0.5f;
            return (float)Math.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + 0.5f;
        }
    }
}
