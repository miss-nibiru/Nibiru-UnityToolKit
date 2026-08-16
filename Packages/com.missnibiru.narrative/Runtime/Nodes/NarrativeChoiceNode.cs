using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeChoiceNode : NarrativeNode
    {
        public const int MaximumChoices = 5;

        [SerializeField, TextArea(1, 4)]
        private string prompt = "Choose a response.";

        [SerializeField]
        private NarrativeChoiceOption[] choices =
            Array.Empty<NarrativeChoiceOption>();

        public override string NodeTitle => "Player Choice";
        public string Prompt => prompt ?? string.Empty;
        public IReadOnlyList<NarrativeChoiceOption> Choices =>
            choices ?? Array.Empty<NarrativeChoiceOption>();

        public NarrativeChoiceOption GetChoice(int index)
        {
            return choices != null && index >= 0 && index < choices.Length
                ? choices[index]
                : null;
        }

        public void Configure(
            string visiblePrompt,
            NarrativeChoiceOption[] options)
        {
            prompt = visiblePrompt ?? string.Empty;
            choices = options ?? Array.Empty<NarrativeChoiceOption>();

            if (choices.Length > MaximumChoices)
                Array.Resize(ref choices, MaximumChoices);
        }

        public void SetChoiceTarget(int index, string targetNodeId)
        {
            NarrativeChoiceOption choice = GetChoice(index);

            if (choice != null)
                choice.SetTargetNodeId(targetNodeId);
        }

        public override IEnumerable<string> GetOutgoingNodeIds()
        {
            if (choices == null)
                yield break;

            foreach (NarrativeChoiceOption choice in choices)
            {
                if (choice != null &&
                    !string.IsNullOrWhiteSpace(choice.TargetNodeId))
                {
                    yield return choice.TargetNodeId;
                }
            }
        }

        private void OnValidate()
        {
            choices ??= Array.Empty<NarrativeChoiceOption>();

            if (choices.Length > MaximumChoices)
                Array.Resize(ref choices, MaximumChoices);
        }
    }
}
