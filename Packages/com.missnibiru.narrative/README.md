# Miss Nibiru Narrative

`com.missnibiru.narrative` is a ScriptableObject-first dialogue runtime and
visual authoring package for Unity 6. It supports direct conversations,
visual-novel scenes, branching choices, conditions, saved state and gameplay
event hooks without making dialogue depend on a specific game.

## Open the tool

Open **Tools > Miss Nibiru > Visual Novel Builder**.

1. Select **New Story** and save the asset under `Assets`.
2. Add dialogue and logic nodes in **Flow**.
3. Drag ports to connect the story.
4. Create reusable characters, emotions, variables, flags and events in
   **Library**.
5. Arrange the player-facing UI in **Presentation**.
6. Test the route in **Preview**, then run **Validation**.

## Import a Twine story

Select **Import Twee** and choose a SugarCube `.twee` file. A review window
shows exactly how many passages, narrator lines, character lines, choices,
mutations, colours and audio keys were detected before anything is created.
Select an existing `TweeImportProfile`, or create one from the review.

An import profile can map several source colours to one character (for example,
two Serena colours), assign an emotion and portrait side, and connect detected
audio keys to Unity clips. Missing mapped characters can be created as safe
placeholders automatically. Uncoloured text becomes narrator lines; coloured
text becomes character lines. The importer preserves ordered dialogue,
conditions and mutations as separate nodes, connects choices to their target
passages, and writes a detailed report beside the generated data.

Use **Find**, **Previous**, **Next**, **Frame All**, and the minimap in the Flow
tab to navigate a large imported graph. Import always creates a new story and
never overwrites an existing asset.

Twine HTML layout and browser audio paths are not Unity assets. Rebuild HUD
markup with Unity UI and map each detected audio key to an `AudioClip` in the
import profile.

## Runtime setup

1. Add `NarrativeRunner` and `NarrativePresenter` to one GameObject.
2. Assign the `NarrativeStory` to the runner.
3. Call `StartStory()` or `StartSequence()` from your room, NPC or interaction
   code.
4. Add `NarrativeEventListener` when designers need to connect narrative event
   assets to UnityEvents in the Inspector.

The presenter creates a runtime uGUI canvas from the story's presentation
profile. You may assign an existing canvas if the game already owns one.

## Save and resume

Use `CreateSaveJson()` and `ResumeFromJson()` when the game owns its save file.
For simple prototypes, use `SaveToPlayerPrefs(slot)` and
`ResumeFromPlayerPrefs(slot)`.

## Gameplay variables

Narrative variables are generic and can power alchemy, reputation, relationship
points or other game systems. Read and change them through the runner's
blackboard, for example:

```csharp
int current = runner.Blackboard.GetInteger(alchemyCurrent);
runner.Blackboard.AddInteger(alchemyCurrent, -1);
```

Subscribe to `VariableChanged` when a HUD must update immediately.

## Extension boundary

The package owns dialogue data, branching state, presentation and narrative
events. Quest logic, inventory, NPC AI, room locks and the game's main save-file
format remain outside this package. Gameplay systems react through
`NarrativeEvent` assets or the runner's C# events.

See [Documentation~/SETUP.md](Documentation~/SETUP.md) and
[Documentation~/SPECIFICATION.md](Documentation~/SPECIFICATION.md) for the
complete setup and architecture.
