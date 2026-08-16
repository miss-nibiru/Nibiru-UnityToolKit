# Nibiru Unity Toolkit

Reusable Unity 6 gameplay systems and editor tools by Miss Nibiru.

This repository is both a package workspace and a demonstration project. Reusable code lives under `Packages/com.missnibiru.*`; project-side demo assets live under `Assets/_Project/00_NibiruToolKit`, with the main integration demo scene under `Assets/_Project/02_Scenes/ToolkitDEMO_Scene.unity`.

The toolkit is designed so another Unity project can adopt one domain without inheriting the architecture of the game the system originally came from.

## Start here

- Full architecture and divergence summary: [`Documentation~/FinalArchitectureSpecification.md`](Documentation~/FinalArchitectureSpecification.md)
- Waves authoring specification: [`Packages/com.missnibiru.waves/Documentation~/WaveLayoutBuilderSpecification.md`](Packages/com.missnibiru.waves/Documentation~/WaveLayoutBuilderSpecification.md)
- Toolkit Debugger specification: [`Packages/com.missnibiru.debugger/Documentation~/ToolkitDebuggerSpecification.md`](Packages/com.missnibiru.debugger/Documentation~/ToolkitDebuggerSpecification.md)
- Development Build stress-test procedure/record: [`Documentation~/DevelopmentBuildStressScenario.md`](Documentation~/DevelopmentBuildStressScenario.md)

## Package map

### Nibiru Core — `com.missnibiru.core`

Foundation systems that do not depend on another Miss Nibiru runtime package.

#### Health

Use for reusable health and damage contracts.

Key types:

- `HealthComponent`
- `IDamageable`
- `IHealthSource`
- `DamageCalculator`
- `DamageCalculationMode`

**Extend it:** implement `IDamageable` when an object needs custom damage handling without using `HealthComponent`; implement `IHealthSource` when a custom health implementation needs to expose health state to reusable consumers such as UI.

#### Patterns

Use for data-driven sequences of reusable tokens rather than hardcoded key combinations.

Key types:

- `PatternToken`
- `PatternDefinition`
- `PatternDatabase`
- `PatternResolver`

**Extend it:** create additional `PatternToken` and `PatternDefinition` ScriptableObjects. The resolver does not need to change when new sequences are added.

#### State Machine

Use for project-defined states that share a context and lifecycle.

Key types:

- `IState<TContext>`
- `StateMachine<TContext>`

**Extend it:** define your own context class/struct and implement `IState<TContext>` for each new state. The state machine itself remains generic.

---

### Nibiru Combat — `com.missnibiru.combat`

Reusable projectile attacks and damage-target resolution. Depends on Core.

#### Projectile attacks

Use when several weapons/enemies share the same projectile execution rules but require different authored values.

Key types:

- `ProjectileAttackConfiguration`
- `ProjectileAttackExecutor`
- `ProjectileSpreadMode`

**Extend it:** create new attack configuration assets first. Only add executor logic when a new attack cannot be represented by configuration.

#### Projectile emission and runtime

Key types:

- `IProjectileEmitter`
- `PrefabProjectileEmitter`
- `ProjectileSpawnRequest`
- `ProjectileActor`
- `DamageableResolver`

**Extend it:** implement `IProjectileEmitter` to replace prefab instantiation with another spawning strategy such as pooling, networking, or a project factory. Implement `IDamageable` on new damage targets rather than editing projectile collision code.

---

### Nibiru Enemies — `com.missnibiru.enemies`

Composable enemy lifecycle, movement, targeting, and attack behaviours. Depends on Core and Combat.

#### Enemy actor

Key types:

- `EnemyActor`
- `EnemyContext`

The actor coordinates independently replaceable behaviours instead of containing every enemy rule in one controller.

#### Movement

Contract:

- `IEnemyMovementBehaviour`

Included behaviours:

- `StationaryMovement`
- `ChaseMovement`
- `PatrolMovement`
- `FormationMovement`

**Extend it:** implement `IEnemyMovementBehaviour` for a new movement style such as flee, orbit, spline, navmesh, or boss-specific movement.

#### Attacks

Contract:

- `IEnemyAttackBehaviour`

Included behaviours include contact damage and projectile attacks.

**Extend it:** implement `IEnemyAttackBehaviour`. Reuse Nibiru Combat for normal projectile attacks instead of creating a second projectile framework.

#### Targeting

Contract:

- `IEnemyTargetProvider`

Default implementation:

- `TransformTargetProvider`

**Extend it:** implement another provider for nearest-target selection, threat, teams, dynamic player selection, or other targeting policy.

---

### Nibiru Information — `com.missnibiru.information`

Generic authored information and collection tracking for clues, evidence, codices, lore, discoveries, tutorials, or other player knowledge.

#### Authored data

Key types:

- `InformationEntry`
- `InformationPage`
- `InformationCategory`
- `InformationType`
- `InformationDatabase`

**Extend it:** add information/category/type assets. Presentation is intentionally separate, so another project can display the same information model through its own UI.

#### Collection state

Key types:

- `InformationCollection`
- `InformationCollectionResult`
- `IInformationCollectionStore`
- `InMemoryInformationCollectionStore`

**Extend it:** implement `IInformationCollectionStore` to connect collection state to a save file, PlayerPrefs, cloud data, or another project save service.

#### Unity integration and organizer

Key types/components include:

- `InformationCollectionComponent`
- `InformationSource`
- Information Organizer editor window/validator utilities

**Extend it:** use the runtime API directly or place the Unity bridge components in scenes. Interaction, dialogue, audio, and quest consequences remain project-owned.

---

### Nibiru Waves — `com.missnibiru.waves`

Reusable runtime wave spawning plus grid-based encounter authoring.

Key runtime types:

- `WaveData`
- `WaveSpawnGroupData`
- `WaveRunner`
- `IWaveSpawner`
- `WaveSpawner`
- `ISpawnPointProvider`
- `GridSpawnPointProvider`
- `TransformSpawnPointProvider`

Key layout types:

- `WaveLayoutData`
- `SpawnCatalog`
- `SpawnableDefinition`
- `SpawnFormationDefinition`
- `WaveLayoutCalculator`
- `WaveLayoutCompiler`
- `WaveSpawnedObject`

Editor tools:

- **Tools > Miss Nibiru > Wave Layout Builder**
- Formation Designer through the Waves authoring workflow

**Extend it:** implement `ISpawnPointProvider` for a new positioning strategy, implement `IWaveSpawner` for another spawning backend, add `SpawnableDefinition` assets for new content, and add `SpawnFormationDefinition` assets for reusable formations.

The package deliberately does not own enemy AI, room doors, cameras, or game progression.

---

### Nibiru UI — `com.missnibiru.ui`

Reusable UI components that bind to toolkit contracts instead of specific game actors. Depends on Core and Unity UI.

Current component:

- `HealthBarUI`

**Extend it:** add UI components that consume reusable interfaces/data. Keep scene flow, project-specific menus, and art-direction-specific screen logic in the consuming game.

---

### Toolkit Debugger — `com.missnibiru.debugger`

Read-only Unity Editor diagnostics for recurring project validation and debugging work.

Open from:

**Tools > Miss Nibiru > Toolkit Debugger**

It provides:

- Quick Scan
- Selection Scan
- Full Project Scan
- package and package-version checks
- assembly-definition checks
- missing/broken serialized reference checks
- stable-ID checks
- toolkit-specific configuration checks
- current editor-session live logs
- actionable issue descriptions and locations

Key types:

- `ToolkitDebuggerWindow`
- `ToolkitProjectScanner`
- `ToolkitDebugIssue`
- `ToolkitDebugReport`
- `ToolkitLogCapture`

**Extend it:** add deterministic, read-only scanner rules that emit `ToolkitDebugIssue` results with a useful severity, category, message, location, and suggested action.

See the package README and debugger specification for adoption details.

## Demo content

`Assets/_Project/00_NibiruToolKit` contains examples for several reusable systems, including health, state machines, patterns, combat configurations, information, and waves.

`Assets/_Project/02_Scenes/ToolkitDEMO_Scene.unity` is the integration scene used to exercise toolkit systems together while keeping the reusable packages independent of demo content.

## How to add a new reusable system

Before adding a new package or feature to an existing package, check four things:

1. **Is the problem reusable across projects?** If it only exists because of one game's progression, scene flow, or content, leave it in the game.
2. **Which package owns the domain?** Avoid placing unrelated behaviour into Core just because Core is shared.
3. **What is the extension boundary?** Prefer a small interface, generic contract, ScriptableObject data model, event, or provider over direct references to concrete game managers.
4. **Can it be tested without the original game?** Reusable runtime logic should be testable independently where practical.

## Architecture rules

- Runtime packages do not reference scripts under `Assets`.
- Editor tooling stays in Editor assemblies/folders.
- Combat reuses Core damage contracts.
- Enemies reuse Core and Combat instead of duplicating health/projectile systems.
- UI binds to reusable contracts rather than player/enemy concrete classes.
- Information persistence is provided through a storage contract.
- Waves owns spawning/encounter scheduling, not room progression or enemy AI.
- New systems should extend contracts before modifying all callers.

## Unity version

The embedded packages target Unity 6 (`6000.0` package compatibility baseline).

## Tests

EditMode tests are included inside the package folders for the major reusable domains. Run the Unity Test Framework after changing package runtime/editor behaviour.

Integration behaviour can also be checked through `ToolkitDEMO_Scene`.
