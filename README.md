# EchoCave

A grid puzzle for WebGL and Android. Tap a bat and it flies in a straight line off the board. If another bat is in the way, it bounces back and the move is spent. Clear the board within the move limit to earn up to three stars.

The twist: some bats are in the shadow (their arrow is hidden), and a limited number of echo pings reveal them for a few seconds. A hint shows a bat that can leave now.

**Status:** playable prototype with 10 levels, a five-lesson tutorial, saved progress, sounds, hints you can earn (a daily gift and a 3-star streak) and a rewarded-ad refill behind a null ad provider. A web playtest is running on itch.io by invitation.

## Open it

- Unity **6000.6.3f1** (URP, Input System, UI Toolkit).
- Add the folder in Unity Hub and open `Assets/EchoCave/Scenes/Game.unity`.
- `Packages/manifest.json` takes `com.rokert.core` (a small set of patterns: service locator, event bus, state machine, command history, object pool) from the public `RokertCore` repository on GitHub (MIT), so the project opens without extra access.

## Tests

`Window > General > Test Runner > EditMode`. The game rules live in `EchoCave.Core`, which has no Unity references, so most of the logic is tested without the engine.

## Where to read more

- `Docs/ARCHITECTURE.md`: layers, assemblies and patterns.
- `Docs/DECISIONS.md`: the decision log, one entry per non-trivial choice.
- `Docs/ART.md`: the original art, the palette, export rules and credits.
- `Docs/PLAYTEST.md`: how a playtest build is made and published.
- `CLAUDE.md`: the working rules of the project.

## Credits

Art and sounds are original. Fredoka is under the SIL Open Font License. DOTween (Demigiant) is used unmodified under its own licence. Details in `Docs/ART.md`.

## Licence

- **Code:** MIT, see `LICENSE`.
- **Art and sounds:** CC BY-NC 4.0, see `LICENSE-ASSETS`. The name "EchoCave", the logo, the app icon and the cover image are not covered and stay all rights reserved.
- Fredoka (SIL OFL) and DOTween keep their own licences.
