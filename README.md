# Nibiru Unity Toolkit

Hi! This is basically my little collection of Unity systems that I kept making over and over again, so I finally pulled them out of my projects and turned them into reusable packages.

The idea is pretty simple: if I make another game and need health, projectiles, enemy movement, waves, information tracking, etc. I should not have to rebuild the whole thing from zero.

Most of the reusable stuff lives in `Packages/com.missnibiru.*`.

There is also a demo scene here:

`Assets/_Project/02_Scenes/ToolkitDEMO_Scene.unity`

That is where I test a bunch of the systems together without making the packages depend on one specific game.

## What is actually in here?

### Core
`com.missnibiru.core`

This is the basic stuff that other systems can build on.

**Health**

There is a reusable `HealthComponent`, damage calculation, `IDamageable`, and `IHealthSource`.

If I just need normal health, I use `HealthComponent`.

If a game needs something weird or custom, I can implement `IDamageable` or `IHealthSource` instead of rewriting the rest of the toolkit.

**Patterns**

This is for things like input patterns or sequences without hardcoding every combination into a script.

It uses `PatternToken`, `PatternDefinition`, `PatternDatabase`, and `PatternResolver`.

To add more, I mostly just make new ScriptableObject tokens/patterns. The resolver does not need to care what game they came from.

**State Machine**

The state machine is generic: `StateMachine<TContext>` + `IState<TContext>`.

So if I need a battle state machine, menu state machine, AI state machine, whatever, I make my own context + states and reuse the same machine.

---

### Combat
`com.missnibiru.combat`

This is the projectile / attack side of the toolkit.

Instead of making a different firing script for every gun or enemy attack, I use `ProjectileAttackConfiguration` + `ProjectileAttackExecutor`.

Things like spread, burst behaviour and attack values live in configuration instead of being copied into a bunch of scripts.

For projectile spawning there is `IProjectileEmitter` and the default `PrefabProjectileEmitter`.

That interface is the important extension point: if later I want object pooling, networking, or some completely different spawning system, I can make another emitter without rewriting the attack executor.

Damage still goes through the Core damage contracts, so Combat does not need its own second health system.

---

### Enemies
`com.missnibiru.enemies`

This package is basically my attempt to stop making giant enemy scripts that do EVERYTHING.

`EnemyActor` coordinates smaller behaviours instead.

For movement there is `IEnemyMovementBehaviour`.

Already included:

- Stationary
- Chase
- Patrol
- Formation

If I suddenly need an enemy that orbits the player, flees, follows a spline, uses NavMesh, etc. I just make another movement behaviour.

Attacks work the same way through `IEnemyAttackBehaviour`.

Target selection goes through `IEnemyTargetProvider`, so targeting can also be swapped without rewriting the actual enemy.

Basically: new enemy idea = combine/replace behaviours instead of making another giant controller.

---

### Information
`com.missnibiru.information`

This started from evidence / clue systems, but I did not want it trapped inside one detective game forever.

Now it is generic information that the player can discover and collect.

So it can be used for:

- clues
- evidence
- lore
- codex entries
- tutorials
- discoveries
- basically anything the player can "learn"

The authored side uses things like `InformationEntry`, `InformationPage`, `InformationCategory`, `InformationType`, and `InformationDatabase`.

Collection state uses `InformationCollection`.

The big extension point here is `IInformationCollectionStore`.

The toolkit includes an in-memory store, but another game can connect it to its own save system, PlayerPrefs, cloud save, etc. without changing how collecting information works.

There is also an Information Organizer editor tool for actually working with the assets without digging through folders forever.

---

### Waves
`com.missnibiru.waves`

This handles wave spawning and encounter layouts.

You can do normal runtime waves with `WaveData` / `WaveRunner`, or author encounters visually using `WaveLayoutData` and the Wave Layout Builder.

Open it here:

**Tools > Miss Nibiru > Wave Layout Builder**

The layout side lets me place spawnables on a grid, make formations, set timing, repeat placements, and preview encounters before running the game.

Useful extension points:

- `ISpawnPointProvider` if a game needs a different way to choose spawn positions
- `IWaveSpawner` if spawning itself needs to work differently
- `SpawnableDefinition` for new things that can spawn
- `SpawnFormationDefinition` for reusable formations

The Waves package only worries about waves/spawning. It does NOT own enemy AI, doors, cameras, room progression, etc. That stuff belongs to the actual game.

More details are here:

[`Packages/com.missnibiru.waves/Documentation~/WaveLayoutBuilderSpecification.md`](Packages/com.missnibiru.waves/Documentation~/WaveLayoutBuilderSpecification.md)

---

### UI
`com.missnibiru.ui`

This is the smallest package right now.

At the moment it mainly has reusable health UI through `HealthBarUI`.

The point is that toolkit UI should listen to reusable interfaces/data instead of knowing about one specific player or enemy script.

So if I add more shared UI later, I keep the game-specific menus/screens in the game and only put actually reusable pieces here.

---

### Toolkit Debugger
`com.missnibiru.debugger`

This one exists because I got tired of manually hunting for the same Unity problems over and over again.

Open it here:

**Tools > Miss Nibiru > Toolkit Debugger**

It can do Quick, Selection, and Full Project scans for things like:

- package problems
- assembly definition problems
- missing scripts / broken references
- duplicate or empty IDs
- bad toolkit configuration
- current Unity warnings/errors

It is read-only. It tells me what looks wrong and where it is, but it does not secretly start changing my project.

If I want it to check something new later, I add another scanner rule that creates a `ToolkitDebugIssue` with a useful message + location + suggested fix.

Debugger spec:

[`Packages/com.missnibiru.debugger/Documentation~/ToolkitDebuggerSpecification.md`](Packages/com.missnibiru.debugger/Documentation~/ToolkitDebuggerSpecification.md)

---

## So... how do I extend this thing?

My general rule is:

**If it is data, make a new asset.**

New attack? New configuration.

New pattern? New pattern asset.

New spawnable? New definition.

**If the behaviour genuinely works differently, implement the interface.**

New movement style? `IEnemyMovementBehaviour`.

New projectile spawning method? `IProjectileEmitter`.

New save backend for collected information? `IInformationCollectionStore`.

New wave spawning backend? `IWaveSpawner`.

That is basically the whole architecture idea: extend the small part that changes instead of copying and rewriting an entire system.

## Demo stuff

The project also has examples under:

`Assets/_Project/00_NibiruToolKit`

There are examples for health, state machines, patterns, projectile attacks, information and waves.

The main integration scene is:

`Assets/_Project/02_Scenes/ToolkitDEMO_Scene.unity`

## If you want the VERY detailed version

The README is intentionally the chill version.

The full architecture, package relationships, testing rules and the "what changed from the original project and why" stuff lives here:

[`Documentation~/FinalArchitectureSpecification.md`](Documentation~/FinalArchitectureSpecification.md)

Development Build / stress scenario notes are here:

[`Documentation~/DevelopmentBuildStressScenario.md`](Documentation~/DevelopmentBuildStressScenario.md)

## Unity version

Built for Unity 6 (`6000.0+`).

## Tests

The main packages have EditMode tests inside their package folders.

For a quick real-project sanity check, I also use `ToolkitDEMO_Scene` to make sure the different systems still play nicely together.
