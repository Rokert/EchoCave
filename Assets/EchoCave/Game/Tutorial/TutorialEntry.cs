namespace EchoCave.Game
{
    // When the Play button starts the tutorial instead of the first level.
    public static class TutorialEntry
    {
        // Only when the game has lessons, this build lets the tutorial start by itself, and the player has not finished or
        // skipped it yet. "How to play" does not go through here: it always starts the tutorial.
        public static bool ShouldAutoStart(bool hasTutorial, bool autoStartEnabled, bool seen)
        {
            return hasTutorial && autoStartEnabled && !seen;
        }
    }
}
