# Miss Nibiru Narrative — Package Specification

Package: `com.missnibiru.narrative`

Editor tool: **Tools > Miss Nibiru > Visual Novel Builder**

## 1. Problem

Dialogue systems often become tied to one project's UI, quest logic, scene flow, inventory, or NPC scripts. That makes authored stories difficult to reuse and usually forces developers to rebuild the same branching, variable, save, and presentation logic for every game.

Miss Nibiru Narrative separates reusable narrative state/execution from project-specific gameplay consequences.

## 2. Goals

The package should:

- support direct dialogue and visual-novel style scenes;
- support branching choices and connected node flows;
- support conditions, variables, flags, and reusable narrative state;
- support reusable characters and emotions;
- support narrative events that project gameplay can react to;
- support save/resume of narrative state;
- provide a default Unity UI presentation path without requiring every game to use it;
- provide visual node-based story authoring;
- provide preview and validation without reading runtime source;
- support importing supported SugarCube `.twee` stories into Unity narrative data;
- keep runtime story execution independent from quests, inventory, combat, NPC AI, and scene progression.

## 3. Main runtime responsibilities

### Story data

`NarrativeStory` and the node/definition assets represent authored content and connections.

Reusable definitions include concepts such as:

- characters;
- emotions;
- variables;
- flags;
- narrative events;
- audio profiles;
- dialogue presentation profiles.

The authored model is ScriptableObject-first so story content can be edited as data instead of being embedded in one MonoBehaviour controller.

### Execution

`NarrativeRunner` owns runtime story execution and current narrative state.

It is responsible for moving through supported nodes, evaluating branching/state, exposing runtime events, and coordinating save/resume data.

### Blackboard/state

`NarrativeBlackboard` owns runtime variable/flag values used by branching and gameplay-facing narrative state.

Variables are generic. They may represent relationship values, reputation, alchemy values, counters, route state, or other values required by the story.

### Presentation

`NarrativePresenter` is the default uGUI-based presentation path.

Presentation is replaceable. A consuming project may use the runner/story data with its own UI instead of editing story execution to match a new art direction.

### Gameplay event bridge

`NarrativeEvent` assets and `NarrativeEventListener` allow narrative content to signal the consuming game without creating direct package dependencies on project systems.

Example consumers include quest, inventory, door, combat, audio, or NPC systems.

## 4. Editor responsibilities

The Visual Novel Builder provides the authoring workflow.

It should allow a user to:

- create/select a story;
- add and connect supported nodes;
- edit reusable library data;
- arrange story flow visually;
- configure presentation data;
- preview routes;
- validate story data;
- search/focus authored nodes;
- import supported `.twee` content;
- understand common setup problems without reading package source.

Editor code remains in the package's Editor assembly and must not become a runtime dependency.

## 5. Twee import boundary

The importer converts supported Twine/SugarCube narrative structure into Unity data where practical.

Expected importable concepts include supported passages, links/connections, state/logic mappings, and editor positions when available.

The importer must not pretend browser presentation is Unity presentation.

The following generally require Unity-side setup/review:

- HTML/CSS layout;
- browser-specific JavaScript behaviour;
- browser asset/audio paths;
- unsupported macros or project-specific SugarCube extensions.

The importer should produce/report unsupported or partially converted content rather than silently changing the meaning of the story.

## 6. Save boundary

The package owns narrative save-state data and serialization helpers.

The consuming game owns:

- save slots;
- full project save files;
- cloud/platform persistence;
- when saving/loading is allowed;
- non-narrative game state.

The package may expose convenience persistence such as PlayerPrefs for prototypes, but that is not intended to replace the consuming game's save architecture.

## 7. Extension boundary

Preferred extension patterns are:

- create new story/definition assets for new content;
- react to `NarrativeEvent` or runner events for project-specific consequences;
- use the blackboard/runtime API for gameplay-facing narrative variables;
- replace/customize presentation without rewriting story execution;
- add supported authoring/runtime behaviour inside the Narrative package only when it represents a generally reusable narrative problem.

Avoid adding direct references from Narrative to one game's quest managers, inventory managers, scene managers, enemies, or room logic.

## 8. Non-goals

Narrative does not own:

- quest implementation;
- inventory implementation;
- NPC AI;
- combat systems;
- room/door state;
- scene progression;
- the project's complete save system;
- general game UI outside narrative presentation;
- browser rendering compatibility for imported Twine HTML/CSS.

## 9. Adoption criteria

A developer should be able to:

1. open the Visual Novel Builder;
2. create a story without reading package source;
3. connect dialogue/logic visually;
4. run it through `NarrativeRunner`;
5. display it with the default presenter or a project-owned presenter;
6. react to narrative events from project gameplay;
7. save/resume narrative state;
8. import a supported Twee story and understand what still requires manual review.

The package is considered reusable when those tasks do not require references to the original game the system came from.
