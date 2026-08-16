# Development Build — Controlled Stress Scenario

## Purpose

This document records the reproducible Development Build configuration requested for the assignment. It does **not** use the Unity Profiler.

The scenario exists to provide a controlled runtime load in a Development Build so the same build can be reproduced on a machine with Unity installed and licensed.

## Build scene

`Assets/_Project/02_Scenes/ToolkitDEMO_Scene.unity`

The assignment build contains only this demo scene so scene content is controlled and repeatable.

## Build mode

Unity build option:

`BuildOptions.Development`

The menu command also uses `BuildOptions.AutoRunPlayer` so the built player launches immediately after a successful build.

No `ConnectWithProfiler` option is used.

## Stress define

The build supplies the additional scripting define:

`NIBIRU_ASSIGNMENT_STRESS`

That define includes `AssignmentStressScenario.cs` in this build only. Normal editor play and normal builds do not run the assignment stress bootstrap.

## Controlled stress workload

The stress scenario is intentionally simple and deterministic:

- duration: **30 seconds**;
- projectile emissions: **4 per rendered frame**;
- projectile lifetime: **2 seconds**;
- projectile speed: **8 units/second**;
- damage: **0** (the workload measures projectile lifecycle/spawn/update behaviour without requiring targets);
- projectile directions: deterministic rotating angle sequence;
- runtime path exercised: `PrefabProjectileEmitter` → `ProjectileActor` creation → per-projectile `Update()` → lifetime completion → `Destroy()` through the emitter completion path.

The scenario therefore creates sustained projectile allocation, movement, completion, and destruction using the real reusable Combat package rather than a fake delay loop.

## Why this scenario is controlled

The following values are fixed in source:

- build scene;
- duration;
- emissions per frame;
- projectile lifetime;
- projectile speed;
- direction sequence.

The scenario does not depend on player input, enemy AI, random seeds, dialogue progression, or manual wave timing. This makes repeated runs meaningfully comparable even though this assignment record is not a profiler report.

## Runtime log output

At startup the player writes a line beginning with:

`NIBIRU_STRESS_START`

At completion it writes a line beginning with:

`NIBIRU_STRESS_RESULT`

The result includes:

- measured scenario duration;
- rendered frame count;
- number of successfully emitted projectiles;
- average frame time in milliseconds;
- maximum observed frame time in milliseconds.

The scenario then calls `Application.Quit(0)`.

These values are lightweight runtime evidence only. They are not a substitute for the Unity Profiler and should not be described as profiler measurements.

## Development Build command

From Unity:

**Tools > Miss Nibiru > Assignment > Build + Run Stress Development Build**

Implementation:

`Assets/_Project/00_NibiruToolKit/Editor/AssignmentDevelopmentBuild.cs`

The command:

1. verifies the demo scene exists;
2. verifies the active target is a supported desktop standalone target;
3. creates `Builds/AssignmentStress`;
4. builds `ToolkitDEMO_Scene` as a Development Build;
5. injects `NIBIRU_ASSIGNMENT_STRESS` for the build;
6. logs the Unity `BuildReport` result;
7. automatically launches the player when invoked from the menu;
8. allows the stress scenario to run for 30 seconds and exit.

## Supported standalone targets

- macOS: `Builds/AssignmentStress/NibiruToolkitStress.app`
- Windows 64-bit: `Builds/AssignmentStress/NibiruToolkitStress.exe`
- Linux 64-bit: `Builds/AssignmentStress/NibiruToolkitStress`

The active Unity Build Target determines which output is produced.

## Command-line build entry point

For a machine with Unity installed and licensed, the same Development Build can be created without the menu by calling:

```text
Unity -batchmode -quit -projectPath . -executeMethod AssignmentDevelopmentBuild.BuildStressScenarioFromCommandLine
```

The command-line method builds but does not auto-run the standalone player. The resulting application can then be launched normally to execute the embedded stress scenario.

## Build report evidence

The editor build command writes a line beginning with:

`NIBIRU_DEVELOPMENT_BUILD_RESULT`

It contains:

- Unity `BuildResult`;
- build duration;
- output size in bytes;
- error count;
- warning count;
- output path.

A successful assignment run should retain/capture both:

1. `NIBIRU_DEVELOPMENT_BUILD_RESULT result=Succeeded ...`
2. `NIBIRU_STRESS_RESULT ...`

## Current execution status

**Prepared, but not executed in the assistant execution environment.**

The environment used to make these repository changes does not contain a Unity Editor installation, and the repository does not currently contain an existing GitHub Actions Unity build workflow that can be used instead. Unity Development Builds require the Unity Editor and a valid Unity licensing/build environment.

For that reason, no successful build result, runtime frame count, or output-size number is fabricated in this document.

The repository now contains the deterministic build command and stress workload required to run the Development Build unchanged on the project's normal Unity machine.
