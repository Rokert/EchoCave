# EchoCave — art

All art in the repository is original (ROK-144). It was drawn as flat vectors in Penpot (an earlier pass was in Figma, file key `jKmVdtfTD4wBmQmmLwvW27`, now superseded) and exported as PNG. The third-party pack used at the start was never committed and is no longer in the project.

## Where things are

| Folder (`Assets/EchoCave/Art/`) | What |
|---|---|
| `Characters` | `Bat`, `Bat Silhouette` (the dark "?" for hidden bats) |
| `Icons` | `Arrow Shape` (white, points up, tinted in Unity), `Echo Icon Ready/Empty`, `Hint Icon Ready/Empty`, `Icon Pause`, `Icon Play`, `Ping Ring` |
| `UI` | `Star Empty`, `Star Filled`. Buttons, signs, toggles and popups are drawn with USS from the palette, not with sprites |
| `Backgrounds` | `Cell Tile` and ten parallax layers (`Backdrop Portrait/Landscape` x `Base/Far/Near/Crystals/Motes`), chosen by camera aspect (D-020) |
| `Brand` | `Logo`, `App Icon` (1024), `ItchCover` |
| `Fonts` | Fredoka Regular, Medium, SemiBold, Bold, and `OFL.txt` |

The web template (`Assets/WebGLTemplates/EchoCave`) uses the same logo and app icon.

## Palette

Defined once in `UI/Styles/Theme.uss` (USS variables) and mirrored in Penpot:
`bg/deep #14102B`, `bg/mid #231B4A`, `bg/rock #3A2D6B`, `silhouette/dark #0B0818`, `bat/body #7B5CD6`, `bat/belly #B9A4F2`, `bat/ear #5A3FB0`, `white #FFFFFF`, `echo/cyan #4FE3FF`, `echo/glow #A8F3FF`, `star/gold #FFC94A`, `danger/red #FF6B6B`, `ui/cream #FFF4DC`, `ui/text #2A1F55`.

## Export rules

- PNG at 1x: every component is already drawn at its final size (bat and silhouette 512, arrow and echo icons 256, app icon 1024).
- World sprites (bat, silhouette, arrow, ping ring, cell tile) use 256 pixels per unit, no mipmaps. The bat prefab has a root scale of 0.45 so the bat fills about 0.9 of a one-unit cell.
- The parallax layers are assigned as `Texture2D`, not as sprites.
- Name the PNG like the Unity asset (`Arrow Shape.png`) and re-export over the file, keeping its `.meta`.

## Music

Four 12 s seamless loops in `Assets/EchoCave/Audio/Music`, made with RokertSfx (loop mode; the presets `music_*.json` live in that repo and are the source): `music_menu`, `music_play`, `music_play_bright`, `music_play_dream`. Import each clip with Load Type Streaming and Compression Format Vorbis (`ShippedMusicTests` checks it). Volumes, the playlist and the time per track are in the `MusicBank` asset (D-035).

## Gotchas

- Unity imports new PNGs as Sprite Mode Multiple and may auto-slice them. For art used as one sprite, set Single. Switching a sprite that a prefab already uses between Single and Multiple changes its sub-asset id (`fileID`), so the prefab then needs the new one (Single is `21300000`).
- A new USS file was not applied until Unity restarted once.

## Credits and licences

- Art, sounds and music: original, by the project's author, under CC BY-NC 4.0 (`LICENSE-ASSETS` at the repository root). The name, the logo, the app icon and the cover image (`Art/Brand`, and the logo, favicon and thumbnail of the web template) are not covered and stay all rights reserved.
- Fredoka: SIL Open Font License 1.1 (`Art/Fonts/OFL.txt`).
- DOTween (Demigiant, `Assets/Plugins/Demigiant`): used for view animations (D-014). It is the free version. Its licence (dotween.demigiant.com/license.php) allows commercial use and allows distributing unmodified copies in any medium, as long as the original copyright notices and `readme.txt` stay with them. So the DLL and its `readme.txt` are kept exactly as downloaded: never edit or repack them. The settings asset in `Assets/Resources` is ours, not part of the package.
