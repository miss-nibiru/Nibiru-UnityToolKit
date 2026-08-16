using System.Collections.Generic;

namespace MissNibiru.Narrative.Editor
{
    internal static class NarrativeNodeConnectionUtility
    {
        public static int GetOutputCount(NarrativeNode node)
        {
            if (node is NarrativeChoiceNode choice)
                return choice.Choices.Count;
            if (node is NarrativeConditionNode)
                return 2;
            if (node is NarrativeEndNode)
                return 0;
            return 1;
        }

        public static string GetOutputLabel(
            NarrativeNode node,
            int index)
        {
            if (node is NarrativeChoiceNode choice)
            {
                NarrativeChoiceOption option = choice.GetChoice(index);
                return option == null || string.IsNullOrWhiteSpace(option.Text)
                    ? $"Choice {index + 1}"
                    : option.Text;
            }

            if (node is NarrativeConditionNode)
                return index == 0 ? "True" : "False";

            return "Next";
        }

        public static string GetTarget(NarrativeNode node, int index)
        {
            if (node is NarrativeStartNode start)
                return start.NextNodeId;
            if (node is NarrativeLineNode line)
                return line.NextNodeId;
            if (node is NarrativeChoiceNode choice)
                return choice.GetChoice(index)?.TargetNodeId ?? string.Empty;
            if (node is NarrativeConditionNode condition)
                return index == 0
                    ? condition.TrueNodeId
                    : condition.FalseNodeId;
            if (node is NarrativeSetValueNode setValue)
                return setValue.NextNodeId;
            if (node is NarrativeEventNode eventNode)
                return eventNode.NextNodeId;
            if (node is NarrativeWaitNode wait)
                return wait.NextNodeId;

            return string.Empty;
        }

        public static void SetTarget(
            NarrativeNode node,
            int index,
            string targetId)
        {
            if (node is NarrativeStartNode start)
                start.SetNextNodeId(targetId);
            else if (node is NarrativeLineNode line)
                line.SetNextNodeId(targetId);
            else if (node is NarrativeChoiceNode choice)
                choice.SetChoiceTarget(index, targetId);
            else if (node is NarrativeConditionNode condition)
            {
                if (index == 0)
                    condition.SetTrueNodeId(targetId);
                else
                    condition.SetFalseNodeId(targetId);
            }
            else if (node is NarrativeSetValueNode setValue)
                setValue.SetNextNodeId(targetId);
            else if (node is NarrativeEventNode eventNode)
                eventNode.SetNextNodeId(targetId);
            else if (node is NarrativeWaitNode wait)
                wait.SetNextNodeId(targetId);
        }

        public static void ClearAllTargets(NarrativeNode node)
        {
            int count = GetOutputCount(node);

            for (int i = 0; i < count; i++)
                SetTarget(node, i, string.Empty);
        }

        public static void ClearReferencesTo(
            IEnumerable<NarrativeNode> nodes,
            string targetId)
        {
            foreach (NarrativeNode node in nodes)
            {
                if (node == null)
                    continue;

                int count = GetOutputCount(node);

                for (int i = 0; i < count; i++)
                {
                    if (GetTarget(node, i) == targetId)
                        SetTarget(node, i, string.Empty);
                }
            }
        }
    }
}
