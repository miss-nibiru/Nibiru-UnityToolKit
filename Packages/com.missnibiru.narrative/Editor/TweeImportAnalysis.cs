using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MissNibiru.Narrative.Editor
{
    public sealed class TweeImportAnalysis
    {
        public TweeStoryData Story { get; internal set; }
        public string SourcePath { get; internal set; } = string.Empty;
        public int PassageCount { get; internal set; }
        public int DialogueLineCount { get; internal set; }
        public int NarratorLineCount { get; internal set; }
        public int CharacterLineCount { get; internal set; }
        public int MutationCount { get; internal set; }
        public int ChoiceCount { get; internal set; }
        public int AudioDefinitionCount { get; internal set; }
        public int AudioUsageCount { get; internal set; }
        public IReadOnlyList<string> DetectedColours { get; internal set; } =
            Array.Empty<string>();
        public IReadOnlyList<string> DetectedAudioKeys { get; internal set; } =
            Array.Empty<string>();
        public IReadOnlyList<TweeAudioDefinitionData> AudioDefinitions
            { get; internal set; } = Array.Empty<TweeAudioDefinitionData>();
        public IReadOnlyList<TweeImportIssue> Issues { get; internal set; } =
            Array.Empty<TweeImportIssue>();
    }

    public static class TweeImportAnalyzer
    {
        public static TweeImportAnalysis AnalyzeFile(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) ||
                !File.Exists(sourcePath))
            {
                return new TweeImportAnalysis
                {
                    SourcePath = sourcePath ?? string.Empty,
                    Issues = new[]
                    {
                        new TweeImportIssue(
                            TweeImportIssueSeverity.Error,
                            string.Empty,
                            "The selected Twee file does not exist.")
                    }
                };
            }

            return AnalyzeSource(File.ReadAllText(sourcePath), sourcePath);
        }

        public static TweeImportAnalysis AnalyzeSource(
            string source,
            string sourcePath = "")
        {
            TweeStoryData story = TweeParser.Parse(source);
            List<TweePassageData> passages = story.Passages
                .Where(passage => !passage.IsSpecial)
                .ToList();
            List<TweeTextSegmentData> lines = passages
                .SelectMany(passage => passage.TextSegments)
                .ToList();
            string[] colours = lines
                .Select(line => TweeImportProfile.NormalizeColour(line.Colour))
                .Where(colour => !string.IsNullOrWhiteSpace(colour))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(colour => colour, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] audioKeys = story.AudioDefinitions
                .Select(definition => definition.Key)
                .Concat(lines.Select(line => line.AudioKey))
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            TweeAudioDefinitionData[] audioDefinitions = story.AudioDefinitions
                .Where(definition =>
                    !string.IsNullOrWhiteSpace(definition.Key))
                .GroupBy(
                    definition => definition.Key,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(
                    definition => definition.Key,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new TweeImportAnalysis
            {
                Story = story,
                SourcePath = sourcePath ?? string.Empty,
                PassageCount = passages.Count,
                DialogueLineCount = lines.Count,
                NarratorLineCount = lines.Count(line =>
                    string.IsNullOrWhiteSpace(line.Colour)),
                CharacterLineCount = lines.Count(line =>
                    !string.IsNullOrWhiteSpace(line.Colour)),
                MutationCount = passages.Sum(passage =>
                    passage.Mutations.Count + passage.Links.Sum(link =>
                        link.Mutations.Count)),
                ChoiceCount = passages.Sum(passage => passage.Links.Count),
                AudioDefinitionCount = story.AudioDefinitions
                    .Where(definition =>
                        !string.IsNullOrWhiteSpace(definition.Key))
                    .Select(definition => definition.Key)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                AudioUsageCount = lines.Count(line =>
                    !string.IsNullOrWhiteSpace(line.AudioKey)),
                DetectedColours = colours,
                DetectedAudioKeys = audioKeys,
                AudioDefinitions = audioDefinitions,
                Issues = story.Issues.ToArray()
            };
        }
    }
}
