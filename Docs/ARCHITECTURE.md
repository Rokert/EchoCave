# EchoCave — Architecture

## Game summary

Grid puzzle. Tap a bat: it flies in its arrow direction until it leaves the board. If another bat blocks the path it bounces back and the move is spent. Each level has a move limit and 3 stars. Some bats are hidden ("in the shadow"); limited echo pings reveal their arrows for a few seconds.

## Layers and assemblies

```
EchoCave.Core    pure C# (noEngineReferences)   Board, Bat, MoveResolver, EchoService, level model, solver
EchoCave.Game    MonoBehaviours + presenters     bat views, board view, input, game flow states, composition root
EchoCave.UI      UI Toolkit views + presenters   HUD, popups (Complete/Pause), stars, ping button (D-016)
EchoCave.Ads     ad edge                         IAdService, providers (AdMob/MAX/null), rewarded flows
EchoCave.Tests   EditMode tests                  core logic, solver, services
Rokert.Core      external package (UPM)          ServiceLocator, EventBus, StateMachine, CommandHistory, ObjectPool
```

Dependency direction: `Game`, `UI`, `Ads` → `Core` → `Rokert.Core`. Core never references the others. Editor tooling (level editor + solver window) lives in an `EchoCave.Editor` assembly referencing Core.

## Core domain (pure C#)

- `Board`: width/height, bats by cell. No rendering concepts.
- `Bat`: id, cell, `Direction`, `IsHidden`.
- `MoveResolver`: given board + tapped bat, returns a `MoveResult` (Exited / Bounced with the blocking bat and stop cell). Deterministic; no side effects until applied.
- `EchoService`: ping budget and the temporary reveal of hidden bats (D-018). `TryPing(board)` returns a `PingResult` and spends a ping only when it starts a reveal; `Tick(deltaSeconds)` runs the reveal timer, so the core owns no clock; `GrantPings` adds pings (rewarded ads). The board is never mutated: a revealed bat is only listed in the service (`IsRevealed(id)`), so undo and the solver are unaffected. A view shows a bat's arrow when `!bat.IsHidden || echo.IsRevealed(bat.Id)`. Notifies through C# events `Pinged` and `Expired`; the adapter to the `EventBus` lives in `Game` (ROK-147).
- `IEchoStrategy` / `EchoPulse`: what a ping reveals (bat ids) and for how long. `RevealAllHiddenStrategy` is the default; its duration is a constructor argument, supplied from a ScriptableObject by the composition root.
- Level data model: plain classes loaded from ScriptableObject/JSON by an adapter in `Game`.

## Presentation (MVP)

- **View** (MonoBehaviour): draws and forwards input events. No decisions.
- **Presenter**: subscribes to the view, calls the core, updates the view. Plain C# class where possible.
- `GridLayout2D` (`EchoCave.Game`): maps cells to world positions and back. Board centered on the origin, Y up like `Direction`. Views and input share it so no view does coordinate math.
- `BatView`: bat sprite + arrow overlay, or a dark "?" silhouette when hidden. Animations use DOTween (D-014). `ResetForPool` kills its tweens and restores the prefab pose.
- `BatFactory`: plain C# over `ObjectPool<BatView>`. `Get(Bat)` returns an active, bound view; `Release` and `ReleaseAll` recycle them. It tracks active views because the pool accepts a double release. Views are children of a container, which is what frees them: the pool drops items it cannot keep without destroying them. The board view builds it from a serialized prefab and container.
- `BoardView`: draws the core board and plays move animations with DOTween. `Build(Board)` replaces the scene, `Play(MoveResult, onComplete)` animates one tap (fly-out and recycle, or fly, bump, knock the blocker and return). It decides nothing; the presenter passes it the result the core already produced. Positions are local to its transform.
- `IBoardView`: the slice of the board view the presenter and the flow use (`Build`, `Clear`, `TryGetCell`, `Play`, `SetRevealed`, `ShowRejected`), so their tests run against a fake. `SetRevealed(ids)` shows exactly those hidden bats as revealed (`BatView` remembers whether it is hidden by default); `ShowRejected` shakes the bats to say "that did nothing".
- `BoardPresenter`: plain C#. `HandleTap(world)` maps the point to a cell through the view, and a tap on a bat becomes a `MoveCommand` run through `CommandHistory`; the view then animates `command.Result`. Taps are ignored while an animation plays (D-015). `Start()` builds the view and clears the busy flag.
- `TapInput`: MonoBehaviour with a code-defined `<Pointer>/press` action (mouse, pen, touch; no input actions asset). Raises `Tapped(worldPoint)` and nothing else.
- `LevelRules` (Core): `Evaluate(session, level)` returns `InProgress`, `Won` or `Lost`. Won = board empty; Lost = move limit used up with bats left; Won is checked first (D-017).
- `GameFlow` (plain C#): owns a `StateMachine<FlowState>` with Menu, Playing, Won and Lost, the current level index, and the session and presenter of the level being played (a new pair per attempt, so undo history never leaks between attempts). The player's intents (`RequestPlay`, `RequestRetry`, `RequestNext`, `RequestMenu`) set a flag only when they make sense in the current state; transitions consume it on the next `Tick`, and every state change clears the flags. Playing waits for the presenter to be idle before it goes to Won or Lost. Publishes `LevelStarted` (with the ping budget), `LevelWon`, `LevelLost`, `PingUsed`, `EchoExpired` and `PingRefused` (readonly structs) on the `EventBus`. It also owns the echo of the attempt (D-019): one `EchoService` per attempt, ticked only while Playing (`Tick(deltaSeconds)`); `RequestPing()` acts at once and only while Playing, and a refused ping publishes `PingRefused` and calls `ShowRejected`; losing reveals every hidden bat so the player sees the mistake.
- `FlowState` and its four subclasses: only react to `Enter`; the rules live in `GameFlow`.
- `LevelCatalog` (ScriptableObject): the levels in play order, handed to the flow as `LevelDefinition`s.
- `EchoSettings` (ScriptableObject): the reveal duration. `GameRoot` turns it into the `RevealAllHiddenStrategy` the flow receives.
- `GameRoot` (composition root of the Game scene): builds the flow from the catalog and the scene views, forwards `TapInput` taps, ticks the flow in `Update`, and re-frames the camera on `LevelStarted`. No edge services are registered yet; save, audio, analytics and ads have their own tickets (ROK-145, ROK-146).
- `GameDebugOverlay`: temporary IMGUI panel to drive the flow until the UI Toolkit HUD exists (ROK-135).
- `MovePathPlanner`: pure function from `MoveResult` + `GridLayout2D` to a `MovePlan` (start, target, bump point, travel in cells). All the animation geometry lives here so it is testable; the view only tweens between the points.
- `BoardCamera` + `CameraFraming`: orthographic framing of the board plus margin, and a background sprite scaled to cover the view. Refits when the screen aspect changes (rotation, WebGL resize). The math is in `CameraFraming` for EditMode tests.
- `BoardViewSettings` (ScriptableObject): cell size, camera margin, fly speed, overshoot, bump/return timings, eases. Distances are in cells.
- Cross-system notifications (level won, ping used, move spent) go through `EventBus` with readonly struct events.

## Patterns in use

| Need | Pattern | Where |
|---|---|---|
| Undoable moves (rewarded undo) | `IUndoableCommand` + `CommandHistory` | `MoveCommand` wraps `MoveResolver` result |
| Game flow | `StateMachine<T>` | Menu → Playing → Won / Lost |
| Ad provider | Strategy (`IAdService`) | AdMob, MAX, null provider for WebGL/editor |
| Echo behaviour | Strategy (`IEchoStrategy`) | `EchoService` |
| Bats and effects | `ObjectPool<T>` + Factory | `BatFactory`, `EffectFactory` |
| Edges (ads, audio, save, analytics) | Service Locator | registered once in the composition root |

Service Locator is not used inside the core: dependencies are constructor-injected. VContainer is deliberately deferred (see DECISIONS D-005).

## Levels as data

Levels are data assets (bat positions, directions, hidden flags, move limit, ping budget, star thresholds). An editor tool runs a solver over each level to (a) validate it is solvable and (b) compute the minimum move count, which feeds the star thresholds. CI-style EditMode test runs the solver over all shipped levels.

## Platforms

- **WebGL**: no native ad SDK; uses the null/web ad provider. Keep the core allocation-light and avoid threading.
- **Android (Play Store)**: AdMob or AppLovin MAX behind `IAdService`; IAP for remove-ads and ping packs.

## Level progression (v1, 10 levels)

1–3: no hidden bats, no move limit. 4–6: first hidden bat, generous pings. 7–10: more hidden bats, fewer pings; difficulty peak at 10.
