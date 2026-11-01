namespace EchoCave.Game
{
    // Whatever actually makes the sound. The audio service talks to this, so it can be tested with a fake.
    public interface ISoundOutput
    {
        void Play(SoundCue cue);
    }
}
