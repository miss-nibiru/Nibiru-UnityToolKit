# Wave Layout Builder Specification

Package: `com.missnibiru.waves`

Tool: **Tools > Miss Nibiru > Wave Layout Builder**

## 1. Problem

Wave and encounter authoring is repetitive when designers must manually place spawn transforms, duplicate enemy references, coordinate timing by hand, and inspect runtime scripts to understand how an encounter is assembled.

The Wave Layout Builder provides a visual, data-driven authoring workflow for exact encounter layouts while keeping the runtime spawning system reusable and independent of any one game.

## 2. Goals

The tool must:

- allow encounters to be authored on a configurable grid;
- support side-view (`XY`) and top-down/3D (`XZ`) placement planes;
- represent enemies, hazards, pickups, and other prefabs through reusable spawnable assets;
- support reusable formations;
- allow per-wave and per-placement timing configuration;
- validate authored data before runtime use;
- preview authored positions without requiring real runtime enemies to be spawned in Edit Mode;
- compile authored layout data into the existing Waves runtime rather than introducing a separate encounter runner;
- allow another developer/designer to author a wave without reading package source.

## 3. Non-goals

The Waves package and Wave Layout Builder do not own:

- enemy AI or movement;
- combat behaviour;
- room doors or room locking;
- cameras;
- scene progression;
- quest logic;
- player input;
- win/lose rules;
- project-specific pooling policy.

Those systems may react to Waves runtime events/state, but they remain external.

## 4. Core authored assets

### 4.1 `WaveLayoutData`

`WaveLayoutData` is the root ScriptableObject for an authored encounter layout.

It stores:

- `SpawnCatalog` reference;
- grid columns;
- grid rows;
- grid plane (`XY` or `XZ`);
- cell size;
- active enemy budget;
- ordered list of authored waves.

Grid dimensions are clamped to the package maximum of 100 × 100 cells. Cell size must be greater than zero, and active enemy budget must be at least one.

### 4.2 `SpawnCatalog`

The catalog defines which reusable spawnable assets are available to an encounter authoring workflow.

The catalog is a content palette, not a runtime enemy manager.

### 4.3 `SpawnableDefinition`

A spawnable asset describes content that may be placed in a layout.

It stores:

- display name;
- prefab;
- icon;
- kind;
- tags;
- grid footprint;
- footprint pivot.

Supported kinds are:

- Enemy
- Hazard
- Pickup
- Other

A spawnable may occupy more than one cell. The footprint and pivot are validated/clamped so authoring code can reason about occupied grid space safely.

### 4.4 `SpawnFormationDefinition`

A formation is a reusable collection of cell offsets. It allows a designer to author a shape once and apply it to multiple spawnables or placements.

Formation transformation is stored on each placement so the same formation asset can be rotated or flipped without duplicating the formation asset.

## 5. Wave model

Each `WaveLayoutWave` stores:

- wave name;
- initial delay;
- whether the wave uses a fixed duration;
- fixed duration value;
- whether the wave waits for active spawned objects to clear;
- whether the next authored wave starts automatically;
- whether active objects should be despawned on completion;
- ordered placement list.

This separates wave-level scheduling from placement-level scheduling.

## 6. Placement model

Each `WaveLayoutPlacement` stores:

- stable placement ID;
- enabled state;
- spawnable reference;
- optional formation reference;
- grid cell;
- 0/90/180/270-degree rotation;
- horizontal flip;
- vertical flip;
- spawn delay;
- sequential formation spawning flag;
- sequence interval;
- repetition count;
- repeat interval.

Duplicating a placement produces a new placement ID so duplicate content does not create duplicate identity.

## 7. Authoring workflow

A normal user workflow is:

1. Open **Tools > Miss Nibiru > Wave Layout Builder**.
2. Create or assign a `WaveLayoutData` asset.
3. Create or assign a `SpawnCatalog`.
4. Create `SpawnableDefinition` assets for the prefabs that should be available.
5. Configure each spawnable's prefab, icon, kind, tags, footprint, and pivot.
6. Add or select an authored wave.
7. Drag/place spawnables onto the grid.
8. Optionally assign a formation and configure rotation/flipping.
9. Configure placement timing, sequencing, and repetition.
10. Configure wave delay/duration/clear/progression behaviour.
11. Validate the layout.
12. Save the authored asset.
13. In the runtime scene, assign the layout to a `WaveRunner` and provide the required origin/spawning setup.
14. Trigger `WaveRunner.StartSequence()` from project-specific encounter flow.

The tool should expose normal authoring actions through UI and contextual editing instead of requiring source changes.

## 8. Coordinate model

The authoring grid is converted into world positions using an origin transform plus the layout's grid plane and cell size.

### `XY`

Use for side-view/2D layouts where authored columns/rows map to world X/Y.

### `XZ`

Use for top-down or 3D layouts where authored columns/rows map to world X/Z.

The origin transform supplies world position and rotation so the same authored layout can be positioned/oriented differently in different scenes.

## 9. Runtime architecture

The authored layout does not replace the Waves runtime.

The main runtime path remains:

```text
WaveLayoutData
    ↓
WaveLayoutCompiler / WaveLayoutCalculator / WaveLayoutGeometry
    ↓
compiled spawn instructions
    ↓
WaveRunner
    ↓
IWaveSpawner / WaveSpawner
    ↓
spawned runtime objects
```

`WaveRunner` also continues to support existing `WaveData[]` sequences. When an authored `WaveLayoutData` is assigned, the runner uses the authored layout path automatically. This keeps one source of runtime sequence behaviour rather than adding a second runner.

## 10. Runtime spawning contracts

### `IWaveSpawner`

Defines the spawning backend expected by runtime wave execution.

**Extension:** implement another spawner when a project needs pooling, networking, dependency injection, or another object-creation policy.

### `ISpawnPointProvider`

Defines a source of spawn positions for non-layout/runtime wave sequences.

Included implementations:

- `GridSpawnPointProvider`
- `TransformSpawnPointProvider`

**Extension:** implement a provider for splines, navmesh samples, procedural points, authored volumes, or another positioning system.

## 11. Active object tracking

`WaveSpawnedObject` allows runtime-spawned content to tell the runner when it is no longer active for wave-completion purposes.

A tracked object can release itself explicitly. Disabling or destroying the object also releases it according to the runtime implementation.

This allows `waitForActiveObjectsToClear` to operate without the Waves package needing to understand enemy health, AI, or project-specific death systems.

## 12. Timing rules

### Wave timing

- **Initial Delay** waits before scheduling the wave.
- **Timed Wave** limits scheduling/completion according to the configured duration.
- **Wait For Clear** prevents completion/progression until tracked active objects are released.
- **Auto Progress** begins the next authored wave automatically.
- **Despawn Active Objects On Completion** clears remaining tracked content when configured.

### Placement timing

- **Spawn Delay** offsets a placement from the wave start.
- **Sequential** causes formation members to be scheduled one after another.
- **Sequence Interval** controls the gap between sequential members.
- **Repetitions** repeats the placement.
- **Repeat Interval** controls the gap between repetitions.

## 13. Active enemy budget

`WaveLayoutData` stores an active enemy budget used by the authored layout system to identify/safeguard encounter configurations that exceed the intended active enemy count.

The budget is authoring/runtime planning data, not enemy AI. The package should not inspect enemy controller internals to enforce it.

## 14. Validation

`WaveLayoutValidator` is responsible for detecting invalid or unsafe authored layout data before runtime.

Validation should cover problems such as:

- missing catalog where required;
- missing spawnable/prefab references;
- placements outside the grid;
- invalid formation/footprint placement;
- duplicate/empty placement IDs;
- invalid timing values;
- active-budget problems;
- authored data that cannot be compiled into a valid runtime plan.

Validation should report the problem and allow the author to locate/fix it. Validation should not silently redesign the encounter.

## 15. Scene preview

`WaveLayoutScenePreview` provides spatial feedback for an authored layout in the Unity editor without requiring the author to enter Play Mode or instantiate the real runtime encounter as part of normal editing.

Preview behaviour is an authoring aid only. Runtime truth remains the compiled layout executed by `WaveRunner`.

## 16. Formation Designer

`SpawnFormationDesignerWindow` provides a focused authoring surface for reusable formation offsets.

A formation asset should contain reusable geometry only. Spawnable identity and placement-specific timing/rotation/flip data remain on the main layout placement.

This prevents formation assets from becoming one-off copies of whole encounters.

## 17. Save and Undo expectations

Editor changes should use Unity serialization/dirty-state conventions so authored ScriptableObjects save predictably. User-visible editing operations should participate in Unity Undo where supported by the current editor implementation.

## 18. Extension rules

When extending Waves:

1. Add content through `SpawnableDefinition` before adding new hardcoded content categories.
2. Add reusable shapes through `SpawnFormationDefinition` rather than duplicating placements across layouts.
3. Add spawn-position policies through `ISpawnPointProvider`.
4. Add object-creation policies through `IWaveSpawner`.
5. Keep enemy behaviour outside this package.
6. Keep room/game progression outside this package.
7. Compile new authoring concepts into the existing `WaveRunner` path where practical rather than adding a second runtime.
8. Add validator coverage for new authored constraints.
9. Add/update EditMode tests when runtime planning or editor data rules change.

## 19. Success criteria

The Wave Layout Builder is considered adoptable when another Unity developer can:

- open it from the Unity Tools menu;
- create a layout and catalog;
- create/edit spawnables;
- place objects/formations visually;
- configure wave and placement timing;
- validate the layout;
- assign the layout to the runtime;
- start the encounter from project flow;

without reading `WaveLayoutBuilderWindow.cs` or other package source code.
