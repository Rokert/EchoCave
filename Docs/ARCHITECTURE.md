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
- `GameFlow` (plain C#): owns a `StateMachine<FlowState>` with Menu, Playing, Won and Lost, the current level index, and the session and presenter of the level being played (a new pair per attempt, so undo history never leaks between attempts). The player's intents (`RequestPlay`, `RequestRetry`, `RequestNext`, `RequestMenu`) set a flag only when they make sense in the current state; transitions consume it on the next `Tick`, and every state change clears the flags. Playing waits for the presenter to be idle before it goes to Won or Lost. Publishes `LevelStarted` (with the ping budget), `LevelWon`, `LevelLost`, `PingUsed`, `EchoExpired` and `PingRefused` (readonly structs) on the `EventBus`. It also owns the echo of the attempt (D-019): one `EchoService` per attempt, ticked only while Playing (`Tick(deltaSeconds)`); `RequestPing()` acts at once and only while Playing, and a refused ping publishes `PingRefused` and calls `ShowRejected`; losing reveals every hidden bat so the player sees the mistake. It also owns the pause (D-021): `IsPaused` with `RequestPause` / `RequestResume`, only while Playing; a pause blocks taps and pings, freezes the echo clock and defers Won and Lost, and retry or the menu end it. `MoveSpent`, `PauseChanged` and `MenuOpened` complete the events the HUD listens to.
- `FlowState` and its four subclasses: only react to `Enter`; the rules live in `GameFlow`.
- `LevelCatalog` (ScriptableObject): the levels in play order, handed to the flow as `LevelDefinition`s.
- `EchoSettings` (ScriptableObject): the reveal duration. `GameRoot` turns it into the `RevealAllHiddenStrategy` the flow receives.
- `GameRoot` (composition root of the Game scene): builds the flow from the catalog and the scene views, forwards `TapInput` taps, ticks the flow in `Update`, and re-frames the camera on `LevelStarted`. The game starts in the menu; the UI's Play button asks the flow to play. It registers the edge services in the `ServiceLocator` (only `IAdService` so far, with `NullAdService`; save, audio and analytics are ROK-145) and unregisters them in `OnDestroy`, using `Replace` because the registry outlives a Play session when Reload Domain is off.
- `ScreenAspect`: the single rule that says portrait or landscape (aspect >= 1 is landscape), shared by the backdrop art and the UI scaling.
- `IPointerBlocker` (`Game`, implemented in `UI`): `TapInput` asks it whether a pointer is over the UI and drops the tap if so.

## UI (EchoCave.UI, UI Toolkit, D-016 and D-021)

- **Stylesheets:** `Theme.uss` holds the Penpot palette as USS variables and the four Fredoka weights as `font-*` classes (static TTFs in `Art/Fonts`, OFL licence next to them). `Hud.uss` styles the HUD. Round buttons, the moves sign and the ping button are drawn with USS; only the icons are images.
- **MVP:** `IHudView` / `HudView` draw and forward the two buttons; `HudPresenter` (plain C#) listens to the `EventBus` (`LevelStarted`, `MoveSpent`, `PingUsed`, `EchoExpired`, `PingRefused`, `PauseChanged`, `MenuOpened`) and calls the game back through two delegates. `Dispose` unsubscribes everything.
- **Responsive:** `UiScaling` picks the reference canvas by orientation (1080x1920 matched to the width in portrait, 1920x1080 matched to the height in landscape) and `UiScale.PixelsPerUnit` gives the resulting scale; `ScreenInsets` turns `Screen.safeArea` into panel-unit padding. `UiRoot` applies both to a runtime copy of the `PanelSettings`, toggles the `portrait` / `landscape` class on the HUD, and re-applies when the screen size changes.
- **Input:** layout containers have `picking-mode: Ignore`, so only real controls catch a pointer. `UiPointerBlocker` implements `IPointerBlocker` with `panel.Pick`. The UI needs an `EventSystem` with the Input System UI module because the project uses only the new input backend.
- **Popups (D-022):** `Popups.uxml` is included in `Hud.uxml` as a template, so there is still one `UIDocument`; the dim layer inside it catches every pointer, so nothing behind a popup can be tapped. `IPopupsView` / `PopupsView` draw the Complete, Lost and Pause popups and forward their buttons as a `PopupAction`; `PopupsPresenter` (plain C#) picks the popup from `LevelWon`, `LevelLost`, `PauseChanged`, `LevelStarted` and `MenuOpened`, and answers the buttons through `IPopupCommands` (`FlowPopupCommands` adapts `GameFlow`). The popup hides the moment a button is pressed. The sound and music switches keep their state in the presenter and publish `AudioToggled`. The fade-in and the stars lighting up are USS transitions started by class changes; without a panel the final state is applied at once, which is what the tests see.
- **Main menu (D-023):** `Menu.uxml` is a second template in `Hud.uxml`, a sibling of the HUD root (the HUD is hidden while the menu shows). `IMenuView` / `MenuView` draw the logo and the Play button; `MenuPresenter` (plain C#) shows the menu on `MenuOpened` and hides it on `LevelStarted` or as soon as Play is pressed, then calls `RequestPlay`. It is told whether the flow is in the menu when the UI is built, because the flow publishes `MenuOpened` in its constructor, before anyone listens.
- **Shared controls:** `Controls.uss` holds the pill button (primary, secondary and large) and the switch, for every screen.
- `MovePathPlanner`: pure function from `MoveResult` + `GridLayout2D` to a `MovePlan` (start, target, bump point, travel in cells). All the animation geometry lives here so it is testable; the view only tweens between the points.
- `BoardCamera` + `CameraFraming`: orthographic framing of the board plus margin. Refits when the screen aspect changes (rotation, WebGL resize). The math is in `CameraFraming` for EditMode tests.
- `BoardBackdrop` (D-020): the layered night-cave background. It only reads the camera, so it also works in the menu. It owns a portrait set and a landscape set of five layers (base, far, motes, near, crystals) and shows the set that matches the camera aspect, not the platform. Each layer covers the view plus the margin its drift needs, and moves by depth from the pointer and a slow idle wander (mouse always; touch and pen only while pressed). The math lives in `Parallax` (pure, EditMode tests); tunables in `BackdropSettings`.
- Cell tiles: `BoardView` draws one tile sprite under every cell, reused between levels; scale and sorting order come from `BoardViewSettings`.
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

`LevelMetrics` (Core) measures a level's structure: bats, hidden bats, traps (bats blocked on the first move), waves (depth of the blocking chains) and a `Difficulty` heuristic. Since the minimum moves always equal the bat count (D-010), difficulty lives in those four numbers and in the move limit's slack. `LevelGenerator` (Core, with `SeededRandom` and `LevelSpec`) builds a level that matches a spec from a seed, deterministically, and keeps its difficulty in a window above the previous level's. It runs only in the editor: the `LevelGenerationPlan` asset (Editor assembly) holds one entry per level, and the Level Solver window writes the `LevelData` assets and fills the `LevelCatalog` from it (D-024).

## Platforms

- **WebGL**: no native ad SDK; uses the null/web ad provider. Keep the core allocation-light and avoid threading.
  - Built with `EchoCave > Build > WebGL (zip for itch.io)` (`Editor/BuildWebGL.cs`): always the Game scene, WebGL 2 only, Gzip with the decompression fallback, the `EchoCave` template in `Assets/WebGLTemplates` (dark loading screen with the logo and a progress bar in the palette, favicon from the app icon), output in `Builds/` (git-ignored) plus a zip with `index.html` at its root. The playtest is hosted on itch.io; see `PLAYTEST.md` (D-025).
- **Android (Play Store)**: AdMob or AppLovin MAX behind `IAdService`; IAP for remove-ads and ping packs.

## Level progression (v1, 10 levels)

1–3: no hidden bats, no move limit. 4–6: first hidden bat, generous pings. 7–10: more hidden bats, fewer pings; difficulty peak at 10.
