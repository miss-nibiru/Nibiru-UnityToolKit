using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    [AddComponentMenu("Miss Nibiru/Narrative/Narrative Runner")]
    public sealed class NarrativeRunner : MonoBehaviour
    {
        private const int MaximumAutomaticSteps = 1000;

        [SerializeField]
        private NarrativeStory story;

        [SerializeField]
        private bool playOnStart;

        private readonly List<NarrativeChoiceOption> _availableChoices =
            new List<NarrativeChoiceOption>();
        private readonly List<int> _availableChoiceIndices =
            new List<int>();

        private NarrativeBlackboard _blackboard;
        private NarrativeNode _currentNode;
        private Coroutine _waitRoutine;
        private bool _isRunning;
        private bool _awaitingInput;

        public event Action<NarrativeLineNode> LinePresented;
        public event Action<NarrativeChoiceNode,
            IReadOnlyList<NarrativeChoiceOption>> ChoicesPresented;
        public event Action<NarrativeEventNode> GameplayEventTriggered;
        public event Action<NarrativeEndNode> StoryCompleted;
        public event Action<string> StoryFaulted;

        public NarrativeStory Story => story;
        public NarrativeNode CurrentNode => _currentNode;
        public NarrativeBlackboard Blackboard => _blackboard;
        public bool IsRunning => _isRunning;
        public bool IsAwaitingInput => _awaitingInput;

        private void Start()
        {
            if (playOnStart)
                StartStory();
        }

        public void SetStory(NarrativeStory value)
        {
            if (_isRunning)
                StopStory();

            story = value;
        }

        public void StartStory()
        {
            StopWait();

            if (story == null)
            {
                Fault("Assign a Narrative Story.");
                return;
            }

            NarrativeNode start = story.FindNode(story.StartNodeId);

            if (start == null)
            {
                Fault("The story has no valid Start node.");
                return;
            }

            _blackboard = new NarrativeBlackboard(story);
            _currentNode = start;
            _isRunning = true;
            _awaitingInput = false;
            ProcessAutomaticNodes();
        }

        public void StartSequence()
        {
            StartStory();
        }

        public void StopStory()
        {
            StopWait();
            _isRunning = false;
            _awaitingInput = false;
            _currentNode = null;
            _availableChoices.Clear();
            _availableChoiceIndices.Clear();
        }

        public void Advance()
        {
            if (!_isRunning || !_awaitingInput ||
                !(_currentNode is NarrativeLineNode line))
            {
                return;
            }

            _awaitingInput = false;
            MoveTo(line.NextNodeId);
        }

        public void Choose(int visibleChoiceIndex)
        {
            if (!_isRunning || !_awaitingInput ||
                !(_currentNode is NarrativeChoiceNode choice) ||
                visibleChoiceIndex < 0 ||
                visibleChoiceIndex >= _availableChoiceIndices.Count)
            {
                return;
            }

            int sourceIndex = _availableChoiceIndices[visibleChoiceIndex];
            NarrativeChoiceOption option = choice.GetChoice(sourceIndex);

            if (option == null)
            {
                Fault("The selected choice is missing.");
                return;
            }

            _awaitingInput = false;
            MoveTo(option.TargetNodeId);
        }

        public NarrativeSaveData CreateSaveData()
        {
            if (story == null || _blackboard == null)
                return null;

            return _blackboard.CreateSaveData(
                story,
                _currentNode == null ? string.Empty : _currentNode.Id);
        }

        public string CreateSaveJson(bool prettyPrint = true)
        {
            NarrativeSaveData data = CreateSaveData();
            return data == null ? string.Empty : data.ToJson(prettyPrint);
        }

        public bool Resume(NarrativeSaveData data)
        {
            StopWait();

            if (story == null || data == null ||
                (!string.IsNullOrWhiteSpace(data.storyId) &&
                 data.storyId != story.Id))
            {
                Fault("Save data does not match this story.");
                return false;
            }

            NarrativeNode node = story.FindNode(data.currentNodeId);

            if (node == null)
            {
                Fault("The saved node no longer exists.");
                return false;
            }

            _blackboard = new NarrativeBlackboard(story);
            _blackboard.Restore(story, data);
            _currentNode = node;
            _isRunning = true;
            _awaitingInput = false;
            ProcessAutomaticNodes();
            return true;
        }

        public bool ResumeFromJson(string json)
        {
            return Resume(NarrativeSaveData.FromJson(json));
        }

        public void SaveToPlayerPrefs(string slot = "default")
        {
            string json = CreateSaveJson(false);

            if (!string.IsNullOrEmpty(json))
                PlayerPrefs.SetString(GetSaveKey(slot), json);
        }

        public bool ResumeFromPlayerPrefs(string slot = "default")
        {
            string key = GetSaveKey(slot);
            return PlayerPrefs.HasKey(key) &&
                   ResumeFromJson(PlayerPrefs.GetString(key));
        }

        private void ProcessAutomaticNodes()
        {
            int steps = 0;

            while (_isRunning && !_awaitingInput && _currentNode != null)
            {
                steps++;

                if (steps > MaximumAutomaticSteps)
                {
                    Fault("Story exceeded the automatic step limit.");
                    return;
                }

                if (_currentNode is NarrativeStartNode start)
                {
                    SetCurrent(start.NextNodeId);
                }
                else if (_currentNode is NarrativeLineNode line)
                {
                    _awaitingInput = true;
                    LinePresented?.Invoke(line);
                }
                else if (_currentNode is NarrativeChoiceNode choice)
                {
                    PresentChoices(choice);
                }
                else if (_currentNode is NarrativeConditionNode condition)
                {
                    bool result = condition.Evaluate(_blackboard);
                    SetCurrent(result
                        ? condition.TrueNodeId
                        : condition.FalseNodeId);
                }
                else if (_currentNode is NarrativeSetValueNode setValue)
                {
                    _blackboard.Apply(setValue);
                    SetCurrent(setValue.NextNodeId);
                }
                else if (_currentNode is NarrativeRandomValueNode randomValue)
                {
                    if (randomValue.Variable == null)
                    {
                        Fault("Random Value has no variable.");
                        return;
                    }

                    int maximumExclusive = randomValue.MaximumInclusive ==
                                           int.MaxValue
                        ? int.MaxValue
                        : randomValue.MaximumInclusive + 1;
                    int value = UnityEngine.Random.Range(
                        randomValue.MinimumInclusive,
                        maximumExclusive);
                    _blackboard.SetInteger(randomValue.Variable, value);
                    SetCurrent(randomValue.NextNodeId);
                }
                else if (_currentNode is NarrativeEventNode eventNode)
                {
                    eventNode.GameplayEvent?.Raise(eventNode.Payload);
                    GameplayEventTriggered?.Invoke(eventNode);
                    SetCurrent(eventNode.NextNodeId);
                }
                else if (_currentNode is NarrativeWaitNode wait)
                {
                    _waitRoutine = StartCoroutine(WaitAndContinue(wait));
                    return;
                }
                else if (_currentNode is NarrativeEndNode end)
                {
                    _isRunning = false;
                    _awaitingInput = false;
                    StoryCompleted?.Invoke(end);
                }
                else
                {
                    Fault("Story contains an unsupported node.");
                }
            }
        }

        private void PresentChoices(NarrativeChoiceNode node)
        {
            _availableChoices.Clear();
            _availableChoiceIndices.Clear();

            for (int i = 0; i < node.Choices.Count; i++)
            {
                NarrativeChoiceOption option = node.Choices[i];

                if (option != null && option.IsAvailable(_blackboard))
                {
                    _availableChoices.Add(option);
                    _availableChoiceIndices.Add(i);
                }
            }

            if (_availableChoices.Count == 0)
            {
                Fault("No choices are currently available.");
                return;
            }

            _awaitingInput = true;
            ChoicesPresented?.Invoke(node, _availableChoices);
        }

        private IEnumerator WaitAndContinue(NarrativeWaitNode wait)
        {
            float duration = wait.Duration;

            if (duration > 0f)
            {
                if (wait.UseUnscaledTime)
                    yield return new WaitForSecondsRealtime(duration);
                else
                    yield return new WaitForSeconds(duration);
            }

            _waitRoutine = null;
            SetCurrent(wait.NextNodeId);
            ProcessAutomaticNodes();
        }

        private void MoveTo(string nodeId)
        {
            SetCurrent(nodeId);
            ProcessAutomaticNodes();
        }

        private void SetCurrent(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                Fault("A story path ends without an End node.");
                return;
            }

            NarrativeNode next = story.FindNode(nodeId);

            if (next == null)
            {
                Fault($"Story link is broken: {nodeId}.");
                return;
            }

            _currentNode = next;
        }

        private void Fault(string message)
        {
            StopWait();
            _isRunning = false;
            _awaitingInput = false;
            StoryFaulted?.Invoke(message);
            Debug.LogError(message, this);
        }

        private void StopWait()
        {
            if (_waitRoutine == null)
                return;

            StopCoroutine(_waitRoutine);
            _waitRoutine = null;
        }

        private string GetSaveKey(string slot)
        {
            string storyId = story == null ? "story" : story.Id;
            return $"MissNibiru.Narrative.{storyId}.{slot ?? "default"}";
        }
    }
}
