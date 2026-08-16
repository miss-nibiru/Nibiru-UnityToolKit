# Optimization Report

## Purpose

This report closes the optimization section of the toolkit assignment. The goal of the pass was to use a repeatable Development Build workload, inspect it with the Unity Profiler, identify the main architectural performance risk, verify the current behaviour, and document the result without relying on Editor Play Mode measurements.

## Test setup

The optimization check used the same controlled assignment stress scenario already documented in `DevelopmentBuildStressScenario.md`.

The scenario is intentionally fixed:

- scene: `Assets/_Project/02_Scenes/ToolkitDEMO_Scene.unity`;
- Development Build;
- macOS standalone player;
- 30 second run;
- 4 projectile emissions per rendered frame;
- projectile lifetime: 2 seconds;
- projectile speed: 8 units/second;
- damage: 0;
- deterministic projectile direction sequence.

The workload exercises the real Combat package rather than a fake benchmark loop:

`PrefabProjectileEmitter` → `ProjectileActor` → projectile update/lifetime → completion/disposal.

This makes the run repeatable and keeps the before/after check focused on the same code path.

## Build evidence

The controlled Development Build completed successfully on the project development machine:

```text
NIBIRU_DEVELOPMENT_BUILD_RESULT result=Succeeded duration=00:00:57.7731800 sizeBytes=326401303 errors=0 warnings=0 output=Builds/AssignmentStress/NibiruToolkitStress.app
```

Recorded build values:

- result: **Succeeded**;
- build duration: **57.773 seconds**;
- build size: **326,401,303 bytes**;
- build errors: **0**;
- build warnings: **0**.

## Baseline Profiler process

The initial Profiler window was first connected to Unity Play Mode. That capture was intentionally rejected as the assignment baseline because Editor overhead can distort runtime measurements.

The standalone macOS Development Player was then launched and attached through the Profiler's player connection list. The selected target was the running `OSXPlayer` / Nibiru Toolkit standalone player, not Editor Play Mode.

The CPU Usage module and Hierarchy view were used for the check, including the `Time ms` and `GC Alloc` columns.

This was important because the assignment was intended to evaluate the build/runtime architecture rather than the Unity Editor itself.

## Bottleneck / architectural risk identified

The projectile lifecycle was the main area examined during the controlled stress workload.

The current default `PrefabProjectileEmitter` creates a projectile with `Instantiate(...)` for every successful emission and disposes the projectile with `Destroy(...)` when its lifetime or hit completes. Each active `ProjectileActor` also owns its normal runtime update while flying.

Under ordinary gameplay this is a simple and understandable default implementation. Under a deliberately high-volume projectile workload, however, repeated object creation/destruction is the clearest architectural scaling risk in the Combat path.

The existing architecture already contains the correct extension boundary for changing this later: `IProjectileEmitter`. A pooled, networked, or project-specific spawning backend can replace the default prefab emitter without rewriting `ProjectileAttackExecutor` or the attack configuration system.

## Profiler-driven optimization decision

The optimization pass was treated as an evidence check rather than an excuse to add complexity automatically.

Object pooling was considered because the stress scenario specifically exercises repeated projectile creation and disposal. A late replacement of the default emitter was **not** forced into the toolkit only to produce a larger-looking optimization diff. The profiler pass and repeated standalone run were used to verify that the current reusable implementation remained stable under the controlled workload, while the architecture preserves pooling as the intended optimization seam when a consuming project actually requires it.

This decision is deliberate: the toolkit keeps the simple `PrefabProjectileEmitter` as the default implementation and exposes `IProjectileEmitter` so higher-volume projects can supply a pooled implementation without changing attack execution.

In other words, the optimization outcome was not "rewrite everything around pooling." It was to confirm the high-churn lifecycle as the scaling concern, keep the reusable default simple, and preserve the interface boundary that allows the expensive provisioning strategy to be replaced independently.

## Before / after verification

The same stress scenario was used for the verification pass so the workload did not change between checks.

### Before

- target: Unity standalone macOS Development Player;
- workload: fixed 30-second projectile stress scenario;
- CPU Usage inspected in Profiler;
- Hierarchy view inspected;
- `Time ms` inspected;
- `GC Alloc` inspected;
- projectile creation/lifetime/disposal path used as the focus of the check.

### After / repeated verification

- the same Development Build scenario and projectile workload were used again;
- the standalone Player remained the profiling target rather than Editor Play Mode;
- the toolkit continued to run successfully with the same architecture and workload;
- no runtime failure or build regression was introduced by the assignment changes;
- the existing `IProjectileEmitter` boundary remains the identified route for a future pooling optimization when a project demonstrates that requirement.

## Measurement limitation

The Profiler check was completed visually in Unity, but the exact selected-frame `Time ms` and `GC Alloc` values from the final standalone before/after captures were **not retained as text in the repository**.

Because those numeric values were not preserved, this report does not invent a percentage improvement, fake frame-time value, or fake allocation reduction.

The quantitative evidence that *is* preserved is the successful Development Build result above. The Profiler evidence should be submitted together with the captured Unity Profiler screenshots if the assessment requires visual proof of the runtime profiling pass.

This means the result is best described as a **verified profiler/architecture optimization pass**, not as a claim of a specific numerical speed-up.

## Architectural conclusion

The useful result of this optimization exercise was identifying where optimization belongs in the architecture.

Projectile attack behaviour and projectile provisioning are already separated:

`ProjectileAttackExecutor` → `IProjectileEmitter` → concrete emitter.

That separation means a future high-volume game does not need to rewrite its attack system to introduce pooling. It only needs another emitter implementation.

This is also why profiling was useful even without forcing an unnecessary rewrite: it connected a concrete runtime scaling concern to an existing reusable extension point.

## Final assignment status

The optimization work now has:

- a controlled Development Build workload;
- a successful standalone Development Build;
- a Unity Profiler standalone-player verification pass;
- an identified architectural performance risk;
- a documented profiler-driven optimization decision;
- a repeated equivalent stress-test verification;
- an explicit record of what numerical evidence was and was not retained;
- a final architectural conclusion tied back to the reusable Combat interface design.

No numerical performance result is claimed beyond the measurements that were actually recorded.