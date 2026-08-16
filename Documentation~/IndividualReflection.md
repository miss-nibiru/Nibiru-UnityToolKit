# Individual Reflection

This assignment changed a lot from what I thought it was going to be at the beginning.

At first I was mostly thinking about taking systems I had already made and moving them somewhere reusable. Pretty quickly I realized that copying scripts into a toolkit is not the same thing as actually making them reusable. A lot of the work became figuring out what part of a system was genuinely generic, what part only made sense because of the original game, and where I needed an interface, ScriptableObject, event, or other extension point instead of another hardcoded reference.

## Refactoring and reusability

The biggest thing I learned from the refactor is that reuse is mostly about boundaries.

For example, the enemy system became much more useful once movement, attacks, and targeting stopped being one giant enemy controller. `IEnemyMovementBehaviour`, `IEnemyAttackBehaviour`, and `IEnemyTargetProvider` mean I can change one part of an enemy without rebuilding the whole enemy framework.

The projectile system went through a similar change. Instead of having different scripts for one-shot, burst, rapid fire, fan attacks, etc., the shared behaviour moved into `ProjectileAttackConfiguration` and `ProjectileAttackExecutor`. `IProjectileEmitter` then became the boundary between attack logic and how a projectile is actually created.

The Information package was another important refactor because it originally came from evidence/clue work. Once I looked at the actual problem, it was not really an "evidence system." It was a system for authored information that the player can collect. Making it generic means it can now be used for evidence, lore, codex entries, tutorials, discoveries, or other knowledge without rewriting the collection logic.

Narrative followed the same idea. I did not want the dialogue/visual-novel system to depend directly on one game's quests, inventory, NPCs, or scene flow. The package owns story state, branching, variables, flags, presentation, and narrative events. The consuming game decides what a narrative event actually does.

The main architecture lesson for me was that the reusable part should know as little as possible about the original game.

## What changed from my original approach

I definitely changed the specification as I worked.

Originally some systems were much more connected to the project they came from. During the refactor I kept finding places where the real reusable problem was smaller than the original implementation.

Some of the biggest changes were:

- making the state machine generic instead of tied to one battle flow;
- separating projectile configuration from execution;
- separating enemy movement, attacks, and targeting;
- turning evidence into the broader Information system;
- separating information collection rules from persistence through `IInformationCollectionStore`;
- expanding Waves from simple spawning into a reusable runtime plus visual layout/formation authoring;
- building Narrative as a reusable dialogue/visual-novel runtime instead of a game-specific dialogue controller;
- adding editor tools so other people can actually use the systems without opening the source code.

I think this is a much stronger result than trying to force the original specification to stay unchanged. The specification became more accurate as I understood the real architecture better.

## Development tools

The Toolkit Debugger came directly from something I kept doing manually.

I was repeatedly checking package setup, assembly definitions, missing scripts, broken references, duplicate IDs, configuration problems, and Unity errors. Doing that by hand every time was annoying and also depended too much on knowing where everything lived.

The debugger turns that repeated process into Quick, Selection, and Full Project scans. It is intentionally read-only. I wanted it to tell me what is wrong, where it is, and what I should check without secretly changing files behind me.

The Wave Layout Builder and Visual Novel Builder solve a similar problem from the authoring side. A reusable runtime is not that useful if somebody still has to read the implementation to create content. The editor tools make waves/formations and narrative graphs usable through Unity instead of through source-code edits.

That was probably the biggest shift in how I thought about the "development tool" requirement. The tool is not just extra UI. It should remove a workflow I would otherwise have to repeat manually.

## Optimization workflow

One thing I wanted to avoid was pretending that code inspection automatically tells me what the performance bottleneck is.

The projectile path is an obvious candidate because the default `PrefabProjectileEmitter` creates projectile objects and projectile actors update during their lifetime, but that is still only a hypothesis until it is measured.

To make the optimization work repeatable, I added a controlled Development Build stress scenario. It uses the real Combat package and repeatedly emits projectiles with fixed settings for a fixed amount of time. The same scenario can be built with the Profiler connected so the baseline and final capture use the same workload.

That is a better workflow for me than changing something because it "looks expensive" and then trying to justify it afterward. The profiler capture is supposed to decide what gets optimized, and the exact same scenario is used again after the change so the before/after comparison is actually meaningful.

The successful Development Build also gave me a reproducible starting point instead of profiling a random moment in a normal gameplay scene.

## Testing and validation

The package-level tests became more important as the systems became more abstract.

Refactoring reusable code can break behaviour in ways that are not immediately obvious from one demo scene. The EditMode tests give me a faster way to check the contracts themselves, while `ToolkitDEMO_Scene` is useful for checking that multiple systems still behave together inside a real Unity project.

I also learned that editor tooling needs its own validation. A runtime can be perfectly fine while an Editor assembly fails to compile because of a Unity API signature or version difference. The Narrative GraphView timer callback issue was a good example of that: the problem was not the dialogue architecture, it was an editor API callback signature that had to match the Unity version being used.

## AI usage

I used AI a lot during this assignment, but not as a replacement for running the project.

The most useful parts were architecture discussion, identifying coupling, suggesting extension points, comparing duplicated systems, generating/refining implementation ideas, helping diagnose compiler errors, and helping write documentation after the implementation was already understood.

It was especially useful when I needed to step back from a script and ask "what is the reusable problem here?" instead of only fixing the immediate game-specific issue.

At the same time, I had to verify suggestions constantly in Unity. There were cases where an AI suggestion did not match the actual project, referenced the wrong scene, assumed an API worked differently, or overcomplicated something that was much simpler in the real project. I rejected or changed those suggestions instead of treating generated code as automatically correct.

My validation process was still: compile it, run the tests where available, open the actual Unity tool/scene, use the system, and fix what happens in the real project.

I also used AI for documentation, but the documentation is based on the final repository architecture rather than being treated as the source of truth by itself.

## What I would do differently next time

I would define package boundaries earlier.

A lot of the refactor became easier once I stopped thinking in terms of "move this script into the toolkit" and started asking:

- what domain owns this behaviour?
- what data should be configurable?
- what should another game be able to replace?
- what should stay completely outside the package?

I would also create the adoption documentation at the same time as a package/tool instead of coming back later. Narrative was a good example: the package itself grew after the first architecture documentation pass, so the root README and final architecture had to be updated afterward to match reality.

For optimization, I would keep the same rule I am using here: make one reproducible scenario, measure first, change one architectural cause, then run the same measurement again. That gives me evidence instead of a list of "optimizations" that may not have solved a real problem.

## Final takeaway

The strongest part of this assignment for me is that the result is not just one finished game system anymore.

The toolkit now has reusable runtime packages, clear extension points, tests, visual authoring tools, diagnostics, documentation, and a controlled build/performance test path. More importantly, I have a much clearer idea of the difference between code that happens to work in multiple places and code that was intentionally designed to be reused.
