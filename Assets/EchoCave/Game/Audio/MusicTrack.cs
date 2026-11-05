using System;

namespace EchoCave.Game
{
    // Every piece of music the game has. The clip behind each one is chosen by the MusicBank.
    public enum MusicTrack
    {
        Menu,
        Cave,
        Bright,
        Dream
    }

    // The two places the music follows: the menu has its own track, play rotates through a playlist.
    public enum MusicArea
    {
        Menu,
        Play
    }

    // The file name (without extension) each track's clip has in Audio/Music, as the RokertSfx tool saves it.
    public static class MusicTrackNames
    {
        public static string FileName(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Menu: return "music_menu";
                case MusicTrack.Cave: return "music_play";
                case MusicTrack.Bright: return "music_play_bright";
                case MusicTrack.Dream: return "music_play_dream";
                default: throw new ArgumentOutOfRangeException(nameof(track), track, null);
            }
        }
    }
}
