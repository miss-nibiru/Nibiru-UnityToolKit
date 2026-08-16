# Changelog

## 0.2.1 - 2026-08-16

- Fixed Editor and test assembly references by pinning them to the Narrative
  Runtime and Editor assembly GUIDs.
- Added explicit Narrative runtime imports to editor and test sources.
- Changed the manual delivery to a complete replacement package so assembly
  definitions cannot be omitted during an update.

## 0.2.0 - 2026-08-16

- Added safe SugarCube Twee import from the Visual Novel Builder.
- Converts passages, graph positions, links, variables, flags, conditions and
  value mutations into ScriptableObject narrative data.
- Added conditional imported text, paged conversion for passages exceeding
  five choices and a generated import report.
- Added generic runtime getters, setters and change events for gameplay-facing
  narrative variables such as alchemy resources.
- Added a reusable Random Value node for Twee `random(min, max)` assignments.
- Added Twee parser, importer and variable API EditMode tests.

## 0.1.3 - 2026-08-16

- Fixed text fields losing focus by preventing per-keystroke graph reloads.
- Choice graph ports now refresh only after add, remove or reorder actions.

## 0.1.2 - 2026-08-16

- Added a Start Here tab with the complete authoring and runtime setup order.

## 0.1.1 - 2026-08-16

- Fixed Unity 6.3 GraphView manipulator extension calls.

## 0.1.0 - 2026-08-16

- Added ScriptableObject narrative definitions and node graph data.
- Added node-based Visual Novel Builder with Miss Nibiru branding.
- Added draggable presentation layout designer and editor preview.
- Added runtime branching, choices, state, events, audio and save/resume.
- Added validation, FAQ, documentation and automated EditMode tests.
