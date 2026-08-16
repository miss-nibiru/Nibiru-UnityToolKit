# Narrative Setup

This is the practical setup guide for `com.missnibiru.narrative`.

## Open the Visual Novel Builder

In Unity, open:

**Tools > Miss Nibiru > Visual Novel Builder**

From there you can create a new story, build the node flow, create reusable library assets, set presentation, preview routes, validate the story, and import supported SugarCube `.twee` files.

## Create a story

1. Open the Visual Novel Builder.
2. Choose **New Story**.
3. Save the `NarrativeStory` asset somewhere under `Assets`.
4. Add dialogue and logic nodes in the Flow view.
5. Connect node ports to create the route.
6. Create reusable characters, emotions, variables, flags, and events from the Library.
7. Configure the story presentation if you want to use the default presenter.
8. Use Preview and Validation before wiring the story into gameplay.

## Runtime setup

The simplest runtime setup is one GameObject containing:

- `NarrativeRunner`
- `NarrativePresenter`

Assign the `NarrativeStory` to the runner.

Start the story from your project code with the runner's public start API, such as `StartStory()` or `StartSequence()` depending on the integration you are using.

The default `NarrativePresenter` can create/use a uGUI presentation based on the story's presentation profile. A game may also replace the presentation layer and keep using the same runner/story data.

## React to gameplay events

Use `NarrativeEvent` assets for things that need to leave the narrative system and affect the game.

Examples:

- start a quest;
- unlock a room;
- give an item;
- change an NPC state;
- trigger a battle;
- update another gameplay system.

`NarrativeEventListener` provides an Inspector-friendly bridge to UnityEvents. Project code can also subscribe to the runner's public events.

The Narrative package should not directly own those gameplay systems. It emits/represents the narrative event; the consuming game decides what happens next.

## Variables and flags

Use narrative variables/flags for authored state that affects branching.

Examples include:

- relationship points;
- reputation;
- route state;
- counters;
- alchemy values;
- choices that need to be remembered.

Values are accessed through the runner's blackboard/runtime API. `VariableChanged` can be observed when another system or HUD needs immediate updates.

## Save and resume

For a project-owned save system, use the runner's JSON save/resume APIs and store the returned narrative state inside the game's normal save file.

For simple prototypes, the package also supports PlayerPrefs-based save/resume helpers.

The package owns narrative state serialization. The game still owns the full save-file format and save slots.

## Import a Twee story

1. Open the Visual Novel Builder.
2. Choose **Import Twee**.
3. Select a SugarCube `.twee` file.
4. Choose where to create the imported story/assets.
5. Read the generated import report.
6. Open the generated graph and review any content that needs manual Unity-side setup.

The importer can preserve/convert supported passage flow and state logic, but browser-specific presentation is not a Unity UI system.

Rebuild HTML/CSS presentation using Unity UI/presentation profiles and reconnect browser audio paths to Unity `AudioClip` assets or gameplay events.

## What belongs outside Narrative?

Keep these in the consuming game unless they later become their own reusable package:

- quest implementation;
- inventory implementation;
- NPC AI;
- combat orchestration;
- room/door logic;
- scene progression;
- the project's full save-file system;
- project-specific UI screens outside narrative presentation.

The main rule is: Narrative owns the conversation, branching state, and narrative-facing events. The game owns what those events actually do.
