using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MissNibiru.Narrative;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Narrative.Editor
{
    public sealed class TweeImportResult
    {
        public NarrativeStory Story { get; internal set; }
        public string StoryPath { get; internal set; } = string.Empty;
        public string ReportPath { get; internal set; } = string.Empty;
        public int PassageCount { get; internal set; }
        public int VariableCount { get; internal set; }
        public int FlagCount { get; internal set; }
        public int NodeCount { get; internal set; }
        public int DialogueLineCount { get; internal set; }
        public int NarratorLineCount { get; internal set; }
        public int CharacterLineCount { get; internal set; }
        public int CharacterCount { get; internal set; }
        public int AudioDefinitionCount { get; internal set; }
        public int AudioUsageCount { get; internal set; }
        public int MappedAudioCount { get; internal set; }
        public int UnmappedAudioCount { get; internal set; }
        public int ChoiceNodeCount { get; internal set; }
        public int ConditionNodeCount { get; internal set; }
        public List<TweeImportIssue> Issues { get; } =
            new List<TweeImportIssue>();

        public int Count(TweeImportIssueSeverity severity)
        {
            return Issues.Count(issue => issue.Severity == severity);
        }
    }

    public static class TweeImportService
    {
        private sealed class SymbolInfo
        {
            public string Name = string.Empty;
            public NarrativeVariableType Type = NarrativeVariableType.Boolean;
            public string DefaultValue = string.Empty;
            public bool HasDefault;
        }

        private sealed class LinkBinding
        {
            public TweeLinkData Source;
            public NarrativeChoiceOption Option;
            public Vector2 Position;
        }

        private sealed class PassageBuild
        {
            public TweePassageData Source;
            public Vector2 Position;
            public NarrativeNode Entry;
            public readonly List<NarrativeLineNode> Lines =
                new List<NarrativeLineNode>();
            public readonly List<NarrativeChoiceNode> ChoicePages =
                new List<NarrativeChoiceNode>();
            public readonly List<LinkBinding> Links =
                new List<LinkBinding>();
        }

        private sealed class SpeakerResolution
        {
            public NarrativeCharacter Character;
            public NarrativeEmotion Emotion;
            public NarrativePortraitSide Side;
        }

        private sealed class PendingExit
        {
            public NarrativeNode Node;
            public int OutputIndex;
        }

        private sealed class StepBuild
        {
            public NarrativeNode Entry;
            public readonly List<PendingExit> Exits =
                new List<PendingExit>();
        }

        private static readonly Regex ConditionSymbolRegex = new Regex(
            "\\$(?<name>[A-Za-z_][A-Za-z0-9_]*)" +
            "(?:\\s*(?:is(?:\\s+not)?|eq|neq|gte|lte|gt|lt|" +
            ">=|<=|==|!=|>|<)\\s*" +
            "(?<value>\"(?:\\\\.|[^\"])*\"|" +
            "'(?:\\\\.|[^'])*'|-?\\d+(?:\\.\\d+)?|" +
            "true|false))?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static TweeImportResult ImportFile(
            string sourceFilePath,
            string requestedStoryPath)
        {
            return ImportFile(sourceFilePath, requestedStoryPath, null);
        }

        public static TweeImportResult ImportFile(
            string sourceFilePath,
            string requestedStoryPath,
            TweeImportProfile profile)
        {
            TweeImportResult result = new TweeImportResult();

            if (string.IsNullOrWhiteSpace(sourceFilePath) ||
                !File.Exists(sourceFilePath))
            {
                result.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Error,
                    string.Empty,
                    "The selected Twee file does not exist."));
                return result;
            }

            if (string.IsNullOrWhiteSpace(requestedStoryPath) ||
                !requestedStoryPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal))
            {
                result.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Error,
                    string.Empty,
                    "Choose a story location inside Assets."));
                return result;
            }

            try
            {
                EditorUtility.DisplayProgressBar(
                    "Import Twee",
                    "Reading passages...",
                    0.05f);
                string source = File.ReadAllText(sourceFilePath);
                TweeStoryData data = TweeParser.Parse(source);
                result.Issues.AddRange(data.Issues);

                if (data.Passages.All(passage => passage.IsSpecial))
                {
                    result.Issues.Add(new TweeImportIssue(
                        TweeImportIssueSeverity.Error,
                        string.Empty,
                        "The Twee file has no story passages."));
                    return result;
                }

                BuildAssets(data, requestedStoryPath, profile, result);
            }
            catch (Exception exception)
            {
                result.Story = null;
                result.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Error,
                    string.Empty,
                    "Import stopped safely: " + exception.Message));
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return result;
        }

        private static void BuildAssets(
            TweeStoryData data,
            string requestedStoryPath,
            TweeImportProfile profile,
            TweeImportResult result)
        {
            string storyPath = AssetDatabase.GenerateUniqueAssetPath(
                requestedStoryPath);
            string storyName = Path.GetFileNameWithoutExtension(storyPath);
            string parent = Path.GetDirectoryName(storyPath)
                ?.Replace('\\', '/') ?? "Assets";
            string dataFolder = EnsureUniqueFolder(
                parent,
                CleanFileName(storyName) + "_TweeData");
            string variablesFolder = EnsureFolder(dataFolder, "Variables");
            string flagsFolder = EnsureFolder(dataFolder, "Flags");
            string charactersFolder = EnsureFolder(dataFolder, "Characters");

            EditorUtility.DisplayProgressBar(
                "Import Twee",
                "Creating story and variables...",
                0.18f);
            NarrativeStory story = NarrativeAssetFactory.CreateStory(storyPath);
            story.Configure(CleanId(data.Title), data.Title,
                story.PresentationProfile);
            result.Story = story;
            result.StoryPath = AssetDatabase.GetAssetPath(story);

            Dictionary<string, SymbolInfo> symbols = DiscoverSymbols(data);
            Dictionary<string, NarrativeFlag> flags =
                new Dictionary<string, NarrativeFlag>(
                    StringComparer.OrdinalIgnoreCase);
            Dictionary<string, NarrativeVariable> variables =
                new Dictionary<string, NarrativeVariable>(
                    StringComparer.OrdinalIgnoreCase);
            CreateStateAssets(
                story,
                symbols,
                variablesFolder,
                flagsFolder,
                flags,
                variables);
            result.VariableCount = variables.Count;
            result.FlagCount = flags.Count;
            Dictionary<string, SpeakerResolution> speakers =
                ResolveSpeakers(
                    story,
                    data,
                    profile,
                    charactersFolder,
                    result);

            NarrativeStartNode start = story.FindNode(
                story.StartNodeId) as NarrativeStartNode;
            NarrativeEndNode fallback = story.Nodes
                .OfType<NarrativeEndNode>()
                .FirstOrDefault();

            if (start == null || fallback == null)
                throw new InvalidOperationException(
                    "The generated story is missing its required nodes.");

            fallback.name = "Imported Story End";
            fallback.Configure("import_complete", "Imported passage ended.");
            HashSet<string> nodeIds = new HashSet<string>(
                story.Nodes.Select(node => node.Id),
                StringComparer.Ordinal);
            List<TweePassageData> passages = data.Passages
                .Where(passage => !passage.IsSpecial)
                .ToList();
            Vector2 minimum = FindMinimum(passages);
            Dictionary<string, PassageBuild> builds =
                new Dictionary<string, PassageBuild>(
                    StringComparer.Ordinal);

            EditorUtility.DisplayProgressBar(
                "Import Twee",
                "Building passage nodes...",
                0.35f);

            for (int i = 0; i < passages.Count; i++)
            {
                TweePassageData passage = passages[i];

                if (builds.ContainsKey(passage.Name))
                {
                    result.Issues.Add(new TweeImportIssue(
                        TweeImportIssueSeverity.Error,
                        passage.Name,
                        "Duplicate passage name ignored."));
                    continue;
                }

                PassageBuild build = BuildPassage(
                    story,
                    passage,
                    MapPosition(passage.Position, minimum),
                    fallback,
                    flags,
                    variables,
                    speakers,
                    profile,
                    nodeIds,
                    result);
                builds.Add(passage.Name, build);
            }

            EditorUtility.DisplayProgressBar(
                "Import Twee",
                "Connecting passages...",
                0.72f);
            ConnectPassages(
                story,
                builds,
                fallback,
                flags,
                variables,
                nodeIds,
                result.Issues);

            if (builds.TryGetValue(
                    data.StartPassage, out PassageBuild startingPassage))
            {
                start.SetNextNodeId(startingPassage.Entry.Id);
                start.SetEditorPosition(
                    startingPassage.Position + new Vector2(-300f, 0f));
            }
            else
            {
                start.SetNextNodeId(fallback.Id);
                result.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Error,
                    "StoryData",
                    $"Start passage '{data.StartPassage}' was not found."));
            }

            result.PassageCount = passages.Count;
            result.NodeCount = story.Nodes.Count;
            result.AudioDefinitionCount = data.AudioDefinitions
                .Where(definition =>
                    !string.IsNullOrWhiteSpace(definition.Key))
                .Select(definition => definition.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            result.Issues.Add(new TweeImportIssue(
                TweeImportIssueSeverity.Information,
                "StoryCaption",
                "Twine HUD markup was not imported. Bind Unity UI to the generated variables instead."));
            if (result.UnmappedAudioCount > 0)
            {
                result.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Warning,
                    string.Empty,
                    $"{result.UnmappedAudioCount} audio uses have no mapped Unity clip."));
            }
            result.Issues.Add(new TweeImportIssue(
                TweeImportIssueSeverity.Information,
                string.Empty,
                "Browser scripts and styles remain outside the Unity narrative graph."));
            DeduplicateIssues(result.Issues);
            EditorUtility.SetDirty(start);
            EditorUtility.SetDirty(fallback);
            EditorUtility.SetDirty(story);
            AssetDatabase.SaveAssets();
            WriteReport(data, dataFolder, result);
            AssetDatabase.Refresh();
            Selection.activeObject = story;
        }

        private static PassageBuild BuildPassage(
            NarrativeStory story,
            TweePassageData passage,
            Vector2 position,
            NarrativeEndNode fallback,
            IReadOnlyDictionary<string, NarrativeFlag> flags,
            IReadOnlyDictionary<string, NarrativeVariable> variables,
            IReadOnlyDictionary<string, SpeakerResolution> speakers,
            TweeImportProfile profile,
            HashSet<string> nodeIds,
            TweeImportResult result)
        {
            PassageBuild build = new PassageBuild
            {
                Source = passage,
                Position = position
            };
            string passageId = UniqueNodeId(
                CleanId(passage.Name), nodeIds);
            List<PendingExit> pending = new List<PendingExit>();
            int builtStepCount = 0;

            foreach (TweePassageStepData sourceStep in passage.Steps
                         .OrderBy(step => step.Order))
            {
                StepBuild step = BuildOrderedStep(
                    story,
                    sourceStep,
                    position + new Vector2(builtStepCount * 275f, 0f),
                    passageId + "_step_" + (builtStepCount + 1),
                    passage.Name,
                    flags,
                    variables,
                    speakers,
                    profile,
                    nodeIds,
                    result,
                    build);

                if (step == null || step.Entry == null)
                    continue;

                if (build.Entry == null)
                    build.Entry = step.Entry;

                ConnectExits(pending, step.Entry.Id);
                pending = step.Exits;
                builtStepCount++;
            }

            BuildChoicePages(
                story,
                passage,
                build,
                passageId,
                builtStepCount,
                flags,
                variables,
                nodeIds,
                result.Issues);
            result.ChoiceNodeCount += build.ChoicePages.Count;

            NarrativeNode destination = build.ChoicePages.Count > 0
                ? build.ChoicePages[0]
                : fallback;
            ConnectExits(pending, destination.Id);

            if (build.Entry == null)
                build.Entry = destination;

            return build;
        }

        private static StepBuild BuildOrderedStep(
            NarrativeStory story,
            TweePassageStepData sourceStep,
            Vector2 position,
            string idPrefix,
            string passage,
            IReadOnlyDictionary<string, NarrativeFlag> flags,
            IReadOnlyDictionary<string, NarrativeVariable> variables,
            IReadOnlyDictionary<string, SpeakerResolution> speakers,
            TweeImportProfile profile,
            HashSet<string> nodeIds,
            TweeImportResult result,
            PassageBuild passageBuild)
        {
            NarrativeNode action;
            string condition;

            if (sourceStep.Kind == TweePassageStepKind.Dialogue)
            {
                TweeTextSegmentData source = sourceStep.Dialogue;

                if (source == null ||
                    (profile != null && !profile.IncludeNarration &&
                     string.IsNullOrWhiteSpace(source.Colour)))
                {
                    return null;
                }

                NarrativeLineNode line =
                    NarrativeAssetFactory.AddNode<NarrativeLineNode>(
                        story, position, false);
                SetNodeIdentity(
                    line,
                    idPrefix + "_line",
                    string.IsNullOrWhiteSpace(source.Colour)
                        ? passage + " Narrator"
                        : passage + " Dialogue",
                    position,
                    nodeIds);
                line.ConfigureImportedText(
                    source.Text,
                    Array.Empty<NarrativeTextSegment>());
                int configuredLimit = profile == null
                    ? story.DefaultLineWordLimit
                    : profile.DefaultWordLimit;
                line.ConfigureWordLimit(configuredLimit);
                ConfigureLineSpeaker(line, source, speakers);
                ConfigureLineAudio(line, source.AudioKey, profile, result);
                EditorUtility.SetDirty(line);
                passageBuild.Lines.Add(line);
                result.DialogueLineCount++;

                if (line.Character == null)
                    result.NarratorLineCount++;
                else
                    result.CharacterLineCount++;

                action = line;
                condition = source.Condition;
            }
            else
            {
                TweeMutationData source = sourceStep.Mutation;

                if (source == null)
                    return null;

                action = CreateMutationNode(
                    story,
                    source,
                    position,
                    idPrefix,
                    passage,
                    flags,
                    variables,
                    nodeIds,
                    result.Issues);
                condition = source.Condition;

                if (action == null)
                    return null;
            }

            StepBuild step = new StepBuild();

            if (string.IsNullOrWhiteSpace(condition))
            {
                step.Entry = action;
                step.Exits.Add(new PendingExit
                {
                    Node = action,
                    OutputIndex = 0
                });
                return step;
            }

            NarrativeConditionNode conditionNode =
                NarrativeAssetFactory.AddNode<NarrativeConditionNode>(
                    story, position + new Vector2(-205f, -45f), false);
            SetNodeIdentity(
                conditionNode,
                idPrefix + "_condition",
                passage + " Condition",
                conditionNode.EditorPosition,
                nodeIds);
            conditionNode.ConfigureImportedCondition(
                TweeConditionCompiler.Compile(
                    condition,
                    flags,
                    variables,
                    result.Issues,
                    passage));
            conditionNode.SetTrueNodeId(action.Id);
            EditorUtility.SetDirty(conditionNode);
            result.ConditionNodeCount++;
            step.Entry = conditionNode;
            step.Exits.Add(new PendingExit
            {
                Node = action,
                OutputIndex = 0
            });
            step.Exits.Add(new PendingExit
            {
                Node = conditionNode,
                OutputIndex = 1
            });
            return step;
        }

        private static void ConnectExits(
            IEnumerable<PendingExit> exits,
            string targetId)
        {
            foreach (PendingExit exit in exits)
            {
                if (exit?.Node == null)
                    continue;

                NarrativeNodeConnectionUtility.SetTarget(
                    exit.Node, exit.OutputIndex, targetId);
                EditorUtility.SetDirty(exit.Node);
            }
        }

        private static void BuildChoicePages(
            NarrativeStory story,
            TweePassageData passage,
            PassageBuild build,
            string passageId,
            int contentStepCount,
            IReadOnlyDictionary<string, NarrativeFlag> flags,
            IReadOnlyDictionary<string, NarrativeVariable> variables,
            HashSet<string> nodeIds,
            List<TweeImportIssue> issues)
        {
            if (passage.Links.Count == 0)
                return;

            int sourceIndex = 0;
            int pageIndex = 0;

            while (sourceIndex < passage.Links.Count)
            {
                int remaining = passage.Links.Count - sourceIndex;
                int capacity;

                if (pageIndex == 0)
                    capacity = remaining > 5 ? 4 : 5;
                else
                    capacity = remaining > 4 ? 3 : 4;

                int count = Math.Min(capacity, remaining);
                List<NarrativeChoiceOption> options =
                    new List<NarrativeChoiceOption>();
                List<LinkBinding> pageBindings = new List<LinkBinding>();

                for (int i = 0; i < count; i++)
                {
                    TweeLinkData link = passage.Links[sourceIndex + i];
                    NarrativeChoiceOption option =
                        new NarrativeChoiceOption();
                    option.Configure(
                        link.Text,
                        Math.Max(12, NarrativeValidator.CountWords(
                            link.Text)),
                        string.Empty,
                        TweeConditionCompiler.Compile(
                            link.Condition,
                            flags,
                            variables,
                            issues,
                            passage.Name));
                    options.Add(option);
                    pageBindings.Add(new LinkBinding
                    {
                        Source = link,
                        Option = option,
                        Position = build.Position + new Vector2(
                            600f + pageIndex * 240f,
                            (sourceIndex + i) * 75f)
                    });
                }

                bool hasPrevious = pageIndex > 0;
                bool hasMore = sourceIndex + count < passage.Links.Count;

                if (hasPrevious)
                {
                    NarrativeChoiceOption back = new NarrativeChoiceOption();
                    back.Configure("Back", 4, string.Empty);
                    options.Add(back);
                }

                if (hasMore)
                {
                    NarrativeChoiceOption more = new NarrativeChoiceOption();
                    more.Configure("More…", 4, string.Empty);
                    options.Add(more);
                }

                NarrativeChoiceNode page =
                    NarrativeAssetFactory.AddNode<NarrativeChoiceNode>(
                        story,
                        build.Position + new Vector2(
                            Math.Max(1, contentStepCount) * 275f +
                            pageIndex * 260f,
                            170f * pageIndex),
                        false);
                SetNodeIdentity(
                    page,
                    passageId + "_choices_" + (pageIndex + 1),
                    passage.Name + " Choices " + (pageIndex + 1),
                    page.EditorPosition,
                    nodeIds);
                page.Configure("Choose a response.", options.ToArray());
                build.ChoicePages.Add(page);
                build.Links.AddRange(pageBindings);
                sourceIndex += count;
                pageIndex++;
            }

            for (int i = 0; i < build.ChoicePages.Count; i++)
            {
                NarrativeChoiceNode page = build.ChoicePages[i];
                int originalCount = page.Choices.Count(option =>
                    option.Text != "Back" && option.Text != "More…");
                int navigationIndex = originalCount;

                if (i > 0)
                {
                    page.GetChoice(navigationIndex++)?.SetTargetNodeId(
                        build.ChoicePages[i - 1].Id);
                }

                if (i < build.ChoicePages.Count - 1)
                {
                    page.GetChoice(navigationIndex)?.SetTargetNodeId(
                        build.ChoicePages[i + 1].Id);
                }
            }

            if (build.ChoicePages.Count > 1)
            {
                issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Information,
                    passage.Name,
                    $"{passage.Links.Count} links were divided into " +
                    $"{build.ChoicePages.Count} five-choice pages."));
            }
        }

        private static void ConnectPassages(
            NarrativeStory story,
            IReadOnlyDictionary<string, PassageBuild> builds,
            NarrativeEndNode fallback,
            IReadOnlyDictionary<string, NarrativeFlag> flags,
            IReadOnlyDictionary<string, NarrativeVariable> variables,
            HashSet<string> nodeIds,
            List<TweeImportIssue> issues)
        {
            foreach (PassageBuild build in builds.Values)
            {
                for (int i = 0; i < build.Links.Count; i++)
                {
                    LinkBinding binding = build.Links[i];
                    string targetId;

                    if (builds.TryGetValue(
                            binding.Source.Target,
                            out PassageBuild target))
                    {
                        targetId = target.Entry.Id;
                    }
                    else
                    {
                        targetId = fallback.Id;
                        issues.Add(new TweeImportIssue(
                            TweeImportIssueSeverity.Error,
                            build.Source.Name,
                            $"Link target '{binding.Source.Target}' " +
                            "does not exist; it was connected to End."));
                    }

                    List<NarrativeNode> mutations = CreateMutationNodes(
                        story,
                        binding.Source.Mutations,
                        binding.Position,
                        CleanId(build.Source.Name) + "_choice_" + i,
                        build.Source.Name,
                        flags,
                        variables,
                        nodeIds,
                        issues);
                    ConnectLinear(mutations, targetId);
                    binding.Option.SetTargetNodeId(
                        mutations.Count > 0
                            ? mutations[0].Id
                            : targetId);
                }
            }
        }

        private static List<NarrativeNode> CreateMutationNodes(
            NarrativeStory story,
            IReadOnlyList<TweeMutationData> mutations,
            Vector2 position,
            string idPrefix,
            string passage,
            IReadOnlyDictionary<string, NarrativeFlag> flags,
            IReadOnlyDictionary<string, NarrativeVariable> variables,
            HashSet<string> nodeIds,
            List<TweeImportIssue> issues)
        {
            List<NarrativeNode> result = new List<NarrativeNode>();

            for (int i = 0; i < mutations.Count; i++)
            {
                TweeMutationData source = mutations[i];
                Vector2 nodePosition = position + new Vector2(i * 225f, 0f);
                NarrativeNode node = CreateMutationNode(
                    story,
                    source,
                    nodePosition,
                    idPrefix + "_" + (i + 1),
                    passage,
                    flags,
                    variables,
                    nodeIds,
                    issues);

                if (node != null)
                    result.Add(node);
            }

            return result;
        }

        private static NarrativeNode CreateMutationNode(
            NarrativeStory story,
            TweeMutationData source,
            Vector2 position,
            string idPrefix,
            string passage,
            IReadOnlyDictionary<string, NarrativeFlag> flags,
            IReadOnlyDictionary<string, NarrativeVariable> variables,
            HashSet<string> nodeIds,
            List<TweeImportIssue> issues)
        {
            if (source.IsRandom)
            {
                if (!variables.TryGetValue(
                        source.VariableName,
                        out NarrativeVariable randomVariable))
                {
                    issues.Add(new TweeImportIssue(
                        TweeImportIssueSeverity.Error,
                        passage,
                        $"Random assignment references unknown variable " +
                        $"${source.VariableName}."));
                    return null;
                }

                NarrativeRandomValueNode random =
                    NarrativeAssetFactory.AddNode<NarrativeRandomValueNode>(
                        story, position, false);
                SetNodeIdentity(
                    random,
                    idPrefix + "_random",
                    "Random " + source.VariableName,
                    position,
                    nodeIds);
                random.Configure(
                    randomVariable,
                    source.RandomMinimum,
                    source.RandomMaximum);
                EditorUtility.SetDirty(random);
                return random;
            }

            NarrativeSetValueNode node =
                NarrativeAssetFactory.AddNode<NarrativeSetValueNode>(
                    story, position, false);
            SetNodeIdentity(
                node,
                idPrefix + "_set",
                "Set " + source.VariableName,
                position,
                nodeIds);
            NarrativeMutation operation = ToMutation(source.Operator);

            if (flags.TryGetValue(source.VariableName, out NarrativeFlag flag))
            {
                node.ConfigureFlag(
                    flag,
                    operation,
                    ParseBoolean(source.RawValue));
                EditorUtility.SetDirty(node);
                return node;
            }

            if (variables.TryGetValue(
                    source.VariableName,
                    out NarrativeVariable variable))
            {
                node.ConfigureVariable(
                    variable,
                    operation,
                    ParseBoolean(source.RawValue),
                    ParseInteger(source.RawValue),
                    ParseFloat(source.RawValue),
                    Unquote(source.RawValue));
                EditorUtility.SetDirty(node);
                return node;
            }

            UnityEngine.Object.DestroyImmediate(node, true);
            issues.Add(new TweeImportIssue(
                TweeImportIssueSeverity.Error,
                passage,
                $"Assignment references unknown variable " +
                $"${source.VariableName}."));
            return null;
        }

        private static Dictionary<string, SpeakerResolution> ResolveSpeakers(
            NarrativeStory story,
            TweeStoryData data,
            TweeImportProfile profile,
            string charactersFolder,
            TweeImportResult result)
        {
            Dictionary<string, SpeakerResolution> resolved =
                new Dictionary<string, SpeakerResolution>(
                    StringComparer.OrdinalIgnoreCase);
            Dictionary<TweeSpeakerMapping, NarrativeCharacter> created =
                new Dictionary<TweeSpeakerMapping, NarrativeCharacter>();
            HashSet<NarrativeCharacter> used =
                new HashSet<NarrativeCharacter>();
            bool createPlaceholders = profile == null ||
                                      profile.CreatePlaceholderCharacters;
            string[] colours = data.Passages
                .Where(passage => !passage.IsSpecial)
                .SelectMany(passage => passage.TextSegments)
                .Select(segment =>
                    TweeImportProfile.NormalizeColour(segment.Colour))
                .Where(colour => !string.IsNullOrWhiteSpace(colour))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string colour in colours)
            {
                TweeSpeakerMapping mapping = profile?.FindSpeaker(colour);
                NarrativeCharacter character = mapping?.Character;

                if (character == null && mapping != null &&
                    created.TryGetValue(mapping, out NarrativeCharacter shared))
                {
                    character = shared;
                }

                if (character == null && createPlaceholders)
                {
                    string visibleName = mapping == null ||
                                         string.IsNullOrWhiteSpace(
                                             mapping.DisplayName)
                        ? "Speaker " + colour.TrimStart('#')
                        : mapping.DisplayName;
                    character = NarrativeAssetFactory.CreateLibraryAsset<
                        NarrativeCharacter>(
                        charactersFolder + "/" +
                        CleanFileName(visibleName) + ".asset",
                        story);
                    character.Configure(
                        CleanId(visibleName),
                        visibleName,
                        ParseColour(colour));
                    EditorUtility.SetDirty(character);

                    if (mapping != null)
                    {
                        created[mapping] = character;
                        mapping.SetCharacter(character);

                        if (profile != null)
                            EditorUtility.SetDirty(profile);
                    }
                }
                else if (character != null)
                {
                    NarrativeAssetFactory.RegisterWithStory(story, character);
                }

                if (character == null)
                {
                    result.Issues.Add(new TweeImportIssue(
                        TweeImportIssueSeverity.Warning,
                        string.Empty,
                        $"Colour {colour} has no character mapping."));
                }
                else
                {
                    used.Add(character);
                }

                resolved[colour] = new SpeakerResolution
                {
                    Character = character,
                    Emotion = mapping?.Emotion,
                    Side = mapping?.PortraitSide ?? NarrativePortraitSide.Left
                };
            }

            result.CharacterCount = used.Count;
            return resolved;
        }

        private static void ConfigureLineSpeaker(
            NarrativeLineNode line,
            TweeTextSegmentData source,
            IReadOnlyDictionary<string, SpeakerResolution> speakers)
        {
            string colour = TweeImportProfile.NormalizeColour(source.Colour);

            if (string.IsNullOrWhiteSpace(colour))
            {
                line.ConfigureSpeaker(null, null, NarrativePortraitSide.Left);
                return;
            }

            if (speakers.TryGetValue(colour, out SpeakerResolution speaker))
            {
                line.ConfigureSpeaker(
                    speaker.Character,
                    speaker.Emotion,
                    speaker.Side);
            }
        }

        private static void ConfigureLineAudio(
            NarrativeLineNode line,
            string key,
            TweeImportProfile profile,
            TweeImportResult result)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            result.AudioUsageCount++;
            TweeAudioMapping mapping = profile?.FindAudio(key);

            if (mapping?.Clip == null)
            {
                result.UnmappedAudioCount++;
                return;
            }

            AudioClip music = null;
            AudioClip voice = null;
            AudioClip effect = null;

            switch (mapping.Role)
            {
                case TweeAudioRole.Music:
                    music = mapping.Clip;
                    break;
                case TweeAudioRole.SoundEffect:
                    effect = mapping.Clip;
                    break;
                default:
                    voice = mapping.Clip;
                    break;
            }

            line.ConfigureAudio(music, voice, effect);
            result.MappedAudioCount++;
        }

        private static Color ParseColour(string colour)
        {
            return ColorUtility.TryParseHtmlString(colour, out Color parsed)
                ? parsed
                : Color.white;
        }

        private static void ConnectLinear(
            IReadOnlyList<NarrativeNode> nodes,
            string finalTarget)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                string target = i + 1 < nodes.Count
                    ? nodes[i + 1].Id
                    : finalTarget;
                NarrativeNodeConnectionUtility.SetTarget(nodes[i], 0, target);
                EditorUtility.SetDirty(nodes[i]);
            }
        }

        private static Dictionary<string, SymbolInfo> DiscoverSymbols(
            TweeStoryData data)
        {
            Dictionary<string, SymbolInfo> symbols =
                new Dictionary<string, SymbolInfo>(
                    StringComparer.OrdinalIgnoreCase);
            TweePassageData initialization = data.FindPassage("StoryInit");

            foreach (TweePassageData passage in data.Passages)
            {
                foreach (TweeMutationData mutation in passage.Mutations)
                {
                    AddMutationSymbol(
                        symbols,
                        mutation,
                        passage == initialization);
                    AddConditionSymbols(symbols, mutation.Condition);
                }

                foreach (TweeLinkData link in passage.Links)
                {
                    foreach (TweeMutationData mutation in link.Mutations)
                        AddMutationSymbol(symbols, mutation, false);

                    AddConditionSymbols(symbols, link.Condition);
                }

                foreach (TweeTextSegmentData segment in passage.TextSegments)
                    AddConditionSymbols(symbols, segment.Condition);
            }

            return symbols;
        }

        private static void AddMutationSymbol(
            IDictionary<string, SymbolInfo> symbols,
            TweeMutationData mutation,
            bool isDefault)
        {
            NarrativeVariableType type = InferType(mutation);

            if (!symbols.TryGetValue(
                    mutation.VariableName, out SymbolInfo symbol))
            {
                symbol = new SymbolInfo
                {
                    Name = mutation.VariableName,
                    Type = type
                };
                symbols.Add(mutation.VariableName, symbol);
            }
            else if (symbol.Type == NarrativeVariableType.Boolean &&
                     type != NarrativeVariableType.Boolean)
            {
                symbol.Type = type;
            }

            if (isDefault)
            {
                symbol.DefaultValue = mutation.RawValue;
                symbol.HasDefault = true;
            }
        }

        private static void AddConditionSymbols(
            IDictionary<string, SymbolInfo> symbols,
            string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return;

            foreach (Match match in ConditionSymbolRegex.Matches(condition))
            {
                string name = match.Groups["name"].Value;
                string value = match.Groups["value"].Value;
                NarrativeVariableType type = string.IsNullOrWhiteSpace(value)
                    ? NarrativeVariableType.Boolean
                    : InferType(value);

                if (!symbols.TryGetValue(name, out SymbolInfo symbol))
                {
                    symbols.Add(name, new SymbolInfo
                    {
                        Name = name,
                        Type = type
                    });
                }
                else if (symbol.Type == NarrativeVariableType.Boolean &&
                         type != NarrativeVariableType.Boolean)
                {
                    symbol.Type = type;
                }
            }
        }

        private static void CreateStateAssets(
            NarrativeStory story,
            IReadOnlyDictionary<string, SymbolInfo> symbols,
            string variablesFolder,
            string flagsFolder,
            IDictionary<string, NarrativeFlag> flags,
            IDictionary<string, NarrativeVariable> variables)
        {
            foreach (SymbolInfo symbol in symbols.Values.OrderBy(
                         value => value.Name,
                         StringComparer.OrdinalIgnoreCase))
            {
                string fileName = CleanFileName(symbol.Name) + ".asset";

                if (symbol.Type == NarrativeVariableType.Boolean)
                {
                    NarrativeFlag flag =
                        NarrativeAssetFactory.CreateLibraryAsset<
                            NarrativeFlag>(
                            flagsFolder + "/" + fileName,
                            story);
                    flag.Configure(
                        CleanId(symbol.Name),
                        ObjectNames.NicifyVariableName(symbol.Name),
                        symbol.HasDefault &&
                        ParseBoolean(symbol.DefaultValue));
                    flags.Add(symbol.Name, flag);
                    EditorUtility.SetDirty(flag);
                    continue;
                }

                NarrativeVariable variable =
                    NarrativeAssetFactory.CreateLibraryAsset<
                        NarrativeVariable>(
                        variablesFolder + "/" + fileName,
                        story);
                variable.Configure(
                    CleanId(symbol.Name),
                    ObjectNames.NicifyVariableName(symbol.Name),
                    symbol.Type);

                if (symbol.HasDefault)
                {
                    switch (symbol.Type)
                    {
                        case NarrativeVariableType.Integer:
                            variable.SetDefault(ParseInteger(
                                symbol.DefaultValue));
                            break;
                        case NarrativeVariableType.Float:
                            variable.SetDefault(ParseFloat(
                                symbol.DefaultValue));
                            break;
                        case NarrativeVariableType.String:
                            variable.SetDefault(Unquote(
                                symbol.DefaultValue));
                            break;
                    }
                }

                variables.Add(symbol.Name, variable);
                EditorUtility.SetDirty(variable);
            }
        }

        private static void WriteReport(
            TweeStoryData data,
            string dataFolder,
            TweeImportResult result)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("MISS NIBIRU TWEE IMPORT REPORT");
            report.AppendLine();
            report.AppendLine("Story: " + data.Title);
            report.AppendLine(
                "Format: " + data.Format + " " + data.FormatVersion);
            report.AppendLine("Start: " + data.StartPassage);
            report.AppendLine("Passages: " + result.PassageCount);
            report.AppendLine(
                "Dialogue lines: " + result.DialogueLineCount);
            report.AppendLine(
                "Narrator lines: " + result.NarratorLineCount);
            report.AppendLine(
                "Character lines: " + result.CharacterLineCount);
            report.AppendLine("Characters: " + result.CharacterCount);
            report.AppendLine("Variables: " + result.VariableCount);
            report.AppendLine("Flags: " + result.FlagCount);
            report.AppendLine(
                "Audio definitions: " + result.AudioDefinitionCount);
            report.AppendLine(
                "Audio uses: " + result.AudioUsageCount);
            report.AppendLine(
                "Mapped audio: " + result.MappedAudioCount);
            report.AppendLine(
                "Unmapped audio: " + result.UnmappedAudioCount);
            report.AppendLine(
                "Choice nodes: " + result.ChoiceNodeCount);
            report.AppendLine(
                "Condition nodes: " + result.ConditionNodeCount);
            report.AppendLine("Generated nodes: " + result.NodeCount);
            report.AppendLine(
                "Errors: " + result.Count(TweeImportIssueSeverity.Error));
            report.AppendLine(
                "Warnings: " + result.Count(TweeImportIssueSeverity.Warning));
            report.AppendLine();
            report.AppendLine("Review every warning before shipping.");
            report.AppendLine();

            foreach (TweeImportIssue issue in result.Issues)
                report.AppendLine(issue.ToString());

            string reportPath = AssetDatabase.GenerateUniqueAssetPath(
                dataFolder + "/TweeImportReport.txt");
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.ImportAsset(reportPath);
            result.ReportPath = reportPath;
        }

        private static void SetNodeIdentity(
            NarrativeNode node,
            string requestedId,
            string visibleName,
            Vector2 position,
            HashSet<string> nodeIds)
        {
            string id = UniqueNodeId(requestedId, nodeIds);
            node.Initialize(id, position);
            node.name = visibleName;
            EditorUtility.SetDirty(node);
        }

        private static string UniqueNodeId(
            string requested,
            HashSet<string> existing)
        {
            string root = string.IsNullOrWhiteSpace(requested)
                ? "passage"
                : requested;
            string candidate = root;
            int suffix = 2;

            while (!existing.Add(candidate))
                candidate = root + "_" + suffix++;

            return candidate;
        }

        private static Vector2 FindMinimum(
            IReadOnlyList<TweePassageData> passages)
        {
            float x = passages.Min(passage => passage.Position.x);
            float y = passages.Min(passage => passage.Position.y);
            return new Vector2(x, y);
        }

        private static Vector2 MapPosition(Vector2 source, Vector2 minimum)
        {
            return (source - minimum) * 2.2f + new Vector2(360f, 180f);
        }

        private static NarrativeMutation ToMutation(string value)
        {
            if (value == "+=")
                return NarrativeMutation.Add;
            if (value == "-=")
                return NarrativeMutation.Subtract;
            return NarrativeMutation.Set;
        }

        private static NarrativeVariableType InferType(
            TweeMutationData mutation)
        {
            return mutation.IsRandom
                ? NarrativeVariableType.Integer
                : InferType(mutation.RawValue);
        }

        private static NarrativeVariableType InferType(string rawValue)
        {
            string value = (rawValue ?? string.Empty).Trim();

            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return NarrativeVariableType.Boolean;
            }

            if ((value.StartsWith("\"", StringComparison.Ordinal) &&
                 value.EndsWith("\"", StringComparison.Ordinal)) ||
                (value.StartsWith("'", StringComparison.Ordinal) &&
                 value.EndsWith("'", StringComparison.Ordinal)))
            {
                return NarrativeVariableType.String;
            }

            if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                return NarrativeVariableType.Integer;
            }

            return float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out _)
                ? NarrativeVariableType.Float
                : NarrativeVariableType.String;
        }

        private static bool ParseBoolean(string value)
        {
            bool.TryParse(Unquote(value), out bool result);
            return result;
        }

        private static int ParseInteger(string value)
        {
            int.TryParse(
                Unquote(value),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int result);
            return result;
        }

        private static float ParseFloat(string value)
        {
            float.TryParse(
                Unquote(value),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result);
            return result;
        }

        private static string Unquote(string value)
        {
            value = (value ?? string.Empty).Trim();

            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }

        private static string EnsureUniqueFolder(
            string parent,
            string requestedName)
        {
            string name = requestedName;
            int suffix = 2;

            while (AssetDatabase.IsValidFolder(parent + "/" + name))
                name = requestedName + "_" + suffix++;

            AssetDatabase.CreateFolder(parent, name);
            return parent + "/" + name;
        }

        private static string EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;

            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);

            return path;
        }

        private static string CleanId(string value)
        {
            string clean = Regex.Replace(
                (value ?? string.Empty).Trim().ToLowerInvariant(),
                @"[^a-z0-9_]+",
                "_");
            clean = clean.Trim('_');
            return string.IsNullOrWhiteSpace(clean) ? "story" : clean;
        }

        private static string CleanFileName(string value)
        {
            string clean = Regex.Replace(
                value ?? string.Empty,
                @"[^A-Za-z0-9_-]+",
                "_");
            return string.IsNullOrWhiteSpace(clean) ? "Imported" : clean;
        }

        private static void DeduplicateIssues(List<TweeImportIssue> issues)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = issues.Count - 1; i >= 0; i--)
            {
                string key = issues[i].ToString();

                if (!seen.Add(key))
                    issues.RemoveAt(i);
            }
        }
    }
}
