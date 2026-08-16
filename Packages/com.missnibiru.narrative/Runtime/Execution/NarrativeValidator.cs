using System;
using System.Collections.Generic;

namespace MissNibiru.Narrative
{
    public static class NarrativeValidator
    {
        public static List<NarrativeValidationIssue> Validate(
            NarrativeStory story)
        {
            List<NarrativeValidationIssue> issues =
                new List<NarrativeValidationIssue>();

            if (story == null)
            {
                issues.Add(new NarrativeValidationIssue(
                    NarrativeValidationSeverity.Error,
                    "NAR001",
                    "Assign a narrative story."));
                return issues;
            }

            if (string.IsNullOrWhiteSpace(story.Id))
            {
                Add(issues, NarrativeValidationSeverity.Error,
                    "NAR002", "Story ID is missing.", context: story);
            }

            if (story.PresentationProfile == null)
            {
                Add(issues, NarrativeValidationSeverity.Warning,
                    "NAR003", "Presentation profile is missing.",
                    context: story);
            }

            Dictionary<string, NarrativeNode> nodes =
                new Dictionary<string, NarrativeNode>(
                    StringComparer.Ordinal);

            foreach (NarrativeNode node in story.Nodes)
            {
                if (node == null)
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR004", "Story contains a missing node.",
                        context: story);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR005", "Node ID is missing.", node, node);
                    continue;
                }

                if (nodes.ContainsKey(node.Id))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR006", $"Duplicate node ID: {node.Id}.",
                        node, node);
                }
                else
                {
                    nodes.Add(node.Id, node);
                }

                ValidateNode(story, node, issues);
            }

            NarrativeNode start = story.FindNode(story.StartNodeId);

            if (start == null)
            {
                Add(issues, NarrativeValidationSeverity.Error,
                    "NAR007", "Start node is missing.", context: story);
            }
            else if (!(start is NarrativeStartNode))
            {
                Add(issues, NarrativeValidationSeverity.Error,
                    "NAR008", "Story start is not a Start node.",
                    start, start);
            }

            ValidateLinks(story, nodes, issues);
            ValidateReachability(start, nodes, issues);
            ValidateLibrary(story.Characters, "character", issues, story);
            ValidateLibrary(story.Variables, "variable", issues, story);
            ValidateLibrary(story.Flags, "flag", issues, story);
            ValidateLibrary(story.GameplayEvents, "event", issues, story);

            if (issues.Count == 0)
            {
                Add(issues, NarrativeValidationSeverity.Information,
                    "NAR000", "Story is ready.", context: story);
            }

            return issues;
        }

        public static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(
                (char[])null,
                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static void ValidateNode(
            NarrativeStory story,
            NarrativeNode node,
            List<NarrativeValidationIssue> issues)
        {
            if (node is NarrativeStartNode start &&
                string.IsNullOrWhiteSpace(start.NextNodeId))
            {
                Add(issues, NarrativeValidationSeverity.Error,
                    "NAR091", "Start is not connected.", start, start);
            }
            else if (node is NarrativeLineNode line)
            {
                if (string.IsNullOrWhiteSpace(line.Text))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR101", "Dialogue text is empty.", line, line);
                }

                int limit = line.WordLimit > 0
                    ? line.WordLimit
                    : story.DefaultLineWordLimit;
                int words = CountWords(line.Text);

                if (words > limit)
                {
                    Add(issues, NarrativeValidationSeverity.Warning,
                        "NAR102", $"Line has {words}/{limit} words.",
                        line, line);
                }

                if (line.Character != null &&
                    !line.Character.SupportsEmotion(line.Emotion))
                {
                    Add(issues, NarrativeValidationSeverity.Warning,
                        "NAR103",
                        "Character has no portrait for this emotion.",
                        line, line.Character);
                }

                if (string.IsNullOrWhiteSpace(line.NextNodeId))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR104", "Dialogue line is not connected.",
                        line, line);
                }
            }
            else if (node is NarrativeChoiceNode choice)
            {
                if (choice.Choices.Count == 0)
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR110", "Choice node has no choices.",
                        choice, choice);
                }

                if (choice.Choices.Count > NarrativeChoiceNode.MaximumChoices)
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR111", "Choice node exceeds five choices.",
                        choice, choice);
                }

                for (int i = 0; i < choice.Choices.Count; i++)
                {
                    NarrativeChoiceOption option = choice.Choices[i];

                    if (option == null ||
                        string.IsNullOrWhiteSpace(option.Text))
                    {
                        Add(issues, NarrativeValidationSeverity.Error,
                            "NAR112", $"Choice {i + 1} is empty.",
                            choice, choice);
                        continue;
                    }

                    int words = CountWords(option.Text);

                    if (words > option.WordLimit)
                    {
                        Add(issues, NarrativeValidationSeverity.Warning,
                            "NAR113",
                            $"Choice {i + 1} has {words}/{option.WordLimit} words.",
                            choice, choice);
                    }

                    if (string.IsNullOrWhiteSpace(option.TargetNodeId))
                    {
                        Add(issues, NarrativeValidationSeverity.Error,
                            "NAR114", $"Choice {i + 1} is not connected.",
                            choice, choice);
                    }

                    ValidateCondition(option.Condition, choice, issues);
                }
            }
            else if (node is NarrativeConditionNode condition)
            {
                if (condition.Condition == null)
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR120", "Condition is missing.",
                        condition, condition);
                }

                if (string.IsNullOrWhiteSpace(condition.TrueNodeId) ||
                    string.IsNullOrWhiteSpace(condition.FalseNodeId))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR123", "Condition needs both branches.",
                        condition, condition);
                }
            }
            else if (node is NarrativeSetValueNode setValue)
            {
                if (setValue.Flag == null && setValue.Variable == null)
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR130", "Set Value has no target.",
                        setValue, setValue);
                }

                if (string.IsNullOrWhiteSpace(setValue.NextNodeId))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR131", "Set Value is not connected.",
                        setValue, setValue);
                }
            }
            else if (node is NarrativeEventNode eventNode)
            {
                if (eventNode.GameplayEvent == null)
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR140", "Gameplay Event is missing.",
                        eventNode, eventNode);
                }

                if (string.IsNullOrWhiteSpace(eventNode.NextNodeId))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR141", "Gameplay Event is not connected.",
                        eventNode, eventNode);
                }
            }
            else if (node is NarrativeWaitNode wait &&
                     string.IsNullOrWhiteSpace(wait.NextNodeId))
            {
                Add(issues, NarrativeValidationSeverity.Error,
                    "NAR151", "Wait is not connected.", wait, wait);
            }

            ValidateCondition(GetCondition(node), node, issues);
        }

        private static NarrativeCondition GetCondition(NarrativeNode node)
        {
            if (node is NarrativeConditionNode conditionNode)
                return conditionNode.Condition;

            return null;
        }

        private static void ValidateCondition(
            NarrativeCondition condition,
            NarrativeNode node,
            List<NarrativeValidationIssue> issues)
        {
            if (condition == null)
                return;

            if (condition.Mode == NarrativeConditionMode.Flag &&
                condition.Flag == null)
            {
                Add(issues, NarrativeValidationSeverity.Error,
                    "NAR121", "Condition flag is missing.", node, node);
            }

            if (condition.Mode == NarrativeConditionMode.Variable &&
                condition.Variable == null)
            {
                Add(issues, NarrativeValidationSeverity.Error,
                    "NAR122", "Condition variable is missing.", node, node);
            }
        }

        private static void ValidateLinks(
            NarrativeStory story,
            Dictionary<string, NarrativeNode> nodes,
            List<NarrativeValidationIssue> issues)
        {
            foreach (NarrativeNode node in story.Nodes)
            {
                if (node == null)
                    continue;

                foreach (string target in node.GetOutgoingNodeIds())
                {
                    if (!string.IsNullOrWhiteSpace(target) &&
                        !nodes.ContainsKey(target))
                    {
                        Add(issues, NarrativeValidationSeverity.Error,
                            "NAR200", $"Broken link to: {target}.",
                            node, node);
                    }
                }
            }
        }

        private static void ValidateReachability(
            NarrativeNode start,
            Dictionary<string, NarrativeNode> nodes,
            List<NarrativeValidationIssue> issues)
        {
            if (start == null)
                return;

            HashSet<string> visited = new HashSet<string>();
            Stack<NarrativeNode> pending = new Stack<NarrativeNode>();
            pending.Push(start);

            while (pending.Count > 0)
            {
                NarrativeNode node = pending.Pop();

                if (node == null || !visited.Add(node.Id))
                    continue;

                foreach (string target in node.GetOutgoingNodeIds())
                {
                    if (nodes.TryGetValue(target, out NarrativeNode next))
                        pending.Push(next);
                }
            }

            foreach (KeyValuePair<string, NarrativeNode> pair in nodes)
            {
                if (!visited.Contains(pair.Key))
                {
                    Add(issues, NarrativeValidationSeverity.Warning,
                        "NAR201", "Node cannot be reached from Start.",
                        pair.Value, pair.Value);
                }
            }
        }

        private static void ValidateLibrary<T>(
            IReadOnlyList<T> assets,
            string label,
            List<NarrativeValidationIssue> issues,
            NarrativeStory story)
            where T : UnityEngine.Object
        {
            HashSet<string> ids = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (T asset in assets)
            {
                if (asset == null)
                {
                    Add(issues, NarrativeValidationSeverity.Warning,
                        "NAR300", $"Story has a missing {label}.",
                        context: story);
                    continue;
                }

                string id = GetAssetId(asset);

                if (string.IsNullOrWhiteSpace(id))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR301", $"{label} ID is missing.",
                        context: asset);
                }
                else if (!ids.Add(id))
                {
                    Add(issues, NarrativeValidationSeverity.Error,
                        "NAR302", $"Duplicate {label} ID: {id}.",
                        context: asset);
                }
            }
        }

        private static string GetAssetId(UnityEngine.Object asset)
        {
            if (asset is NarrativeCharacter character)
                return character.Id;
            if (asset is NarrativeVariable variable)
                return variable.Id;
            if (asset is NarrativeFlag flag)
                return flag.Id;
            if (asset is NarrativeEvent gameplayEvent)
                return gameplayEvent.Id;

            return asset == null ? string.Empty : asset.name;
        }

        private static void Add(
            List<NarrativeValidationIssue> issues,
            NarrativeValidationSeverity severity,
            string code,
            string message,
            NarrativeNode node = null,
            UnityEngine.Object context = null)
        {
            issues.Add(new NarrativeValidationIssue(
                severity, code, message, node, context));
        }
    }
}
