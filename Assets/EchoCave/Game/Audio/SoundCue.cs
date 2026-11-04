using System;

namespace EchoCave.Game
{
    // Everything the game can ask to hear. The clip behind each cue is chosen by the SoundBank.
    public enum SoundCue
    {
        Ping,
        Reveal,
        Expire,
        Refuse,
        Fly,
        Bounce,
        Win,
        Lose,
        Click,
        Toggle,
        Star1,
        Star2,
        Star3,
        Hint,
        HintEarned
    }

    // The file name (without extension) each cue's clip has in Audio/Sfx, as the RokertSfx tool saves it.
    public static class SoundCueNames
    {
        public static string FileName(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Ping: return "ping";
                case SoundCue.Reveal: return "reveal";
                case SoundCue.Expire: return "expire";
                case SoundCue.Refuse: return "refuse";
                case SoundCue.Fly: return "fly";
                case SoundCue.Bounce: return "bounce";
                case SoundCue.Win: return "win";
                case SoundCue.Lose: return "lose";
                case SoundCue.Click: return "ui_click";
                case SoundCue.Toggle: return "toggle";
                case SoundCue.Star1: return "star_1";
                case SoundCue.Star2: return "star_2";
                case SoundCue.Star3: return "star_3";
                case SoundCue.Hint: return "hint";
                case SoundCue.HintEarned: return "hint_earned";
                default: throw new ArgumentOutOfRangeException(nameof(cue), cue, null);
            }
        }
    }
}
