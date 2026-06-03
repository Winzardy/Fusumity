# Fusumity — Overview

> This is an OVERVIEW-level doc. Fusumity is a git submodule (separate repo). Do not commit changes here — the orchestrator handles git.
> Code is the source of truth. Every concrete claim cites `file_path:line`.
> Related docs: [Sapientia](../Sapientia/CLAUDE.md) · [Architecture](../../../Docs/Core/ARCHITECTURE.md)

## 1. Purpose

Fusumity is a **Unity infrastructure and lifecycle library** used as a git submodule across the project. It provides two primary areas:

1. **Reactive / lifecycle bridge** (`Reactive/`): a `UnityLifecycle` singleton `MonoBehaviour` that exposes static `DelayableAction` event hooks (`UpdateEvent`, `LateUpdateEvent`, `FixedUpdateEvent`, `EndOfFrameEvent`, etc.) and `EachSecond*` timers, bridging Unity's callback-driven runtime to a subscribe/unsubscribe model used throughout the codebase.
2. **Infrastructure** (`Infrastructure/`): a large collection of subsystem frameworks for Booting/Bootstrap, UI (screens, popups, layouts, MVVM, localization), Content/Addressables, Analytics, Audio, Advertising, In-App Purchasing, In-App Review, Input, Localization, Messaging, Migration, Notifications, and Presenters.

Additionally it contains `Attributes/`, `Collections/`, `Editor/`, `Gizmo/`, and `Utility/` support folders.

## 2. Where it lives

- **Folder:** `Assets/Submodules/Fusumity/` (git submodule — separate repo).
- **Assembly:** `Assets/Submodules/Fusumity/Fusumity.asmdef` — assembly name `Fusumity`, `allowUnsafeCode: true`. References: `UniTask`, `Sapientia`, `Unity.Addressables`, `Unity.ResourceManager`, `Unity.Collections`, `Unity.Mathematics`, `Unity.Burst`, `ShapesEditor`, `ShapesRuntime`. Auto-referenced (`autoReferenced: true`).
- Additional asmdefs: `Fusumity.Editor.asmdef` (editor tools), `Fusumity.LocalSave.asmdef`, `UserLocator.asmdef`, `Fusumity.Utility.Camera.asmdef` (sub-utilities). `Infrastructure/Booting/Booting.asmdef` is a separate assembly for the bootstrap system.
- **Namespaces:** `Fusumity.Reactive` (`UnityLifecycle`), `Booting` (bootstrap), `Fusumity.Utility` (utilities), UI namespaces within `Infrastructure/UI/`, and many feature-specific namespaces.

## 3. Key types & entry points

- `Reactive/UnityLifecycle.cs:9` — **`UnityLifecycle`** (`MonoBehaviour`, `partial`): the central static event hub. **Start here.** Static fields: `UpdateEvent`, `LateUpdateEvent`, `FixedUpdateEvent`, `OnGUIEvent`, `EndOfFrameEvent` (`DelayableAction`); `EachSecondEvent`, `UnscaledEachSecondEvent`, `FixedEachSecondEvent`, `FixedUnscaledEachSecondEvent` (1-second heartbeats, capped at 20 tick catch-up). Also exposes `ApplicationPauseEvent`, `ApplicationResumeEvent`, `ApplicationShutdown`, `ApplicationFocusEvent`, `ApplicationUnfocusEvent`, `GestureBackEvent`, `LateExecuteOnceEvent`, `ResolutionChangedEvent`. Resolves `DeltaTime`, `ApplicationQuitting`, `ApplicationPause`, and `ApplicationCancellationToken`.
- `Reactive/UnityLifecycle.Singleton.cs` — singleton setup for `UnityLifecycle` (likely `[DefaultExecutionOrder]` + `DontDestroyOnLoad`).
- `Reactive/UnityLifecycle.Coroutine.cs` — coroutine helpers on `UnityLifecycle`.
- `Reactive/TimeEvent.cs` — `TimeEvent` data struct used by `UnityLifecycle.UpdateTimeEvents` for time-gated callbacks.
- `Infrastructure/Booting/Bootstrap.cs:24` — **`Bootstrap`** (`MonoBehaviour`, `[DefaultExecutionOrder(-2000)]`): sequential task runner. Holds an `IBootTask[]` array; runs each task's `RunAsync(Blackboard, token)` in order, then calls `OnBootCompleted` on all completed tasks. Editor button auto-fills the task list by reflecting all `IBootTask` implementations (sorted by `Priority`).
- `Infrastructure/Booting/BaseBootTask.cs` — **`BaseBootTask`**: abstract base for all boot tasks; implements `IBootTask`. Subclassed by `CheatsBootTask`, `LogBootTask`, and all game-specific boot tasks.
- `Infrastructure/Booting/IBootTask.cs` — **`IBootTask`** interface: `Active`, `Priority`, `RunAsync`, `OnBootCompleted`, `Dispose`.
- `Infrastructure/Content/` — Addressables content loading framework (details **unknown** at overview level).
- `Infrastructure/MVVM/` — MVVM presentation layer (details **unknown** at overview level).
- `Infrastructure/UI/Screens/UIScreen.cs` — UI screen base; `Infrastructure/UI/Popups/UIPopupManager.cs` — popup queue management.
- `Infrastructure/Messaging/` — messaging infrastructure (distinct from `Sapientia`'s `Messenger`; relationship **unknown** at overview level).
- `Infrastructure/Migration/` — data migration tooling for Fusumity-level data.
- `Utility/` — `ComponentUtility`, `GameObjectUtility`, `IOUtility`, `ColorUtility`, `GizmosUtility`, `EventSystemUtility`, `Blackboard`, `Camera/`, `LocalSave/`, `UserLocator/`.

## 4. Data / State / Logic / View breakdown

Fusumity is plumbing/infrastructure, not a feature layer. It does not use the `Game.Core` `Generic → Data → State → Logic → View` hierarchy.

- **Reactive/lifecycle:** `UnityLifecycle` is the Unity-to-subscriber bridge — pure event routing.
- **Booting:** `Bootstrap` + `IBootTask`/`BaseBootTask` — sequential async startup sequencing.
- **Infrastructure:** UI, Content, Analytics, Audio, etc. — managed `MonoBehaviour`-based frameworks for platform-specific concerns (out of deep-doc scope for A3-Support; handled by respective feature teams).
- **Utility:** stateless helper methods and editor tooling.

## 5. Lifecycle & tick

- `Bootstrap` runs at `DefaultExecutionOrder(-2000)` — before all normal `MonoBehaviour.Start` calls. It runs `RunTasksAsync` sequentially through `tasks[]`, awaiting each `IBootTask.RunAsync`. Boot tasks typically register services into `ServiceLocator`, initialize subsystems, and subscribe to `UnityLifecycle` events.
- `UnityLifecycle` drives the `UpdateEvent`/`LateUpdateEvent`/`FixedUpdateEvent` etc. from Unity's `Update`/`LateUpdate`/`FixedUpdate` callbacks (`UnityLifecycle.cs:52-89`). All subscribers are invoked synchronously via `ImmediatelyInvoke()`.
- `EachSecond*` events fire at most once per second of accumulated time, with a per-frame tick catch-up cap of 20 (`UnityLifecycle.cs:145-162`).
- The `GameRuntime` (in `Game.Core`) subscribes its own `Update`/`LateUpdate` to `UnityLifecycle` events to drive the simulation tick — Fusumity is therefore the bridge between Unity's frame loop and the simulation.

## 6. Dependencies

- **Depends-on:** `Sapientia` (for `Blackboard`, `SafePtr`/data utilities, `DelayableAction`), `UniTask`, Unity Addressables/Resources, Unity Collections, Unity Burst/Mathematics. External SDKs (Analytics, Advertising, IAP, etc.) are referenced per-subsystem.
- **Depended-by:** virtually all game assemblies depend on `Fusumity` (it is `autoReferenced: true`). `Game.Core` uses `UnityLifecycle` for tick driving and `Booting` for startup tasks. `Game.Cheats` uses `UnityLifecycle.UpdateEvent` and `BaseBootTask`.

## 7. Gotchas & invariants

- **`DelayableAction`** — `ImmediatelyInvoke()` is called synchronously on the main thread. Subscribers that throw will break the whole invoke chain for that frame unless the implementation catches exceptions internally (unknown from listing).
- **`UnityLifecycle` is a singleton** — `_instance` is referenced throughout (e.g. `ApplicationQuitting`, `ApplicationCancellationToken`). Destroying or creating a second instance during play will likely cause `NullReferenceException` or stale-instance issues.
- **`Bootstrap` runs `tasks[]` strictly sequentially** — a slow async task blocks all subsequent tasks. Long boot tasks cause visible load-time delays.
- **`UniTask`/`CancellationToken`** used throughout — `CancellationToken` is the `MonoBehaviour.destroyCancellationToken`; if the Bootstrap `GameObject` is destroyed mid-boot, remaining tasks are cancelled.
- **Resolution tracking** (`UnityLifecycle.Resolution.cs:5`) — marked `//TODO: Resolution???`; the feature is incomplete or experimental.

## 8. Open questions / TODO / risks

- `Assets/Submodules/Fusumity/Reactive/UnityLifecycle.Resolution.cs:5` — `//TODO: Resolution???` — incomplete feature.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Assistents/UITextLocalizationAssigner.cs:11-12` — two TODOs: (1) add pooling for `TextLocalizationArgsPool`; (2) localization updates fire on disabled widget TMPs.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Decorators/StateSwitcher/Abstract/Reliable/ReliableTweenStateSwitcher.cs:127` — `//TODO: Not working properly atm.` in `PlayTweenCached`.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Screens/UIScreen.cs:25` — TODO: window stays in queue if never closed (PauseScreen case).
- `Assets/Submodules/Fusumity/Infrastructure/UI/Layouts/UIBaseLayout.cs:14` — TODO: split animation block into a separate class.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Layouts/UIBaseLayout.Animations.cs:44` — TODO: move to Overlay.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Layouts/Generic/UILabeledButtonLayout.cs:42` — TODO: remove, use `StateSwitcher<string>`.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Popups/UIPopupManager.cs:22` — TODO: Default popup opened over Standalone popup starts processing the queue.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Popups/UIPopupDispatcher.cs:71` — TODO: return `UIRequestToken<T>` instead of `T`.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Popups/UIPopup.cs:27` — TODO: same unclosed-window queue issue.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Generic/SafeAreaFitter.cs:11` — TODO: per-platform/per-device safe area settings.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Generic/UILineRenderer.cs:8` — TODO: add Transform-based line construction and custom Editor.
- `Assets/Submodules/Fusumity/Infrastructure/UI/Generic/ImageSlicedCircleRadiusCalculator.cs:8` — TODO: fix (unspecified bug).
- `Assets/Submodules/Fusumity/Infrastructure/UI/Layouts/Generic/Localization/UILocalizedBaseLayout.Editor.cs:9` — TODO: serializable dictionary for localization tags/arguments.
- **Large `Infrastructure/` sub-tree is not deep-documented here** — UI/Content/Analytics/Audio/etc. subsystems are **unknown** at overview level; deep docs are out of A3-Support scope.
