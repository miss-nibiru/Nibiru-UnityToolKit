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
    }
}
