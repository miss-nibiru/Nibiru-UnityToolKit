using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MissNibiru.Narrative.Editor
{
    internal sealed class NarrativeNodeView : Node
    {
        private readonly Action<NarrativeNodeView> _selected;
        private readonly List<Port> _outputs = new List<Port>();

        public NarrativeNode Model { get; }
        public Port Input { get; private set; }
        public IReadOnlyList<Port> Outputs => _outputs;

        public NarrativeNodeView(
            NarrativeNode model,
            Action<NarrativeNodeView> selected)
        {
            Model = model;
            _selected = selected;
            title = model.NodeTitle;
            viewDataKey = model.Id;
            capabilities &= ~Capabilities.Deletable;
            style.minWidth = 190f;
            style.borderTopWidth = 2f;
            style.borderTopColor = GetColour(model);

            if (!(model is NarrativeStartNode))
            {
                Input = Port.Create<Edge>(
                    Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Multi,
                    typeof(bool));
                Input.portName = string.Empty;
                inputContainer.Add(Input);
            }

            int outputCount =
                NarrativeNodeConnectionUtility.GetOutputCount(model);

            for (int i = 0; i < outputCount; i++)
            {
                Port output = Port.Create<Edge>(
                    Orientation.Horizontal,
                    Direction.Output,
                    Port.Capacity.Single,
                    typeof(bool));
                output.portName =
                    NarrativeNodeConnectionUtility.GetOutputLabel(model, i);
                output.userData = i;
                outputContainer.Add(output);
                _outputs.Add(output);
            }

            Label preview = new Label(GetPreview(model));
            preview.style.whiteSpace = WhiteSpace.Normal;
            preview.style.color = new Color(0.82f, 0.76f, 0.90f);
            preview.style.marginTop = 5f;
            preview.style.maxWidth = 220f;
            extensionContainer.Add(preview);

            SetPosition(new Rect(
                model.EditorPosition,
                new Vector2(220f, 130f)));
            RefreshExpandedState();
            RefreshPorts();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            _selected?.Invoke(this);
        }

        private static Color GetColour(NarrativeNode node)
        {
            if (node is NarrativeStartNode)
                return new Color(0.34f, 0.90f, 0.55f);
            if (node is NarrativeEndNode)
                return new Color(0.95f, 0.35f, 0.55f);
            if (node is NarrativeChoiceNode)
                return new Color(0.78f, 0.40f, 0.98f);
            if (node is NarrativeConditionNode)
                return new Color(0.95f, 0.70f, 0.24f);
            if (node is NarrativeEventNode)
                return new Color(0.30f, 0.72f, 0.96f);

            return new Color(0.60f, 0.36f, 0.88f);
        }

        private static string GetPreview(NarrativeNode node)
        {
            if (node is NarrativeLineNode line)
                return Shorten(line.Text, 90);
            if (node is NarrativeChoiceNode choice)
                return $"{choice.Choices.Count}/5 choices";
            if (node is NarrativeWaitNode wait)
                return $"{wait.Duration:0.##} seconds";
            if (node is NarrativeEventNode eventNode)
            {
                return eventNode.GameplayEvent == null
                    ? "No event assigned"
                    : eventNode.GameplayEvent.DisplayName;
            }
            if (node is NarrativeEndNode end)
                return end.EndingId;

            return node.Id;
        }

        private static string Shorten(string value, int maximum)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Empty dialogue";

            string clean = value.Replace('\n', ' ').Trim();
            return clean.Length <= maximum
                ? clean
                : clean.Substring(0, maximum - 1) + "…";
        }
    }
}
