# Nibiru Waves

Nibiru Waves provides reusable runtime wave spawning plus the Wave Layout
Builder, an editor tool for designing exact grid-based encounters without
spawning real objects while editing.

## Open the tool

Open **Window > Miss Nibiru > Wave Layout Builder**.

## Quick setup

1. Create a `WaveLayoutData` from the tool's **New** button.
2. Create a `SpawnCatalog` from **New Catalog**.
3. Add `SpawnableDefinition` assets for enemies, hazards, pickups or other
   prefabs.
4. Set each spawnable's prefab and grid footprint.
5. Drag spawnables onto the active wave grid.
6. Add a `WaveRunner` and `WaveSpawner` to the scene.
7. Select the runner and an Origin transform in the tool.
8. Press **Validate**, then save.
9. Call `WaveRunner.StartSequence()` from the room trigger or game flow.

The Origin's position and rotation map the editor grid into world space.
`XY` supports side-view games and `XZ` supports top-down or 3D games.

## Runtime compatibility

`WaveRunner` supports both authoring modes:

- Existing `WaveData[]` sequences continue using an `ISpawnPointProvider`.
- An assigned `WaveLayoutData` compiles into exact timed spawn instructions.

When an authored layout is assigned, `StartSequence()` uses it automatically.
No second wave runner or competing runtime system is introduced.

## Formations

Formations are ScriptableObjects containing reusable cell offsets. Create one
from the Formations palette, edit its mini-grid, then drag it onto the main grid
after selecting a spawnable. Rotation and flipping are stored per placement.

## Timing

- Initial Delay waits before a wave begins.
- Spawn Delay offsets one placement or group.
- Sequential staggers formation members.
- Repetitions and Repeat Gap repeat a placement.
- Timed Wave stops scheduling at its duration.
- Wait For Clear waits for tracked objects to release.
- Auto Progress begins the next authored wave.

`WaveSpawnedObject.Release()` tells the runner that an active object is gone.
Disabling or destroying the object also releases it.

## Scope

This package owns wave data, placement, validation, calculation and spawning.
Doors, cameras, room locking, enemy AI and movement remain external systems.

See `Documentation~/WaveLayoutBuilderSpecification.md` for the complete
architecture and extension points.
