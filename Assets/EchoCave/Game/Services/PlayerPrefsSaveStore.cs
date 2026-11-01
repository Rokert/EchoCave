using UnityEngine;

namespace EchoCave.Game
{
    // The real store. PlayerPrefs lives in the browser's IndexedDB on WebGL and in app storage on Android,
    // so one implementation covers both targets.
    public sealed class PlayerPrefsSaveStore : ISaveStore
    {
        public const string Key = "echocave.save";

        public bool TryRead(out string content)
        {
            content = PlayerPrefs.GetString(Key, null);
            return !string.IsNullOrEmpty(content);
        }

        public void Write(string content)
        {
            PlayerPrefs.SetString(Key, content);

            // Without this the browser only flushes when the page closes, which a mobile tab often never reaches.
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
