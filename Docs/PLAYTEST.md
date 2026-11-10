# EchoCave — Web playtest

How to build, publish and run the 20-30 person playtest (ROK-138, D-025).

## Build
0. **Decide about the tutorial.** `Assets/EchoCave/Data/TutorialSettings.asset` has *Auto Start*: on, the first Play starts a five-lesson tutorial (Skip is always visible) and the first level follows; off, it only shows if the tester presses *How to play* in the menu or the pause popup. The playtest question "is the rule understood without explanation?" is only fair with it off, so for that question either ship a build with it off, or ship two builds and compare. A save from before the tutorial counts as not seen, so a tester who already has progress sees it once on a build with it on.
0. **Turn the ad refill off.** Open `Assets/EchoCave/Data/HintSettings.asset` and untick *Refill With Ads*. The only ad provider is still `NullAdService`, which grants every request at once, so with the refill on a tester who runs out of hints gets another by pressing the button: hints would be unlimited and nobody could judge the economy. With it off, the button with no hints looks empty and a press is refused with the usual shake. **Never commit the asset with it off**: the committed value stays on, because that is the real behaviour once a real ad SDK exists. Before staging, check `git status` and leave `HintSettings.asset` out (or `git restore` it after the build).
1. In Unity: `EchoCave > Build > WebGL (zip for itch.io)`. It builds the Game scene and writes `Builds/EchoCave-WebGL.zip`, then opens the folder.
2. Read the console line `[BuildWebGL] Built ... MB`. If the zip is much over 30 MB, the textures need compressing before a phone on mobile data is asked to download it.

## Publish on itch.io
1. Dashboard > Create new project.
2. **Kind of project:** HTML. Upload the zip and tick *This file will be played in the browser*.
3. **Embed options:** viewport 1080 x 1920 (or tick *Fullscreen button* and *Mobile friendly*), *Enable scrollbars* off, *Automatically start on page load* off.
4. **Visibility:** Restricted, with a password. Only the owners can open the page without it; testers open the link and type the password, no itch.io account needed. The page is unlisted in browse and search. Send testers the link and the password. (Download keys, one per tester, are the alternative if a password ever leaks.)
5. After each new build, upload the zip over the old one and bump the version in Unity (`Project Settings > Player > Version`) so the number on the menu changes.

## Before sending it
- Play levels 1 to 10 on desktop (Chrome and Safari) and on at least one iPhone and one Android phone, in portrait and landscape.
- Check a tap on a bat, a tap on a button, the pause popup, the pings, and rotating the phone in the middle of a level.

## What to find out
1. Is the rule understood without explanation? Watch the first two levels in silence.
2. Does the echo help or frustrate? Note when a tester first uses it and what they say.
3. Do they ask for another level at the end?
4. Do the hints feel enough? A new player starts with 3 and earns more only by opening the game on a new day (1 hint) or by winning 3 new levels in a row with 3 stars (1 hint, with one spare kept when the stock of 5 is full). Note whether testers run out, how soon, and whether they notice the gold "+1" tag and the pop of the button when a free hint arrives. The knobs are in `HintSettings.asset`: starting hints, cap, reserve, streak length, hints per streak and per day.

A tester's first day gives no daily hint (the starting hints are that day's gift), so the daily gift only shows on a second day of play. A save from an older build is still read: the tester keeps their stars and starts with the starting hints and no reserve.

5. Is the music liked? Ask whether they noticed it, whether it fits the game, and whether they turned it off in the pause popup (and why). Note if anyone plays with the sound on but the music off.
6. Do they find the challenges? Watch what happens after the first unlock: do they notice the unlock note on the level complete popup and the NEW badge on the menu button, and do they open the list without being told? Ask what they think a challenge is, whether the locked ones made them want to keep playing, and whether the three results under each challenge mean anything to them. Challenges are the same board for everyone, with a ping budget and no retries on the same result, so also note whether a tester understands why a second try does not feel like a new level.

Also note where they stop, which level they lose at, and what they tap that does nothing.

## Collecting feedback
The release build records nothing: the event catalog exists (D-037), but only the editor and development builds log it, and no provider is chosen yet. Use a short form with the questions above, the version shown at the bottom of the menu, and the device.
