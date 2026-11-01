namespace EchoCave.Game
{
    // Where the serialized save lives: PlayerPrefs on a device, memory in tests.
    public interface ISaveStore
    {
        // False when nothing has been saved yet.
        bool TryRead(out string content);

        void Write(string content);

        void Delete();
    }
}
