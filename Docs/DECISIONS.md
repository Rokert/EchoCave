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
**Consequence:** `EchoCave.UI` references `UnityEngine.UIElementsModule` instead of uGUI. Needs a `PanelSettings` asset (scale with screen size, 1080x1920 reference), Fredoka as a font asset, and sprites used through `background-image` with USS slicing. The MVP rule is unchanged: views draw and forward events, presenters decide. A tap on a panel must not reach the board: `TapInput` has to ignore pointers over UI (`panel.Pick`) once the first panel exists, which is part of the HUD ticket. Sprite PPU only matters for world sprites.

## Open questions
- Check the "EchoCave" name against Play Store before publishing.
- Play Store closed-testing requirement for new personal accounts.
