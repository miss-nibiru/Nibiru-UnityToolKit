using System.Collections.Generic;
using MissNibiru.Narrative;

namespace MissNibiru.Narrative.Editor
{
    internal sealed class NarrativePreviewSession
    {
        private const int MaximumSteps = 1000;
        private readonly List<NarrativeChoiceOption> _choices =
            new List<NarrativeChoiceOption>();
        private readonly List<int> _choiceIndices = new List<int>();
        private NarrativeStory _story;
        private NarrativeBlackboard _blackboard;

        public NarrativeNode CurrentNode { get; private set; }
        public NarrativeLineNode CurrentLine =>
            CurrentNode as NarrativeLineNode;
        public NarrativeChoiceNode CurrentChoice =>
            CurrentNode as NarrativeChoiceNode;
        public string CurrentText => CurrentLine == null
            ? string.Empty
            : CurrentLine.ResolveText(_blackboard);
        public IReadOnlyList<NarrativeChoiceOption> Choices => _choices;
        public string Error { get; private set; } = string.Empty;
        public bool IsComplete { get; private set; }

        public void Start(NarrativeStory story)
        {
            _story = story;
            _blackboard = story == null
                ? null
                : new NarrativeBlackboard(story);
            CurrentNode = story?.FindNode(story.StartNodeId);
            Error = string.Empty;
            IsComplete = false;
            _choices.Clear();
            _choiceIndices.Clear();

            if (story == null)
                return;

            Process();
        }

        public void Next()
        {
            if (CurrentLine == null)
                return;

            CurrentNode = _story.FindNode(CurrentLine.NextNodeId);
            Process();
        }

        public void Choose(int visibleIndex)
        {
            if (CurrentChoice == null || visibleIndex < 0 ||
                visibleIndex >= _choiceIndices.Count)
            {
                return;
            }

            NarrativeChoiceOption option = CurrentChoice.GetChoice(
                _choiceIndices[visibleIndex]);
            CurrentNode = option == null
                ? null
                : _story.FindNode(option.TargetNodeId);
            Process();
        }

        private void Process()
        {
            int steps = 0;
            _choices.Clear();
            _choiceIndices.Clear();

            while (CurrentNode != null &&
                   !(CurrentNode is NarrativeLineNode) &&
                   !(CurrentNode is NarrativeChoiceNode) &&
                   !(CurrentNode is NarrativeEndNode))
            {
                if (++steps > MaximumSteps)
                {
                    Error = "Preview exceeded the step limit.";
                    return;
                }

                if (CurrentNode is NarrativeStartNode start)
                {
                    CurrentNode = _story.FindNode(start.NextNodeId);
                }
                else if (CurrentNode is NarrativeConditionNode condition)
                {
                    bool result = condition.Condition == null ||
                                  condition.Condition.Evaluate(_blackboard);
                    CurrentNode = _story.FindNode(result
                        ? condition.TrueNodeId
                        : condition.FalseNodeId);
                }
                else if (CurrentNode is NarrativeSetValueNode setValue)
                {
                    _blackboard.Apply(setValue);
                    CurrentNode = _story.FindNode(setValue.NextNodeId);
                }
                else if (CurrentNode is NarrativeRandomValueNode randomValue)
                {
                    if (randomValue.Variable == null)
                    {
                        Error = "Random Value has no variable.";
                        return;
                    }

                    int maximumExclusive = randomValue.MaximumInclusive ==
                                           int.MaxValue
                        ? int.MaxValue
                        : randomValue.MaximumInclusive + 1;
                    _blackboard.SetInteger(
                        randomValue.Variable,
                        UnityEngine.Random.Range(
                            randomValue.MinimumInclusive,
                            maximumExclusive));
                    CurrentNode = _story.FindNode(
                        randomValue.NextNodeId);
                }
                else if (CurrentNode is NarrativeEventNode eventNode)
                {
                    CurrentNode = _story.FindNode(eventNode.NextNodeId);
                }
                else if (CurrentNode is NarrativeWaitNode wait)
                {
                    CurrentNode = _story.FindNode(wait.NextNodeId);
                }
                else
                {
                    Error = "Preview found an unsupported node.";
                    return;
                }
            }

            if (CurrentNode == null)
            {
                Error = "Preview reached a broken link.";
                return;
            }

            if (CurrentNode is NarrativeEndNode)
            {
                IsComplete = true;
                return;
            }

            if (CurrentNode is NarrativeChoiceNode choice)
            {
                for (int i = 0; i < choice.Choices.Count; i++)
                {
                    NarrativeChoiceOption option = choice.Choices[i];

                    if (option != null && option.IsAvailable(_blackboard))
                    {
                        _choices.Add(option);
                        _choiceIndices.Add(i);
                    }
                }

                if (_choices.Count == 0)
                    Error = "No choices are available.";
            }
        }
    }
}
