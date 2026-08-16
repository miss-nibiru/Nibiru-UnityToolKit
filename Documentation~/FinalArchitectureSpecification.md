# Nibiru Unity Toolkit — Final Architecture Specification

## 1. Purpose

Nibiru Unity Toolkit is a collection of reusable Unity 6 systems extracted and refactored from project-specific gameplay code. The final architecture separates reusable runtime behaviour from project content, editor tooling, and game-specific orchestration so that the same systems can be adopted by unrelated Unity projects without copying the original game architecture.

The toolkit is divided into independently versioned packages with explicit responsibilities:

- `com.missnibiru.core`
- `com.missnibiru.combat`
- `com.missnibiru.enemies`
- `com.missnibiru.information`
- `com.missnibiru.waves`
- `com.missnibiru.ui`
- `com.missnibiru.debugger`

The repository also contains demo assets and scenes under `Assets/_Project/00_NibiruToolKit` and `Assets/_Project/02_Scenes` so the packages can be exercised without coupling the packages themselves to demo content.

## 2. Architectural goals

The final architecture follows these goals:

1. **Reusable before project-specific.** Core behaviour is expressed through small runtime APIs, interfaces, ScriptableObjects, and components rather than direct references to one game's managers or scenes.
2. **Composable systems.** Movement, attacks, targeting, spawning, information storage, patterns, and state transitions can be replaced independently.
3. **Data separate from execution.** ScriptableObjects hold reusable configuration while runtime services and components perform execution.
4. **Explicit package ownership.** Each package owns one domain and avoids absorbing unrelated game logic.
5. **Editor tooling remains editor-only.** Authoring and diagnostic windows are separated from runtime assemblies.
6. **Extension through contracts.** Interfaces and generic APIs provide intended extension seams instead of requiring edits to existing package code.
7. **Backward-compatible authoring where practical.** New authoring workflows are added without requiring a second competing runtime system.
8. **Testable units.** Core behaviour is covered by EditMode tests at package level.

## 3. Package dependency map

The intended dependency direction is:

```text
Core
├── Combat
│   └── Enemies
└── UI

Information   (standalone runtime domain)
Waves         (standalone runtime domain)
Debugger      (editor-only diagnostics across the project)
```

Detailed dependency rules:

- **Core** does not depend on another Miss Nibiru runtime package.
- **Combat** depends on Core for shared health/damage contracts.
- **Enemies** depends on Core and Combat.
- **UI** depends on Core and Unity UI.
- **Information** is designed as an independent runtime domain.
- **Waves** is designed as an independent runtime spawning/encounter domain.
- **Debugger** is editor-only and inspects packages, assemblies, assets, scenes, and toolkit configuration without becoming a runtime dependency.

Project code may depend on several packages at once. Packages should not depend on project-specific scripts in `Assets`.

---

# 4. Core package

Package: `com.missnibiru.core`

## 4.1 Responsibility

Core contains small, project-agnostic systems that other packages can build on. It currently owns three reusable areas:

- health and damage contracts
- pattern definition/resolution
- generic state machines

## 4.2 Health system

Main types:

- `IDamageable`
- `IHealthSource`
- `HealthComponent`
- `DamageCalculator`
- `DamageCalculationMode`

`HealthComponent` is the standard reusable Unity health implementation. Other systems should interact through the public health/damage contracts rather than requiring a specific player or enemy class.

### Extension point

Implement `IDamageable` for any object that receives damage without using `HealthComponent`, or implement `IHealthSource` when another component needs to expose health state to UI or gameplay systems.

`DamageCalculator` can be reused by systems that need the same damage rules without introducing a combat-manager dependency.

## 4.3 Pattern system

Main types:

- `PatternToken`
- `PatternDefinition`
- `PatternDatabase`
- `PatternResolver`

Patterns are data-driven. A `PatternToken` represents a reusable input or semantic token. `PatternDefinition` defines a valid sequence and its result. `PatternDatabase` stores definitions, and `PatternResolver` resolves input sequences against the configured database.

The system does not assume that tokens are keyboard keys. They can represent directions, controller inputs, commands, symbols, dialogue actions, or other project-defined concepts.

### Extension point

Create new `PatternToken` and `PatternDefinition` assets rather than modifying the resolver. A project can interpret a resolved pattern however it wants after resolution.

## 4.4 State machine

Main types:

- `IState<TContext>`
- `StateMachine<TContext>`

The state machine is generic over a caller-defined context. It owns the lifecycle of the current state and invokes `Exit`, `Enter`, and `Tick` at the appropriate times. `StateChanged` allows surrounding systems to observe transitions without becoming part of the state implementation.

### Extension point

Define a project context type and implement `IState<TContext>` for each state. No subclass of `StateMachine<TContext>` is required for normal use.

---

# 5. Combat package

Package: `com.missnibiru.combat`

## 5.1 Responsibility

Combat owns reusable projectile execution and damage-target resolution. It depends on Core but does not own enemy AI, player controls, wave logic, or UI.

Main areas:

- projectile attack configuration
- attack execution
- projectile emission
- projectile lifetime/hit behaviour
- damageable lookup

## 5.2 Projectile attack configuration

Main types:

- `ProjectileAttackConfiguration`
- `ProjectileAttackExecutor`
- `ProjectileSpreadMode`

Attack behaviour is configured as data and executed by shared logic rather than duplicated in every weapon or enemy script. Configuration controls values such as projectile count, spread, timing, speed, damage, and lifetime according to the fields exposed by the current implementation.

This allows multiple attacks such as one-shot, rapid fire, burst, fan, or shotgun-style patterns to use the same executor with different data.

### Extension point

Create additional attack configuration assets for new attack patterns. Extend configuration/execution only when a genuinely new execution rule cannot be represented by existing data.

## 5.3 Projectile emission

Main types:

- `IProjectileEmitter`
- `PrefabProjectileEmitter`
- `ProjectileSpawnRequest`
- `ProjectileActor`

`IProjectileEmitter` separates attack execution from the mechanism used to provide a projectile. `PrefabProjectileEmitter` is the default implementation and instantiates a configured projectile prefab.

`ProjectileSpawnRequest` carries the runtime launch data. `ProjectileActor` owns projectile travel, lifetime, collision handling, owner filtering, and damage application.

### Extension point

Implement another `IProjectileEmitter` to change projectile provisioning without changing attack execution. Examples include object pooling, network-authoritative spawning, ECS-backed projectiles, or a project-specific factory.

## 5.4 Damage resolution

`DamageableResolver` resolves collision targets to the reusable `IDamageable` contract. This keeps projectile code independent of concrete player/enemy health classes.

### Extension point

New damageable objects normally only need to implement `IDamageable`; projectile code does not need to be changed.

---

# 6. Enemies package

Package: `com.missnibiru.enemies`

## 6.1 Responsibility

Enemies provides a composable enemy actor architecture. It owns lifecycle coordination, movement behaviour, attack behaviour, and target provision while delegating generic health and projectile concerns to Core and Combat.

Main areas:

- actor/context
- movement behaviours
- attack behaviours
- targeting

## 6.2 Actor and context

Main types:

- `EnemyActor`
- `EnemyContext`

`EnemyActor` coordinates the configured behaviours. `EnemyContext` supplies the shared runtime references required by those behaviours.

The actor is intentionally not a monolithic enemy implementation. Behaviour is delegated to contracts so different enemies can be assembled from different capabilities.

## 6.3 Movement

Contract:

- `IEnemyMovementBehaviour`

Current implementations:

- `StationaryMovement`
- `ChaseMovement`
- `PatrolMovement`
- `FormationMovement`
- `MovementPlane`
- `PatrolLoopMode`

The movement contract exposes initialization and lifecycle methods so the actor can activate, tick, and deactivate movement consistently.

### Extension point

Implement `IEnemyMovementBehaviour` for a new movement model. Examples include flee, orbit, spline, navmesh, knockback-controlled, flying, or boss-phase movement.

Movement should remain responsible for movement only; attacks and target acquisition belong to their own contracts.

## 6.4 Attacks

Contract:

- `IEnemyAttackBehaviour`

Current implementations include:

- `ContactDamageAttack`
- `EnemyProjectileAttack`

Projectile attacks reuse the Combat package rather than implementing a second enemy-only projectile system.

### Extension point

Implement `IEnemyAttackBehaviour` for a new attack style. Where the attack fires normal projectiles, prefer composing the existing Combat system rather than duplicating projectile rules.

## 6.5 Targeting

Contract:

- `IEnemyTargetProvider`

Default implementation:

- `TransformTargetProvider`

### Extension point

Implement another provider for dynamic target selection, nearest-target search, threat systems, team targeting, or other project-specific target rules.

---

# 7. Information package

Package: `com.missnibiru.information`

## 7.1 Responsibility

Information is a generic system for authored information entries and player/runtime collection state. It is intentionally broader than an evidence-only implementation so it can support codices, clues, documents, discoveries, lore, tutorials, quest knowledge, or other collectable information.

The package separates:

- authored information data
- runtime collection state
- Unity scene components
- editor authoring/validation tools

## 7.2 Authored data

Main types:

- `InformationEntry`
- `InformationPage`
- `InformationCategory`
- `InformationType`
- `InformationDatabase`

Entries may be organized by reusable categories and types and stored in a database. Pages allow an entry to contain structured/multi-page authored content rather than forcing all information into one string.

### Extension point

Projects create new data assets and categories/types without modifying runtime collection code. Presentation remains project-owned and can interpret the information assets differently.

## 7.3 Collection runtime

Main types:

- `InformationCollection`
- `InformationCollectionResult`
- `IInformationCollectionStore`
- `InMemoryInformationCollectionStore`

`InformationCollection` owns collection rules while the store contract owns persistence of collected IDs.

### Extension point

Implement `IInformationCollectionStore` to persist collection state using a save file, cloud storage, PlayerPrefs, a project save service, or another backend. The collection system itself does not need to know how persistence works.

## 7.4 Unity integration

Main components:

- `InformationCollectionComponent`
- `InformationSource`

These bridge reusable data/runtime logic into scenes and GameObjects.

### Extension point

Projects can call the collection API directly or use the provided components. Interaction prompts, dialogue, UI, audio, and quest logic remain external consumers.

## 7.5 Editor tooling

The Information Organizer editor code supports creation, editing, validation, and organization of information assets. Editor assemblies remain separate from runtime assemblies.

---

# 8. Waves package

Package: `com.missnibiru.waves`

## 8.1 Responsibility

Waves owns reusable timed spawning and grid-authored encounter layouts. It does not own enemy AI, room doors, cameras, combat rules, or project progression.

It supports both:

1. runtime wave sequences based on `WaveData[]`; and
2. authored `WaveLayoutData` compiled into spawn instructions.

Both authoring approaches use the same runtime `WaveRunner`; the package does not introduce parallel competing runtimes.

## 8.2 Runtime wave data

Main types:

- `WaveData`
- `WaveSpawnGroupData`
- `WaveRunner`

`WaveRunner` controls sequence timing, wave progression, active object tracking, completion, and transition between waves.

### Extension point

Project flow calls `WaveRunner.StartSequence()` when an encounter should begin and subscribes to/uses runner state as needed. Room-state logic remains external.

## 8.3 Spawning contracts

Main types:

- `IWaveSpawner`
- `WaveSpawner`
- `ISpawnPointProvider`
- `GridSpawnPointProvider`
- `TransformSpawnPointProvider`

The runtime separates **what should spawn** from **where/how spawn points are provided**.

### Extension point

Implement `ISpawnPointProvider` for another positioning source or `IWaveSpawner` for another spawning backend.

## 8.4 Authored layouts

Main types:

- `WaveLayoutData`
- `SpawnCatalog`
- `SpawnableDefinition`
- `SpawnFormationDefinition`
- `WaveLayoutCalculator`
- `WaveLayoutCompiler`
- `WaveLayoutGeometry`
- `WaveSpawnedObject`

The editor stores encounter intent as data. Planning/compiler classes convert that data into runtime-ready spawn instructions without requiring real enemies to exist while the encounter is authored.

### Extension point

Add new `SpawnableDefinition` assets for enemies, hazards, pickups, or other prefabs. Add reusable formations through `SpawnFormationDefinition`. The catalog and layout model should remain content-agnostic.

## 8.5 Editor tools

Main editor classes:

- `WaveLayoutBuilderWindow`
- `SpawnFormationDesignerWindow`
- `WaveLayoutValidator`
- `WaveLayoutEditorUtility`
- `WaveLayoutScenePreview`

The Wave Layout Builder provides grid-based encounter authoring, palette management, timing, validation, and preview without requiring source-code edits.

See `Packages/com.missnibiru.waves/Documentation~/WaveLayoutBuilderSpecification.md` for the dedicated tool specification.

---

# 9. UI package

Package: `com.missnibiru.ui`

## 9.1 Responsibility

UI contains reusable presentation components that depend on toolkit runtime contracts rather than concrete game actors.

Current reusable component:

- `HealthBarUI`

`HealthBarUI` consumes health information from the Core health domain rather than being written specifically for one player or enemy prefab.

### Extension point

Additional toolkit UI should follow the same rule: bind to reusable interfaces/data and keep game-specific art direction, screen flow, and scene orchestration outside the package.

---

# 10. Debugger package

Package: `com.missnibiru.debugger`

## 10.1 Responsibility

Toolkit Debugger is a read-only Unity Editor diagnostic tool. It automates repeated inspection of package, assembly, asset, scene, and toolkit configuration problems.

Main types:

- `ToolkitDebuggerWindow`
- `ToolkitProjectScanner`
- `ToolkitDebugIssue`
- `ToolkitDebugReport`
- `ToolkitLogCapture`

The window is opened from:

`Tools > Miss Nibiru > Toolkit Debugger`

## 10.2 Scan modes

- **Quick Scan** — package, assembly, open-scene, and toolkit ScriptableObject checks.
- **Selection Scan** — checks selected folders, assets, and scene objects.
- **Full Project** — checks the wider project asset set.

## 10.3 Output

Findings are represented as typed issues with:

- severity
- category
- code
- message
- suggested action
- asset path/context when available

The tool also captures live Unity log output for the current editor session.

## 10.4 Safety boundary

The debugger is diagnostic and read-only. Scans should not silently mutate project content. Fixes remain intentional developer actions.

### Extension point

Add scanner rules that emit `ToolkitDebugIssue` instances for new toolkit packages or project conventions. New checks should remain deterministic, actionable, and read-only.

See `Packages/com.missnibiru.debugger/Documentation~/ToolkitDebuggerSpecification.md` for the dedicated tool specification.

---

# 11. Cross-package integration rules

1. **Depend downward, not sideways without reason.** New package dependencies must be justified by domain ownership.
2. **Prefer interfaces at boundaries.** Systems should consume `IDamageable`, `IProjectileEmitter`, `IEnemyMovementBehaviour`, `IEnemyAttackBehaviour`, `IEnemyTargetProvider`, `IInformationCollectionStore`, `ISpawnPointProvider`, or `IWaveSpawner` where replacement is expected.
3. **Do not put game managers in toolkit packages.** Scene transitions, quests, room locking, win conditions, player input maps, and narrative progression belong to the consuming project.
4. **Do not duplicate shared behaviour.** Enemy projectile attacks should reuse Combat. UI health display should reuse Core health state. New authored encounter tools should compile into the existing Waves runtime.
5. **Keep editor code out of runtime assemblies.** Editor windows, validators, and authoring helpers live in Editor assemblies/folders.
6. **Use ScriptableObjects for reusable authored data.** Behaviour that designers should configure repeatedly belongs in assets rather than copied MonoBehaviour values when practical.
7. **Preserve stable extension seams.** Prefer adding an implementation of an existing contract over editing every caller.

---

# 12. Testing strategy

The repository contains EditMode tests for the major reusable domains, including Core, Combat, Enemies, Information, Waves, and Debugger behaviour.

Tests are intended to protect reusable contracts during refactoring. New fixes to package behaviour should add or update tests at the package level where the behaviour can be isolated.

Demo scene verification remains useful for integration, but it does not replace package-level tests.

---

# 13. Divergence summary

The final architecture differs from the earlier project-specific implementation in several deliberate ways.

## 13.1 Monolithic/project scripts → independent packages

**Revision:** Reusable systems were moved into separate Unity packages with their own runtime/editor assemblies and tests.

**Reasoning:** Copying project folders does not create a reusable architecture. Package boundaries make ownership, dependencies, and portability explicit.

## 13.2 Concrete game-state flow → generic state machine

**Revision:** State handling became `StateMachine<TContext>` with `IState<TContext>`.

**Reasoning:** A state machine should not know the original game's battle manager or state names. A generic context allows the same transition mechanism to support combat, menus, AI, dialogue, encounters, or other domains.

## 13.3 Hardcoded attack implementations → shared projectile configuration/executor

**Revision:** Projectile attacks were consolidated around reusable configuration, shared execution, spawn requests, and an emitter contract.

**Reasoning:** One-shot, rapid, burst, fan, and similar attacks mostly differ in data. Centralizing execution removes duplicated timing/spread/projectile logic and gives future projects one extension point.

## 13.4 Enemy-specific logic → composable enemy behaviours

**Revision:** Enemy movement, attacks, and targeting were split into contracts and implementations.

**Reasoning:** Enemy reuse requires mixing behaviour rather than duplicating entire enemy controllers. This also made 2D/3D or different movement-plane use cases easier to support without separate enemy frameworks.

## 13.5 Evidence-specific collection → generic information system

**Revision:** The collection model was generalized into information entries, pages, categories, types, databases, and a pluggable collection store.

**Reasoning:** The underlying problem is broader than evidence. The same model can support clues, codices, lore, discoveries, tutorials, or knowledge tracking in future projects.

## 13.6 Direct persistence assumption → storage abstraction

**Revision:** Collected information is accessed through `IInformationCollectionStore` with an in-memory default implementation.

**Reasoning:** Persistence strategy is project-specific. The package should own collection semantics, not force a save technology.

## 13.7 Basic wave spawning → shared runtime plus authored layout system

**Revision:** Waves now support reusable runtime sequences and exact grid-authored layouts/formations through one `WaveRunner`.

**Reasoning:** Encounter authoring needed better designer tooling without creating a second runtime implementation. Authored layouts compile into the existing runtime path.

## 13.8 Manual recurring project inspection → Toolkit Debugger

**Revision:** Repeated checks for package, assembly, asset, scene, ID, and toolkit configuration errors were formalized into a read-only editor diagnostic tool.

**Reasoning:** The recurring debugging workflow was slow and required source knowledge. A dedicated tool makes common checks repeatable and usable by another developer from the Unity menu.

---

# 14. Non-goals

The toolkit does not attempt to provide a complete game framework. The following remain intentionally project-owned unless promoted into a reusable package later:

- scene progression
- save-game orchestration
- input schemes
- quest/game-specific objectives
- room locking and encounter triggers
- cameras
- narrative flow
- project-specific UI screens and art direction
- networking policy
- project-specific object pooling policy

The rule for future extraction is: a feature becomes toolkit code only when it represents a stable, reusable problem with a clear package responsibility and extension boundary.
