# CLAUDE.md — EchoCave

Unity 6 (6000.6.3f1) grid puzzle for WebGL and Android — URP 17.6, Input System 1.20, UI Toolkit for HUD and popups (D-016). Solo project (Julian / Rokert). Namespace `EchoCave`, English-only code and comments.

Concept: tap a bat and it flies in a straight line off the board; if another bat blocks it, it bounces back and costs a move. Move limit per level, 3 stars. Twist: some bats are "in the shadow" (arrow hidden) and a limited number of echo pings reveal them. Free with ads.

## Sources of truth

- `Docs/ARCHITECTURE.md` — what exists and why (layers, assemblies, patterns). Update it when a system/pattern changes.
- `Docs/DECISIONS.md` — decision log (ADR-lite). Add an entry for any non-trivial choice; don't silently reverse one.
- [Linear — EchoCave](https://linear.app/rokert/project/echocave-b2f32e840edd) — the backlog (team `Rokert`, project `P-ROK-7`). Never file tickets in markdown.
- `com.rokert.core` docs: `Documentation~/PATTERNS.md` in the RokertCore repo (`../RokertCore` next to this project).

## Architecture rules (see `Docs/ARCHITECTURE.md`)

- **Core is pure C#**: `EchoCave.Core` (Board, Bat, MoveResolver, EchoService, level data model, solver) has `noEngineReferences: true`. No `UnityEngine`, no `Random`, no `Time`. Deterministic and EditMode-testable.
- **MVP**: Views are MonoBehaviours that only draw and forward input. Presenters connect views to core. No game logic in Views.
- **Assemblies**: `EchoCave.Core`, `EchoCave.Game`, `EchoCave.UI`, `EchoCave.Ads`, `EchoCave.Tests`. Dependencies point inward: UI/Game/Ads → Core, never the reverse.
- **Service Locator only at the composition root and game edges** (ads, audio, save, analytics). Inside the core, dependencies go through constructors.
- **Patterns**: Command (`IUndoableCommand` + `CommandHistory`) for moves, `StateMachine<T>` for flow (menu/playing/won/lost), Strategy for ad provider and echo behaviour, `ObjectPool<T>` + Factory for bats and effects, `EventBus` for cross-system messages (readonly struct events).
- **Levels are data** (ScriptableObject or JSON). Every shipped level is validated by the editor solver, which also computes the minimum move count.
- Reuse `Rokert.Core` instead of re-implementing patterns. Anything generally useful goes to RokertCore only after it is used in two projects.

## RokertCore workflow

`Packages/manifest.json` pins `"com.rokert.core": "https://github.com/Rokert/RokertCore.git#vX.Y.Z"` plus `"testables": ["com.rokert.core"]`.

To edit RokertCore while developing: temporarily switch the dependency to `"file:../../RokertCore"` (never both at once), then on finishing, release a new tag in RokertCore (with `.meta` files committed) and switch back to the URL with the new tag. Confirm Unity imports with no errors.

## Art

All art is original (ROK-144): drawn in Penpot, exported as PNG into `Assets/EchoCave/Art/{Characters,Icons,UI,Backgrounds,Brand,Fonts}`. The old third-party pack is gone and was never in git history, so do not bring in third-party art (fonts under an open licence are fine, with their licence file next to them). Font: Fredoka (SIL OFL, `Art/Fonts/OFL.txt`). Sounds are original too (RokertSfx, D-030). Inventory, palette, export rules and credits are in `Docs/ART.md`. The bat has a single front pose: show the direction with an arrow overlay, and a dark silhouette with "?" for hidden bats.

## Pipeline gates

1. **Pick & branch** — move the issue to In Progress in Linear; branch `feature/ROK-XX-short-desc` from `main`.
2. **Analyze once** — read only what memory/docs don't already cover.
3. **Plan before code** — list files to create/edit AND every manual Unity-editor step (prefabs, SO wiring, scene edits) upfront. Size-L tickets get split into Linear sub-issues first.
4. **Execute** — implement end-to-end. Out-of-scope ideas become new Linear issues.
5. **Manual checkpoint (HARD STOP)** — end the turn with the editor-only checklist. Never write `.meta` files by hand. Never commit before Julian confirms Unity imported cleanly.
6. **Test** — Claude writes EditMode tests; Julian runs them in the editor (don't assume a Unity CLI).
7. **Review** — run the checklist below; `/code-review` for core-logic changes.
8. **Commit & close** — only when Julian says so. Convention: `<area>: <imperative summary>` + `Refs ROK-XX`. Stage each new asset's `.meta` in the same commit. Move the Linear issue to Done.

## Review checklist (try to refute the diff)

- **Core purity**: `using UnityEngine` inside `EchoCave.Core` is a finding.
- **Determinism**: no `Random`/`Time`/`DateTime` in move resolution or the solver.
- **MVP**: logic in a View is a finding.
- **Service Locator misuse**: `ServiceLocator.Get` inside core/domain classes is a finding.
- **Tunables**: numbers (pings per level, move limits, timings) come from ScriptableObjects/level data — flag magic numbers.
- **Ads edge**: ad SDK types only behind `IAdService` in `EchoCave.Ads`.
- **Events**: subscribers unsubscribe in `OnDisable`; events are readonly structs.
- **Asset hygiene**: every new asset has its `.meta` in the same commit.
- **Test theater**: tests that assert the right thing but cannot fail.

## Hard rules

- Commit only when Julian explicitly says so.
- Never create or edit anything under `Library/`, `Temp/`, `Logs/`, `obj/`, or `UserSettings/`.
- Never hand-write `.meta` files.
- Zero-warning policy: code must compile with no warnings in the Unity console.
