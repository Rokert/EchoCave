namespace EchoCave.Game
{
    // A store that forgets everything when the app closes. For tests, and as the save of a build that must not persist.
    public sealed class InMemorySaveStore : ISaveStore
    {
        private string _content;

        public bool TryRead(out string content)
        {
            content = _content;
            return _content != null;
        }

        public void Write(string content)
        {
            _content = content;
        }

        public void Delete()
        {
            _content = null;
        }
    }
}
