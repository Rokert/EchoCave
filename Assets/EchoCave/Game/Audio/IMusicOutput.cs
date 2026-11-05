namespace EchoCave.Game
{
    // Whatever actually plays the music. It crossfades from the track that was playing to the new one by itself. The
    // audio service talks to this, so it can be tested with a fake.
    public interface IMusicOutput
    {
        // Starts the track (a crossfade from the current one). Does nothing when it is already the one playing.
        void Play(MusicTrack track);

        // Fades the music out.
        void Stop();
    }
}
