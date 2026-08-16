using System.Collections.Generic;
using System.IO;
using System.Linq;
using MissNibiru.Narrative;
using MissNibiru.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Narrative.Tests
{
    public sealed class TweeImporterTests
    {
        private const string TestFolder =
            "Assets/__MissNibiruTweeImporterTests";
        private string _sourcePath;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets", "__MissNibiruTweeImporterTests");
            }

            _sourcePath = Path.Combine(
                Application.temporaryCachePath,
                "MissNibiruNarrativeTest.twee");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_sourcePath))
                File.Delete(_sourcePath);

            AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Parser_ReadsSugarCubePassagesAndActions()
        {
            TweeStoryData story = TweeParser.Parse(CreateSource());
            TweePassageData start = story.FindPassage("Start");

            Assert.That(story.Title, Is.EqualTo("Alchemy Test"));
            Assert.That(story.StartPassage, Is.EqualTo("Start"));
            Assert.That(start.HasPosition, Is.True);
            Assert.That(start.Links.Count, Is.EqualTo(1));
            Assert.That(start.Links[0].Target, Is.EqualTo("Ending"));
            Assert.That(start.Links[0].Mutations.Single().VariableName,
                Is.EqualTo("AlchemyCurrent"));
        }

        [Test]
        public void Import_CreatesStoryVariablesFlagsAndNodes()
        {
            File.WriteAllText(_sourcePath, CreateSource());

            TweeImportResult result = TweeImportService.ImportFile(
                _sourcePath,
                TestFolder + "/ImportedStory.asset");

            Assert.That(result.Story, Is.Not.Null);
            Assert.That(result.PassageCount, Is.EqualTo(2));
            Assert.That(result.VariableCount, Is.EqualTo(1));
            Assert.That(result.FlagCount, Is.EqualTo(1));
            Assert.That(result.Story.Variables.Single().Id,
                Is.EqualTo("alchemycurrent"));
            Assert.That(result.Story.Flags.Single().Id,
                Is.EqualTo("visitedhall"));
            Assert.That(result.Story.Nodes.OfType<NarrativeChoiceNode>().Any(),
                Is.True);
        }

        [Test]
        public void Blackboard_AllowsGameplayAlchemyChanges()
        {
            NarrativeStory story = NarrativeAssetFactory.CreateStory(
                TestFolder + "/RuntimeStory.asset");
            NarrativeVariable alchemy =
                NarrativeAssetFactory.CreateLibraryAsset<NarrativeVariable>(
                    TestFolder + "/Alchemy.asset",
                    story);
            alchemy.Configure(
                "alchemy_current",
                "Alchemy Current",
                NarrativeVariableType.Integer);
            alchemy.SetDefault(3);
            NarrativeBlackboard blackboard = new NarrativeBlackboard(story);

            blackboard.AddInteger(alchemy, -1);

            Assert.That(blackboard.GetInteger(alchemy), Is.EqualTo(2));
        }

        [Test]
        public void ConditionCompiler_EvaluatesAndOrAndNot()
        {
            NarrativeStory story = NarrativeAssetFactory.CreateStory(
                TestFolder + "/ConditionStory.asset");
            NarrativeVariable madness =
                NarrativeAssetFactory.CreateLibraryAsset<NarrativeVariable>(
                    TestFolder + "/Madness.asset",
                    story);
            madness.Configure(
                "madness",
                "Madness",
                NarrativeVariableType.Integer);
            madness.SetDefault(3);
            NarrativeFlag visited =
                NarrativeAssetFactory.CreateLibraryAsset<NarrativeFlag>(
                    TestFolder + "/Visited.asset",
                    story);
            visited.Configure("visited", "Visited", false);
            Dictionary<string, NarrativeVariable> variables =
                new Dictionary<string, NarrativeVariable>
                {
                    { "Madness", madness }
                };
            Dictionary<string, NarrativeFlag> flags =
                new Dictionary<string, NarrativeFlag>
                {
                    { "Visited", visited }
                };
            List<TweeImportIssue> issues = new List<TweeImportIssue>();
            NarrativeConditionExpression expression =
                TweeConditionCompiler.Compile(
                    "$Madness gte 2 and not $Visited",
                    flags,
                    variables,
                    issues,
                    "Test");

            Assert.That(
                expression.Evaluate(new NarrativeBlackboard(story)),
                Is.True);
            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void Import_SplitsMoreThanFiveLinksWithoutDroppingThem()
        {
            string choices = string.Join(
                "\n",
                Enumerable.Range(1, 6).Select(index =>
                    $"[[Choice {index}|Ending]]"));
            string source = ":: StoryTitle\nPaged Choices\n\n" +
                            ":: StoryData\n" +
                            "{\"format\":\"SugarCube\"," +
                            "\"start\":\"Start\"}\n\n" +
                            ":: Start\n" + choices + "\n\n" +
                            ":: Ending\nDone.\n";
            File.WriteAllText(_sourcePath, source);

            TweeImportResult result = TweeImportService.ImportFile(
                _sourcePath,
                TestFolder + "/PagedStory.asset");
            NarrativeChoiceNode[] pages = result.Story.Nodes
                .OfType<NarrativeChoiceNode>()
                .ToArray();

            Assert.That(pages.Length, Is.EqualTo(2));
            Assert.That(pages.All(page =>
                page.Choices.Count <=
                NarrativeChoiceNode.MaximumChoices), Is.True);
            Assert.That(pages.SelectMany(page => page.Choices)
                .Count(choice => choice.Text.StartsWith("Choice")),
                Is.EqualTo(6));
        }

        [Test]
        public void Analyzer_ReportsRealisticGenerationSummary()
        {
            TweeImportAnalysis analysis = TweeImportAnalyzer.AnalyzeSource(
                CreateRealisticSource());

            Assert.That(analysis.PassageCount, Is.EqualTo(2));
            Assert.That(analysis.DialogueLineCount, Is.EqualTo(4));
            Assert.That(analysis.NarratorLineCount, Is.EqualTo(2));
            Assert.That(analysis.CharacterLineCount, Is.EqualTo(2));
            Assert.That(analysis.DetectedColours,
                Is.EquivalentTo(new[] { "#F4A6B8", "#FF675E" }));
            Assert.That(analysis.AudioDefinitionCount, Is.EqualTo(1));
            Assert.That(analysis.AudioUsageCount, Is.EqualTo(1));
            Assert.That(analysis.ChoiceCount, Is.EqualTo(1));
            Assert.That(analysis.MutationCount, Is.EqualTo(2));
        }

        [Test]
        public void Import_MapsCombinedColoursEmotionAndEverySpeakerLine()
        {
            File.WriteAllText(_sourcePath, CreateRealisticSource());
            NarrativeCharacter serena =
                NarrativeAssetFactory.CreateLibraryAsset<
                    NarrativeCharacter>(
                    TestFolder + "/Serena.asset");
            serena.Configure("serena", "Serena", Color.magenta);
            NarrativeEmotion worried =
                NarrativeAssetFactory.CreateLibraryAsset<NarrativeEmotion>(
                    TestFolder + "/Worried.asset");
            worried.Configure("worried", "Worried");
            TweeSpeakerMapping speaker = new TweeSpeakerMapping();
            speaker.Configure(
                "Serena",
                new[] { "#FF675E", "#F4A6B8" },
                serena,
                worried,
                NarrativePortraitSide.Right);
            TweeAudioMapping voice = new TweeAudioMapping();
            voice.Configure("serena_warning", null, TweeAudioRole.Voice);
            TweeImportProfile profile =
                NarrativeAssetFactory.CreateLibraryAsset<TweeImportProfile>(
                    TestFolder + "/ImportProfile.asset");
            profile.Configure(
                "nerethos",
                "Nerethos",
                new[] { speaker },
                new[] { voice });

            TweeImportResult result = TweeImportService.ImportFile(
                _sourcePath,
                TestFolder + "/MappedStory.asset",
                profile);
            NarrativeLineNode[] characterLines = result.Story.Nodes
                .OfType<NarrativeLineNode>()
                .Where(line => line.Character != null)
                .ToArray();

            Assert.That(characterLines.Length, Is.EqualTo(2));
            Assert.That(characterLines.All(line => line.Character == serena),
                Is.True);
            Assert.That(characterLines.All(line => line.Emotion == worried),
                Is.True);
            Assert.That(characterLines.All(line =>
                line.PortraitSide == NarrativePortraitSide.Right), Is.True);
            Assert.That(result.CharacterCount, Is.EqualTo(1));
            Assert.That(result.NarratorLineCount, Is.EqualTo(2));
            Assert.That(result.CharacterLineCount, Is.EqualTo(2));
            Assert.That(result.AudioUsageCount, Is.EqualTo(1));
            Assert.That(result.UnmappedAudioCount, Is.EqualTo(1));
        }

        [Test]
        public void Import_PreservesOrderedDialogueConditionsAndMutations()
        {
            File.WriteAllText(_sourcePath, CreateRealisticSource());
            TweeImportResult result = TweeImportService.ImportFile(
                _sourcePath,
                TestFolder + "/OrderedStory.asset");
            NarrativeStartNode start = result.Story.FindNode(
                result.Story.StartNodeId) as NarrativeStartNode;
            NarrativeLineNode narrator = result.Story.Nodes
                .OfType<NarrativeLineNode>()
                .Single(line => line.Text == "Narrator first.");
            NarrativeLineNode firstSpeaker = result.Story.Nodes
                .OfType<NarrativeLineNode>()
                .Single(line => line.Text.Contains("Stay close"));
            NarrativeSetValueNode mutation = result.Story.Nodes
                .OfType<NarrativeSetValueNode>()
                .Single(node => node.Variable != null &&
                    node.Variable.Id == "trust");
            NarrativeConditionNode condition = result.Story.Nodes
                .OfType<NarrativeConditionNode>()
                .Single();
            NarrativeLineNode conditionalLine = result.Story.Nodes
                .OfType<NarrativeLineNode>()
                .Single(line => line.Text.Contains("I mean it"));
            NarrativeChoiceNode choice = result.Story.Nodes
                .OfType<NarrativeChoiceNode>()
                .Single();

            Assert.That(start.NextNodeId, Is.EqualTo(narrator.Id));
            Assert.That(narrator.NextNodeId, Is.EqualTo(firstSpeaker.Id));
            Assert.That(firstSpeaker.NextNodeId, Is.EqualTo(mutation.Id));
            Assert.That(mutation.NextNodeId, Is.EqualTo(condition.Id));
            Assert.That(condition.TrueNodeId,
                Is.EqualTo(conditionalLine.Id));
            Assert.That(condition.FalseNodeId, Is.EqualTo(choice.Id));
            Assert.That(conditionalLine.NextNodeId, Is.EqualTo(choice.Id));
            Assert.That(result.ConditionNodeCount, Is.EqualTo(1));
            Assert.That(result.ChoiceNodeCount, Is.EqualTo(1));
        }

        [Test]
        public void Import_CreatesPlaceholderCharactersForUnknownColours()
        {
            File.WriteAllText(_sourcePath, CreateRealisticSource());

            TweeImportResult result = TweeImportService.ImportFile(
                _sourcePath,
                TestFolder + "/PlaceholderStory.asset");

            Assert.That(result.CharacterCount, Is.EqualTo(2));
            Assert.That(result.Story.Characters.Count, Is.EqualTo(2));
            Assert.That(result.Story.Nodes.OfType<NarrativeLineNode>()
                .Where(line => line.Text.Contains("Stay close") ||
                               line.Text.Contains("I mean it"))
                .All(line => line.Character != null), Is.True);
        }

        private static string CreateSource()
        {
            return ":: StoryTitle\n" +
                   "Alchemy Test\n\n" +
                   ":: StoryData\n" +
                   "{\"format\":\"SugarCube\"," +
                   "\"format-version\":\"2.37.3\"," +
                   "\"start\":\"Start\"}\n\n" +
                   ":: StoryInit\n" +
                   "<<set $AlchemyCurrent = 3>>\n" +
                   "<<set $VisitedHall to false>>\n\n" +
                   ":: Start {\"position\":\"100,200\"}\n" +
                   "You hold a vial.\n" +
                   "<<if $AlchemyCurrent gte 1>>" +
                   "[[Drink|Ending][$AlchemyCurrent -= 1]]" +
                   "<</if>>\n\n" +
                   ":: Ending {\"position\":\"400,200\"}\n" +
                   "The vial is empty.\n";
        }

        private static string CreateRealisticSource()
        {
            return ":: StoryTitle\nNerethos Import Test\n\n" +
                   ":: StoryData\n" +
                   "{\"format\":\"SugarCube\"," +
                   "\"format-version\":\"2.37.3\"," +
                   "\"start\":\"Start\"}\n\n" +
                   ":: StoryInit\n" +
                   "<<set $Trust = 0>>\n" +
                   "<<set $Seen to false>>\n" +
                   "<<cacheaudio \"serena_warning\" " +
                   "\"audio/serena_warning.mp3\">>\n\n" +
                   ":: Start {\"position\":\"100,200\"}\n" +
                   "Narrator first.\n\n" +
                   "<span class=\"voice-wrap\">" +
                   "<<link \"Play\">>" +
                   "<<audio \":playing\" stop>>" +
                   "<<audio \"serena_warning\" play>>" +
                   "<</link>></span>" +
                   "<span style=\"color: #FF675E;\">" +
                   "Stay close.</span>\n" +
                   "<<set $Trust += 1>>\n" +
                   "<<if $Trust gte 1>>" +
                   "<span style=\"color: #F4A6B8;\">" +
                   "I mean it.</span><</if>>\n" +
                   "[[Continue|Ending][$Seen to true]]\n\n" +
                   ":: Ending {\"position\":\"500,200\"}\n" +
                   "Narrator last.\n";
        }
    }
}
