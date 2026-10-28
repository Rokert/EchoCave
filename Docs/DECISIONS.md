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

## Open questions
- Level format: ScriptableObject vs JSON.
- Check the "EchoCave" name against Play Store before publishing.
- Play Store closed-testing requirement for new personal accounts.
