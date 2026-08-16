using System.Linq;
using MissNibiru.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Narrative.Tests
{
    public sealed class NarrativeRuntimeTests
    {
        private const string TestFolder =
            "Assets/__MissNibiruNarrativeRuntimeTests";
        private NarrativeStory _story;
        private GameObject _runnerObject;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets", "__MissNibiruNarrativeRuntimeTests");
            }

            _story = NarrativeAssetFactory.CreateStory(
                TestFolder + "/Story.asset");
            _runnerObject = new GameObject("Narrative Runner Test");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_runnerObject);
            AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Runner_PresentsConnectedLine()
        {
            NarrativeStartNode start =
                _story.FindNode(_story.StartNodeId) as NarrativeStartNode;
            NarrativeLineNode line =
                NarrativeAssetFactory.AddNode<NarrativeLineNode>(
                    _story, Vector2.zero);
            NarrativeEndNode end =
                _story.Nodes.OfType<NarrativeEndNode>().First();
            start.SetNextNodeId(line.Id);
            line.SetNextNodeId(end.Id);
            NarrativeRunner runner =
                _runnerObject.AddComponent<NarrativeRunner>();
            runner.SetStory(_story);
            NarrativeLineNode presented = null;
            runner.LinePresented += value => presented = value;

            runner.StartStory();

            Assert.That(presented, Is.SameAs(line));
            Assert.That(runner.IsAwaitingInput, Is.True);
        }

        [Test]
        public void Runner_AdvanceCompletesStory()
        {
            NarrativeStartNode start =
                _story.FindNode(_story.StartNodeId) as NarrativeStartNode;
            NarrativeLineNode line =
                NarrativeAssetFactory.AddNode<NarrativeLineNode>(
                    _story, Vector2.zero);
            NarrativeEndNode end =
                _story.Nodes.OfType<NarrativeEndNode>().First();
            start.SetNextNodeId(line.Id);
            line.SetNextNodeId(end.Id);
            NarrativeRunner runner =
                _runnerObject.AddComponent<NarrativeRunner>();
            runner.SetStory(_story);
            bool completed = false;
            runner.StoryCompleted += value => completed = value == end;

            runner.StartStory();
            runner.Advance();

            Assert.That(completed, Is.True);
            Assert.That(runner.IsRunning, Is.False);
        }

        [Test]
        public void Runner_PresentsAndChoosesBranch()
        {
            NarrativeStartNode start =
                _story.FindNode(_story.StartNodeId) as NarrativeStartNode;
            NarrativeChoiceNode choice =
                NarrativeAssetFactory.AddNode<NarrativeChoiceNode>(
                    _story, Vector2.zero);
            NarrativeEndNode end =
                _story.Nodes.OfType<NarrativeEndNode>().First();
            SerializedObject serializedChoice = new SerializedObject(choice);
            SerializedProperty choices =
                serializedChoice.FindProperty("choices");
            choices.arraySize = 1;
            SerializedProperty option = choices.GetArrayElementAtIndex(0);
            option.FindPropertyRelative("text").stringValue = "Continue";
            option.FindPropertyRelative("targetNodeId").stringValue = end.Id;
            serializedChoice.ApplyModifiedPropertiesWithoutUndo();
            start.SetNextNodeId(choice.Id);
            NarrativeRunner runner =
                _runnerObject.AddComponent<NarrativeRunner>();
            runner.SetStory(_story);
            int visibleChoices = 0;
            runner.ChoicesPresented += (node, presented) =>
                visibleChoices = presented.Count;

            runner.StartStory();
            runner.Choose(0);

            Assert.That(visibleChoices, Is.EqualTo(1));
            Assert.That(runner.IsRunning, Is.False);
        }

        [Test]
        public void SaveData_RoundTripsJson()
        {
            NarrativeSaveData source = new NarrativeSaveData
            {
                storyId = "story",
                currentNodeId = "line_1"
            };
            source.flags.Add(new NarrativeFlagSaveValue
            {
                id = "met_hero",
                value = true
            });

            NarrativeSaveData result = NarrativeSaveData.FromJson(
                source.ToJson(false));

            Assert.That(result.storyId, Is.EqualTo("story"));
            Assert.That(result.currentNodeId, Is.EqualTo("line_1"));
            Assert.That(result.flags.Single().value, Is.True);
        }

        [Test]
        public void Blackboard_AppliesFlagMutation()
        {
            NarrativeFlag flag =
                NarrativeAssetFactory.CreateLibraryAsset<NarrativeFlag>(
                    TestFolder + "/Flag.asset", _story);
            NarrativeSetValueNode setValue =
                NarrativeAssetFactory.AddNode<NarrativeSetValueNode>(
                    _story, Vector2.zero);
            SerializedObject serializedSet = new SerializedObject(setValue);
            serializedSet.FindProperty("flag").objectReferenceValue = flag;
            serializedSet.FindProperty("booleanValue").boolValue = true;
            serializedSet.ApplyModifiedPropertiesWithoutUndo();
            NarrativeBlackboard blackboard =
                new NarrativeBlackboard(_story);

            blackboard.Apply(setValue);

            Assert.That(blackboard.GetFlag(flag), Is.True);
        }

        [Test]
        public void PresentationRect_ClampsInsideScreen()
        {
            NarrativeRect value = new NarrativeRect(
                0.9f, 0.9f, 0.5f, 0.5f);

            Assert.That(value.x + value.width, Is.LessThanOrEqualTo(1f));
            Assert.That(value.y + value.height, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void PresentationProfile_StoresCustomLayout()
        {
            DialoguePresentationProfile profile =
                ScriptableObject.CreateInstance<DialoguePresentationProfile>();
            NarrativeRect expected = new NarrativeRect(
                0.1f, 0.2f, 0.3f, 0.4f);

            profile.SetRect(NarrativeLayoutElement.BodyText, expected);
            NarrativeRect result = profile.GetRect(
                NarrativeLayoutElement.BodyText);

            Assert.That(result.x, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(result.width, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(0.4f).Within(0.001f));
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }
}
