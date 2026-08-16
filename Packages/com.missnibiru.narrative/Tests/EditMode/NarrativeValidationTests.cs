using System.Linq;
using MissNibiru.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Narrative.Tests
{
    public sealed class NarrativeValidationTests
    {
        private const string TestFolder =
            "Assets/__MissNibiruNarrativeTests";
        private NarrativeStory _story;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets", "__MissNibiruNarrativeTests");
            }

            _story = NarrativeAssetFactory.CreateStory(
                TestFolder + "/Story.asset");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void NewStory_HasStartAndEnd()
        {
            Assert.That(_story, Is.Not.Null);
            Assert.That(_story.FindNode(_story.StartNodeId),
                Is.TypeOf<NarrativeStartNode>());
            Assert.That(_story.Nodes.OfType<NarrativeEndNode>().Count(),
                Is.EqualTo(1));
        }

        [Test]
        public void Validator_FindsBrokenLink()
        {
            NarrativeLineNode line =
                NarrativeAssetFactory.AddNode<NarrativeLineNode>(
                    _story, Vector2.zero);
            line.SetNextNodeId("missing_node");
            NarrativeStartNode start =
                _story.FindNode(_story.StartNodeId) as NarrativeStartNode;
            start.SetNextNodeId(line.Id);

            var issues = NarrativeValidator.Validate(_story);

            Assert.That(issues.Any(issue => issue.Code == "NAR200"),
                Is.True);
        }

        [Test]
        public void Validator_FindsDuplicateNodeIds()
        {
            NarrativeLineNode first =
                NarrativeAssetFactory.AddNode<NarrativeLineNode>(
                    _story, Vector2.zero);
            NarrativeLineNode second =
                NarrativeAssetFactory.AddNode<NarrativeLineNode>(
                    _story, Vector2.one);
            second.Initialize(first.Id, Vector2.one);

            var issues = NarrativeValidator.Validate(_story);

            Assert.That(issues.Any(issue => issue.Code == "NAR006"),
                Is.True);
        }

        [Test]
        public void CountWords_IgnoresExtraWhitespace()
        {
            Assert.That(
                NarrativeValidator.CountWords(" one  two\nthree "),
                Is.EqualTo(3));
        }

        [Test]
        public void DeleteNode_ClearsIncomingLinks()
        {
            NarrativeStartNode start =
                _story.FindNode(_story.StartNodeId) as NarrativeStartNode;
            NarrativeLineNode line =
                NarrativeAssetFactory.AddNode<NarrativeLineNode>(
                    _story, Vector2.zero);
            start.SetNextNodeId(line.Id);

            Assert.That(
                NarrativeAssetFactory.DeleteNode(_story, line),
                Is.True);
            Assert.That(start.NextNodeId, Is.Empty);
        }
    }
}
