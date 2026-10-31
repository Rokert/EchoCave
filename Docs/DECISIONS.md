# EchoCave — Decisions

Format: ID, status, decision, why, consequences. Add new entries at the bottom; supersede instead of rewriting.

## D-001 — Pure C# core, MVP presentation (Accepted)
Board, Bat, MoveResolver, EchoService live in `EchoCave.Core` with `noEngineReferences`. Views only draw and forward input.
**Why:** deterministic, fast EditMode tests, solver reuse in the editor, portable between WebGL and Android.
**Consequence:** a mapping layer between core types and Unity objects is required.

## D-002 — Share patterns via `com.rokert.core` (Accepted)
Installed from git tag (`#v0.1.1`) with `testables`. Local `file:../../RokertCore` only while editing the package.
**Why:** one implementation of ServiceLocator/EventBus/StateMachine/CommandHistory/ObjectPool across Rokert games.
**Consequence:** package changes need a release (tag with `.meta` files) before the game can use them.

## D-003 — Service Locator only at edges (Accepted)
Allowed at the composition root and for ads, audio, save, analytics. Core uses constructor injection.
**Why:** keeps core dependencies explicit and testable.

## D-004 — Command for moves, State machine for flow (Accepted)
`MoveCommand` implements `IUndoableCommand`; undo is the rewarded-ad reward. Flow is `StateMachine<T>`: Menu / Playing / Won / Lost.
**Consequence:** a bounce that costs a move must also be undoable exactly (move counter restored).

## D-005 — VContainer deferred (Accepted)
Not added now. Revisit if the composition root grows hard to maintain by hand.

## D-006 — Levels as data with solver validation (Accepted)
ScriptableObject or JSON (choice pending in the level-data ticket). A solver validates solvability and computes the minimum moves.

## D-007 — Hidden bats use arrow overlay and "?" silhouette (Accepted)
The art has one front pose, so direction is an arrow overlay; hidden bats show a dark silhouette with "?".

## D-008 — Art reuse from Hungry Bat (Accepted)
Reuse bat, `bg_night`, UI, popups, `gomarice_rocks`. Do not use fruit assets. An echo icon must be created. Check the asset-pack licence permits shipping before publishing.

## D-009 — Monetisation (Accepted, provider open)
Free with ads: rewarded (pings, undo), moderate interstitials, IAP remove-ads and ping packs. Mediation: AdMob or AppLovin MAX, decided in the ads ticket behind `IAdService`.

## D-010 — A bounce leaves the board unchanged (Accepted)
`MoveResolver` returns `Exited` or `Bounced`. On a bounce the bat returns to its own cell and only a move is spent; `StopCell` is the furthest cell it reached, for animation. Any bat in the path blocks, hidden or not.
**Why:** keeps the puzzle about ordering (free the path first) and makes undo a counter restore.
**Consequence:** the solver can treat a bounce as a wasted move that never changes the position.

## D-011 — Every tap spends one move (Accepted)
`MoveCommand` spends a move on any tap, exit or bounce. Only an exit changes the board. Undo restores the bat at its original index in `Board.Bats` and refunds the move.
**Why:** a bounce is simply a wasted tap; the move limit and the star thresholds count taps.
**Consequence:** the rewarded undo can revert a bounce and give the move back.

## D-012 — Levels: ScriptableObject for authoring, pure definition in Core (Accepted)
`LevelData` (ScriptableObject, `EchoCave.Game`) is edited in the inspector and converts to `LevelDefinition` (plain C#, `EchoCave.Core`). `LevelValidator` lists every structural error; `LevelBuilder` builds a `LevelSession` or throws `InvalidLevelException`. Bat ids are indices in the bat list. Stars: three at or under `ThreeStarMoves`, two at or under `TwoStarMoves`, otherwise one. `MoveLimit` 0 means unlimited.
**Why:** designers get inspector editing; the solver and tests use the Core types without Unity. JSON is not needed yet.
**Consequence:** a JSON export can be added later by writing a second adapter to `LevelDefinition`. Supersedes D-006's open choice.

## D-013 — Solver by waves, not search (Accepted)
Because a bounce changes nothing (D-010) and removing a bat never blocks another, `LevelSolver` clears every free bat in waves; the level is solvable when the board empties. Minimum moves with perfect information therefore equals the bat count. The solver also reports waves (blocking-chain depth), a valid order and mutually blocking bats.
**Why:** exact and linear-ish; BFS would explore states that cannot differ.
**Consequence:** stars are lost only through wasted taps (bounces), which makes hidden bats and pings the real difficulty lever. If a future rule makes bounces move bats, this solver must become a search. Suggested thresholds: three stars at the minimum, two with a configurable slack in the editor window.

## D-014 — DOTween for view animations (Accepted)
Fly-out, bounce and later reveal animations use DOTween, imported as the precompiled `DOTween.dll` under `Assets/Plugins`. Only `EchoCave.Game` and `EchoCave.UI` may use it; `EchoCave.Core` never does.
**Why:** tweens with easing, sequences and kill-on-recycle are what the pooled bat views need; hand-written coroutines would re-implement them. Supersedes the coroutine-based plan.
**Consequence:** the precompiled DLL is auto-referenced by our asmdefs. DOTween's module shortcuts (sprite, UI) need its Utility Panel to create `DOTween.Modules.asmdef`; do that only when a view needs them. Pooled views must kill their tweens on release.

## D-015 — Taps are ignored while a move animates (Accepted)
`BoardPresenter` sets a busy flag when it starts a move animation and clears it in the view's `onComplete`; taps in between are dropped, not queued.
**Why:** a bounce returns the bat to its cell and an exit recycles its view, so a second tap mid-animation could hit a bat that is still flying. Dropping is the simplest rule that keeps view and core in step.
**Consequence:** a rebuild (`Start`) clears the flag because killed tweens never call back. Animations are short (fly speed 12 cells/s), so this should not feel unresponsive; if playtests say otherwise, queue the taps instead.

## D-016 — UI Toolkit for HUD and popups (Accepted)
HUD, popups and menus are built with UI Toolkit (UXML + USS, `UIDocument`), not uGUI. The board itself stays in world space with `SpriteRenderer`. Supersedes the uGUI plan in CLAUDE.md and ROK-135.
**Why:** USS variables map one to one to the Penpot palette (`bg/deep`, `star/gold-dark`, ...), styling lives in text files that diff well in git, and layout adapts to portrait and landscape without per-screen prefab work. The art for the UI is original (Penpot), so the Hungry Bat uGUI prefabs have nothing left to reuse.
**Consequence:** `EchoCave.UI` drops its `UnityEngine.UI` and `Unity.TextMeshPro` references; UI Toolkit is a built-in engine module, so it needs no asmdef reference. Needs a `PanelSettings` asset (scale with screen size, 1080x1920 reference), Fredoka as a font asset, and sprites used through `background-image` with USS slicing. The MVP rule is unchanged: views draw and forward events, presenters decide. A tap on a panel must not reach the board: `TapInput` has to ignore pointers over UI (`panel.Pick`) once the first panel exists, which is part of the HUD ticket. Sprite PPU only matters for world sprites.

## D-017 — Win and lose rules, and when the flow reacts (Accepted)
Won = the board is empty. Lost = the move limit is used up while bats remain (only when the level has a limit). Won is checked first, so clearing the board on the last allowed move wins. `GameFlow` only moves from Playing to Won or Lost once the presenter is idle, so the result appears after the last animation, not while a bat is still flying. Retry builds a fresh session and presenter from the level definition.
**Why:** a lost move is only known once it is resolved, but showing the popup mid-flight looks broken, and tying the check to the animation callback would couple the rules to the view. Polling a stateless rule on `Tick` keeps the rules in Core and testable.
**Consequence:** the rewarded undo (after a loss) will need its own state or a kept history, since a retry discards it. Edge services (save, audio, analytics, ads) are not wired by the composition root yet: ROK-145 and ROK-146.

## D-018 — Echo: the reveal is service state, and a useless ping is free (Accepted)
`EchoService` keeps the set of revealed bat ids and a timer; the `Board` and its immutable `Bat`s are never changed. A ping costs no move (D-011 counts taps on bats; pings have their own budget). A ping that cannot do anything spends nothing and returns the reason: no pings left, a reveal already running, or no hidden bat on the board. Time enters through `Tick(deltaSeconds)`. The reveal duration has no default in Core: the strategy takes it in its constructor and the composition root reads it from a ScriptableObject. Notifications are plain C# events (`Pinged`, `Expired`) so Core stays free of the `EventBus`.
**Why:** a reveal is temporary, so mutating bats would need a matching re-hide and would leak into undo and the solver; keeping it beside the board makes both untouched. Refusing a ping while one runs avoids burning a paid resource by accident.
**Consequence:** views decide visibility as `!bat.IsHidden || echo.IsRevealed(id)`. A new `EchoService` is created per attempt, so a retry restores the pings. `Bat.WithHidden` stays in the model but the echo does not use it. Granting pings from a rewarded ad calls `GrantPings` (ROK-146). Wiring into the flow and views is ROK-147.

## D-019 — Echo in the game flow, and every refusal is visible (Accepted)
`GameFlow` creates one `EchoService` per attempt and ticks it only while Playing. A ping acts at once, only while Playing, and is allowed while a move animates (the reveal does not depend on moves). The player must always see an answer: a refused ping publishes `PingRefused` (for the HUD) and shakes the bats; losing reveals every hidden bat so the mistake is visible. The reveal duration lives in `EchoSettings`, not in code.
**Why:** a silent refusal reads as a broken button, and a loss that hides what went wrong teaches nothing. Ticking only while Playing means a paused or finished level never changes under the player.
**Consequence:** `IBoardView` gained `SetRevealed` and `ShowRejected`, and `GameFlow.Tick` takes the frame time. The shake and the plain show or hide are placeholders: the fine reveal and expiry animation, the ping wave and the sound belong to ROK-136. `PingRefused` carries the reason so the HUD can word it. The shake settings live in `BoardViewSettings`.

## D-020 — Background: two art sets by aspect, layered, with parallax (Accepted)
The night-cave background is five transparent layers per orientation (portrait 1080x1920, landscape 1920x1080) exported from the Penpot `Export` page. `BoardBackdrop` picks the set from the camera aspect (aspect >= 1 is landscape), never from the platform, so a narrow desktop browser window gets the tall art and WebGL, WebGPU and mobile share the same assets. Layers drift by depth from the pointer position plus a slow idle wander, within a margin that every layer scales up to cover, so an edge never shows.
**Why:** a single portrait image cropped to a wide window lost the stalactites and looked empty; separate art keeps the cave framed. Parallax gives the static board some life without touching gameplay.
**Consequence:** the old flattened `Background Night Cave` is gone. Strengths and the reduce-motion switch live in `BackdropSettings`; a reduce-motion option in the player settings can drive that switch later. Layers are assigned as textures and wrapped in full-rect sprites at runtime: the project imports PNGs with Sprite Mode Multiple and Unity auto-sliced the layers (the motes into about 26 pieces), so dragging a texture into a Sprite field assigned a single piece, which was then scaled to cover the whole view. Sizes come from `sprite.rect` over pixels per unit, a value that depends on nothing but the image. Layers use the default sprite material through `AddComponent`, so check them under the 2D renderer. A phone gyroscope as a motion source is possible later, but WebGL has no useful equivalent, so the idle wander stays the base.

## D-021 — Responsive UI Toolkit HUD, pause as a flag, USS-drawn controls (Accepted)
The HUD is laid out for portrait and landscape from the start. The panel switches its reference canvas by orientation (1080x1920 matched to the width, 1920x1080 matched to the height) on a runtime copy of the `PanelSettings`, so the asset never changes in the editor; safe-area insets become panel padding computed in code, since USS has no `env()`. Buttons, the moves sign and the ping button are drawn with USS from palette variables; only icons are images, exported from the Penpot `Export` page. Pause is a flag on `GameFlow` and not a state: pausing blocks taps and pings, freezes the echo clock and defers Won and Lost, and resuming does not rebuild the level, which re-entering a Playing state would. Pause moved from ROK-150 to ROK-149 so the pause button works. A tap that starts on a UI element never reaches the board (`IPointerBlocker`).
**Why:** a USS control stays sharp at any size, takes its colour from one place and gets pressed or disabled looks for free; images would need one export per size and state. A flag keeps resume cheap and the machine small.
**Consequence:** the look is close to Penpot but not pixel-identical (the lip under a button is a border or an offset layer); Penpot stays the reference and the comparison is made on screen. The pressed look, the shake of the ping button and the layout of the ping button in landscape are first versions to refine by eye. Single-sprite art should be imported with Sprite Mode Single, because the project default is Multiple and Unity auto-slices it.

## D-022 — Popups: one document, answer at once, audio switches that only announce (Accepted)
The Complete, Lost and Pause popups live in `Popups.uxml`, included in `Hud.uxml`, so the UI stays a single `UIDocument`. A dim layer behind them catches every pointer, which blocks the board without extra code. A popup hides the moment a button is pressed and the game follows on the next tick. The earned stars light up one after another with a bounce, and the card fades in. On the last level the Next button reads Menu, because `RequestNext` already returns to the menu there. The Pause popup gets a Menu button the original art did not have, and the Lost popup is new; both were added to Penpot. The sound and music switches keep their state in memory from level to level and publish `AudioToggled`; they play and persist nothing until ROK-145.
**Why:** a button that waits for the game to react feels dead, and a second tap in that gap is what let a double press on Next skip a level (`RequestNext` now ignores a repeat before the next tick). Letting the scrim block input removes any need for the board to know a popup is open.
**Consequence:** the fade, the bounce and the star order are first versions to refine by eye. There is no exit animation. Escape or Android back to pause is not wired. The Lost popup has no undo slot yet: the rewarded undo comes with ROK-146.

## D-023 — Main menu: a sibling of the HUD, the game starts in the menu, the debug overlay is gone (Accepted)
The game no longer starts a level by itself: it boots into the menu, and the Play button of `Menu.uxml` asks the flow to play. The menu is a template in `Hud.uxml` placed next to the HUD root, not inside it, because the HUD hides itself while the menu shows. `MenuPresenter` hides the menu as soon as Play is pressed, like the popups. It is told at construction whether the flow is in the menu, because the flow publishes `MenuOpened` in its constructor before any UI exists. `GameDebugOverlay` and its keyboard shortcuts are deleted; every action now has a real button. The logo (bat, echo arcs and the "EchoCave" wordmark in Fredoka Bold) is an original Penpot drawing on a new `Brand` page, exported as `Art/Brand/Logo.png`.
**Why:** an autoplaying build cannot show a title or give the player a moment before the first level, and the overlay was a debug aid that hid the real buttons.
**Consequence:** there is no keyboard way to start or skip levels any more; testing a late level means playing to it until a level-select or debug menu exists. The menu has no settings, level select or version label yet. The logo and the Play button size are first versions to refine by eye.

## D-024 — The first 10 levels are generated from a seeded plan and curated by playing (Accepted)
Levels 2-10 are produced by `LevelGenerator` from a `LevelGenerationPlan` (one entry per level: board size, bats, hidden bats, pings, move-limit slack, minimum chain depth and traps, a seed). Level 1 stays hand-made and is only listed in the catalog. Each generated level must be solvable, meet its entry, and be at least as hard as the previous one and at most `maxDifficultyStep` harder, so the curve climbs in small steps by construction. The curve follows the project plan: 1-3 no hidden bats and no pings, 4-6 the first hidden bats with generous pings, 7-10 more hidden bats and fewer pings, with the peak at 10. A shipped-levels test checks the tiers and that difficulty never drops from one level to the next.
**Why:** the solver proves a level can be won but the minimum moves always equal the bat count (D-010), so difficulty has to be measured on traps, chain depth, hidden bats and slack. Placing bats by hand into YAML has already produced an invalid level once (a two-star threshold above the move limit); a generator that validates every level removes that class of mistake.
**Consequence:** the solver cannot say whether a level is fun. A random valid layout can be dull or ugly, so each level is played and, if it is not good, its seed is changed and it is regenerated. `Difficulty` is a heuristic (`Bats + 2*Hidden + 3*Waves + Traps`), not a promise about feel; the weights are a first guess to refine by playing. Level 1 keeps its generous move limit of 9. Hints (ROK-168) will not change this: a hint is just the next solver step.

## Open questions
- Check the "EchoCave" name against Play Store before publishing.
- Play Store closed-testing requirement for new personal accounts.
