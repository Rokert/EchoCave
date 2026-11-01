# EchoCave — Web playtest

How to build, publish and run the 20-30 person playtest (ROK-138, D-025).

## Build
1. In Unity: `EchoCave > Build > WebGL (zip for itch.io)`. It builds the Game scene and writes `Builds/EchoCave-WebGL.zip`, then opens the folder.
2. Read the console line `[BuildWebGL] Built ... MB`. If the zip is much over 30 MB, the textures need compressing before a phone on mobile data is asked to download it.

## Publish on itch.io
1. Dashboard > Create new project.
2. **Kind of project:** HTML. Upload the zip and tick *This file will be played in the browser*.
3. **Embed options:** viewport 1080 x 1920 (or tick *Fullscreen button* and *Mobile friendly*), *Enable scrollbars* off, *Automatically start on page load* off.
4. **Visibility:** Restricted, with a password, or Draft with a secret link. Send testers the link and the password.
5. After each new build, upload the zip over the old one and bump the version in Unity (`Project Settings > Player > Version`) so the number on the menu changes.

## Before sending it
- Play levels 1 to 10 on desktop (Chrome and Safari) and on at least one iPhone and one Android phone, in portrait and landscape.
- Check a tap on a bat, a tap on a button, the pause popup, the pings, and rotating the phone in the middle of a level.

## What to find out
1. Is the rule understood without explanation? Watch the first two levels in silence.
2. Does the echo help or frustrate? Note when a tester first uses it and what they say.
3. Do they ask for another level at the end?

Also note where they stop, which level they lose at, and what they tap that does nothing.

## Collecting feedback
No analytics yet (ROK-145 holds the interface). Use a short form with the three questions above, the version shown at the bottom of the menu, and the device.
