using System.Collections.Generic;
using EchoCave.Game;

namespace EchoCave.UI
{
    // Which sound a pressed UI button makes. Decided from the button's USS classes, so a new button gets the
    // right sound from its look without code.
    public static class UiSoundRules
    {
        private const string ToggleClass = "toggle";
        private const string PingButtonClass = "ping-button";

        // Null when the button makes no sound of its own: the ping button already plays the ping, or the
        // refusal, through its game events, and a click on top would blur them.
        public static SoundCue? CueForButton(IEnumerable<string> classes)
        {
            bool isToggle = false;
            foreach (string name in classes)
            {
                if (name == PingButtonClass) return null;
                if (name == ToggleClass) isToggle = true;
            }

            return isToggle ? SoundCue.Toggle : SoundCue.Click;
        }

        // The sound of the star that just lit, by its position: each one is higher than the last.
        public static SoundCue CueForStar(int starIndex)
        {
            switch (starIndex)
            {
                case 0: return SoundCue.Star1;
                case 1: return SoundCue.Star2;
                default: return SoundCue.Star3;
            }
        }
    }
}
