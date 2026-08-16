using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MissNibiru.Narrative.Editor
{
    public enum TweeImportIssueSeverity
    {
        Information,
        Warning,
        Error
    }

    public sealed class TweeImportIssue
    {
        public TweeImportIssueSeverity Severity { get; }
        public string Passage { get; }
        public string Message { get; }

        public TweeImportIssue(
            TweeImportIssueSeverity severity,
            string passage,
            string message)
        {
            Severity = severity;
            Passage = passage ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            string location = string.IsNullOrWhiteSpace(Passage)
                ? string.Empty
                : $" [{Passage}]";
            return $"{Severity}{location}: {Message}";
        }
    }

    public sealed class TweeMutationData
    {
        public string VariableName { get; set; } = string.Empty;
        public string Operator { get; set; } = "=";
        public string RawValue { get; set; } = string.Empty;
        public bool IsRandom { get; set; }
        public int RandomMinimum { get; set; }
        public int RandomMaximum { get; set; }
    }

    public sealed class TweeLinkData
    {
        public string Text { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public List<TweeMutationData> Mutations { get; } =
            new List<TweeMutationData>();
    }

    public sealed class TweeTextSegmentData
    {
        public string Text { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
    }

    public sealed class TweePassageData
    {
        public string Name { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public Vector2 Position { get; set; }
        public bool HasPosition { get; set; }
        public bool IsSpecial { get; set; }
        public List<TweeTextSegmentData> TextSegments { get; } =
            new List<TweeTextSegmentData>();
        public List<TweeMutationData> Mutations { get; } =
            new List<TweeMutationData>();
        public List<TweeLinkData> Links { get; } =
            new List<TweeLinkData>();
    }

    public sealed class TweeStoryData
    {
        public string Title { get; set; } = "Imported Story";
        public string Format { get; set; } = string.Empty;
        public string FormatVersion { get; set; } = string.Empty;
        public string StartPassage { get; set; } = string.Empty;
        public List<TweePassageData> Passages { get; } =
            new List<TweePassageData>();
        public List<TweeImportIssue> Issues { get; } =
            new List<TweeImportIssue>();

        public TweePassageData FindPassage(string name)
        {
            return Passages.Find(passage =>
                string.Equals(
                    passage.Name,
                    name,
                    StringComparison.Ordinal));
        }
    }

    public static class TweeParser
    {
        [Serializable]
        private sealed class StoryDataJson
        {
            public string format;
            public string formatVersion;
            public string start;
        }

        [Serializable]
        private sealed class PassageMetadataJson
        {
            public string position;
        }

        private sealed class ConditionFrame
        {
            public readonly List<string> PreviousBranches =
                new List<string>();
            public string Current = string.Empty;
        }

        private sealed class MacroLinkContext
        {
            public string Text = string.Empty;
            public string Target = string.Empty;
            public string Condition = string.Empty;
            public readonly List<TweeMutationData> Mutations =
                new List<TweeMutationData>();
            public bool ContainsAudio;
        }

        private static readonly Regex PassageHeaderRegex = new Regex(
            @"(?m)^::\s*(?<header>[^\r\n]+)\r?\n",
            RegexOptions.Compiled);

        private static readonly Regex TokenRegex = new Regex(
            @"(?s)(\[\[.*?\]\]|<<.*?>>)",
            RegexOptions.Compiled);

        private static readonly Regex AssignmentRegex = new Regex(
            @"^\$(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*" +
            @"(?<operator>\+=|-=|=|\bto\b)\s*(?<value>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RandomRegex = new Regex(
            @"^random\s*\(\s*(?<minimum>-?\d+)\s*,\s*" +
            @"(?<maximum>-?\d+)\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex LinkMacroLabelRegex = new Regex(
            "^link\\s+(?:\"(?<double>.*?)\"|'(?<single>.*?)')",
            RegexOptions.Compiled | RegexOptions.IgnoreCase |
            RegexOptions.Singleline);

        private static readonly Regex GotoRegex = new Regex(
            "^goto\\s+(?:\"(?<double>.*?)\"|'(?<single>.*?)')",
            RegexOptions.Compiled | RegexOptions.IgnoreCase |
            RegexOptions.Singleline);

        private static readonly Regex ColourElementRegex = new Regex(
            "(?is)<(?<tag>span|p)\\b[^>]*style\\s*=\\s*" +
            "[\"'](?<style>[^\"']*color\\s*:\\s*" +
            "(?<colour>#[0-9a-f]{6,8})[^\"']*)[\"'][^>]*>" +
            "(?<body>.*?)</\\k<tag>>",
            RegexOptions.Compiled);

        private static readonly Regex RemainingTagRegex = new Regex(
            @"(?is)</?(?!i\b|b\b|color\b)[a-z][^>]*>",
            RegexOptions.Compiled);

        public static TweeStoryData Parse(string source)
        {
            TweeStoryData story = new TweeStoryData();

            if (string.IsNullOrWhiteSpace(source))
            {
                story.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Error,
                    string.Empty,
                    "The Twee file is empty."));
                return story;
            }

            MatchCollection headers = PassageHeaderRegex.Matches(source);

            for (int i = 0; i < headers.Count; i++)
            {
                Match match = headers[i];
                int bodyStart = match.Index + match.Length;
                int bodyEnd = i + 1 < headers.Count
                    ? headers[i + 1].Index
                    : source.Length;
                string header = match.Groups["header"].Value.Trim();
                string body = source.Substring(
                    bodyStart, bodyEnd - bodyStart).Trim();
                TweePassageData passage = ParseHeader(header, i);

                if (passage.Name == "StoryTitle")
                {
                    story.Title = body.Trim();
                    passage.IsSpecial = true;
                }
                else if (passage.Name == "StoryData")
                {
                    ReadStoryData(body, story);
                    passage.IsSpecial = true;
                }

                ParsePassageBody(passage, body, story.Issues);
                story.Passages.Add(passage);
            }

            if (headers.Count == 0)
            {
                story.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Error,
                    string.Empty,
                    "No Twee passages were found."));
            }

            if (!string.IsNullOrWhiteSpace(story.Format) &&
                !story.Format.Equals(
                    "SugarCube",
                    StringComparison.OrdinalIgnoreCase))
            {
                story.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Warning,
                    "StoryData",
                    $"{story.Format} syntax is not fully supported. " +
                    "This importer targets SugarCube."));
            }

            if (string.IsNullOrWhiteSpace(story.StartPassage))
            {
                TweePassageData first = story.Passages.Find(
                    passage => !passage.IsSpecial);
                story.StartPassage = first?.Name ?? string.Empty;
                story.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Warning,
                    "StoryData",
                    "No start passage was declared; the first passage was used."));
            }

            return story;
        }

        public static string ConvertToUnityRichText(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;

            string result = source.Replace("\r\n", "\n");
            string previous;

            do
            {
                previous = result;
                result = ColourElementRegex.Replace(
                    result,
                    match =>
                        $"<color={match.Groups["colour"].Value}>" +
                        match.Groups["body"].Value + "</color>");
            }
            while (result != previous);

            result = Regex.Replace(
                result,
                @"(?is)<br\s*/?>|</p\s*>|</div\s*>",
                "\n");
            result = RemainingTagRegex.Replace(result, string.Empty);
            result = WebUtility.HtmlDecode(result);
            result = Regex.Replace(result, @"[ \t]+", " ");
            result = Regex.Replace(result, @" *\n *", "\n");
            result = Regex.Replace(result, @"\n{3,}", "\n\n");
            return result.Trim();
        }

        private static TweePassageData ParseHeader(
            string header,
            int index)
        {
            TweePassageData passage = new TweePassageData();
            string metadata = string.Empty;
            int metadataStart = header.LastIndexOf(" {", StringComparison.Ordinal);

            if (metadataStart >= 0)
            {
                metadata = header.Substring(metadataStart + 1);
                header = header.Substring(0, metadataStart).Trim();
            }

            int tagStart = header.LastIndexOf(" [", StringComparison.Ordinal);

            if (tagStart >= 0 && header.EndsWith("]", StringComparison.Ordinal))
            {
                passage.Tags = header.Substring(
                    tagStart + 2, header.Length - tagStart - 3);
                header = header.Substring(0, tagStart).Trim();
            }

            passage.Name = string.IsNullOrWhiteSpace(header)
                ? $"Passage_{index + 1}"
                : header;
            passage.IsSpecial = passage.Name == "StoryInit" ||
                                passage.Name == "StoryCaption" ||
                                passage.Tags.IndexOf(
                                    "stylesheet",
                                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                                passage.Tags.IndexOf(
                                    "script",
                                    StringComparison.OrdinalIgnoreCase) >= 0;

            if (!string.IsNullOrWhiteSpace(metadata))
            {
                try
                {
                    PassageMetadataJson parsed =
                        JsonUtility.FromJson<PassageMetadataJson>(metadata);

                    if (parsed != null && TryParsePosition(
                            parsed.position, out Vector2 position))
                    {
                        passage.Position = position;
                        passage.HasPosition = true;
                    }
                }
                catch (ArgumentException)
                {
                    passage.HasPosition = false;
                }
            }

            if (!passage.HasPosition)
            {
                passage.Position = new Vector2(
                    100f + index % 8 * 420f,
                    100f + index / 8 * 260f);
            }

            return passage;
        }

        private static void ReadStoryData(
            string body,
            TweeStoryData story)
        {
            try
            {
                string normalized = body.Replace(
                    "\"format-version\"",
                    "\"formatVersion\"");
                StoryDataJson data = JsonUtility.FromJson<StoryDataJson>(
                    normalized);
                story.Format = data?.format ?? string.Empty;
                story.FormatVersion = data?.formatVersion ?? string.Empty;
                story.StartPassage = data?.start ?? string.Empty;
            }
            catch (ArgumentException)
            {
                story.Issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Error,
                    "StoryData",
                    "StoryData JSON could not be read."));
            }
        }

        private static void ParsePassageBody(
            TweePassageData passage,
            string body,
            List<TweeImportIssue> issues)
        {
            List<ConditionFrame> conditions = new List<ConditionFrame>();
            MacroLinkContext macroLink = null;
            MatchCollection tokens = TokenRegex.Matches(body);
            int cursor = 0;

            foreach (Match token in tokens)
            {
                if (token.Index > cursor && macroLink == null)
                {
                    AddText(
                        passage,
                        body.Substring(cursor, token.Index - cursor),
                        CurrentCondition(conditions));
                }

                string value = token.Value;

                if (value.StartsWith("[[", StringComparison.Ordinal))
                {
                    if (macroLink == null)
                    {
                        TweeLinkData link = ParseWikiLink(
                            value,
                            CurrentCondition(conditions),
                            passage.Name,
                            issues);

                        if (link != null)
                            passage.Links.Add(link);
                    }
                }
                else
                {
                    string macro = value.Substring(2, value.Length - 4).Trim();
                    ProcessMacro(
                        passage,
                        macro,
                        conditions,
                        ref macroLink,
                        issues);
                }

                cursor = token.Index + token.Length;
            }

            if (cursor < body.Length && macroLink == null)
            {
                AddText(
                    passage,
                    body.Substring(cursor),
                    CurrentCondition(conditions));
            }

            if (macroLink != null)
            {
                issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Warning,
                    passage.Name,
                    "An unclosed <<link>> macro was ignored."));
            }

            if (conditions.Count > 0)
            {
                issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Warning,
                    passage.Name,
                    "An unclosed <<if>> block was closed during import."));
            }

            for (int i = passage.TextSegments.Count - 1; i >= 0; i--)
            {
                TweeTextSegmentData segment = passage.TextSegments[i];
                segment.Text = ConvertToUnityRichText(segment.Text);

                if (string.IsNullOrWhiteSpace(segment.Text))
                    passage.TextSegments.RemoveAt(i);
            }
        }

        private static void ProcessMacro(
            TweePassageData passage,
            string macro,
            List<ConditionFrame> conditions,
            ref MacroLinkContext macroLink,
            List<TweeImportIssue> issues)
        {
            string lower = macro.ToLowerInvariant();

            if (lower.StartsWith("if "))
            {
                string expression = macro.Substring(3).Trim();
                ConditionFrame frame = new ConditionFrame
                {
                    Current = expression
                };
                frame.PreviousBranches.Add(expression);
                conditions.Add(frame);
                return;
            }

            if (lower.StartsWith("elseif "))
            {
                if (conditions.Count == 0)
                    return;

                ConditionFrame frame = conditions[conditions.Count - 1];
                string expression = macro.Substring(7).Trim();
                string previous = string.Join(" or ", frame.PreviousBranches);
                frame.Current = $"not ({previous}) and ({expression})";
                frame.PreviousBranches.Add(expression);
                return;
            }

            if (lower == "else")
            {
                if (conditions.Count == 0)
                    return;

                ConditionFrame frame = conditions[conditions.Count - 1];
                frame.Current = "not (" +
                    string.Join(" or ", frame.PreviousBranches) + ")";
                return;
            }

            if (lower == "/if")
            {
                if (conditions.Count > 0)
                    conditions.RemoveAt(conditions.Count - 1);
                return;
            }

            Match linkMatch = LinkMacroLabelRegex.Match(macro);

            if (linkMatch.Success)
            {
                macroLink = new MacroLinkContext
                {
                    Text = FirstValue(linkMatch, "double", "single"),
                    Condition = CurrentCondition(conditions)
                };
                return;
            }

            if (lower == "/link")
            {
                if (macroLink == null)
                    return;

                if (!string.IsNullOrWhiteSpace(macroLink.Target))
                {
                    TweeLinkData link = new TweeLinkData
                    {
                        Text = ConvertToUnityRichText(macroLink.Text),
                        Target = macroLink.Target,
                        Condition = macroLink.Condition
                    };
                    link.Mutations.AddRange(macroLink.Mutations);
                    passage.Links.Add(link);
                }
                else if (!macroLink.ContainsAudio)
                {
                    issues.Add(new TweeImportIssue(
                        TweeImportIssueSeverity.Warning,
                        passage.Name,
                        "A <<link>> without <<goto>> was ignored."));
                }

                macroLink = null;
                return;
            }

            if (lower.StartsWith("set "))
            {
                List<TweeMutationData> parsed = ParseMutations(
                    macro.Substring(4), passage.Name, issues);

                if (macroLink == null)
                    passage.Mutations.AddRange(parsed);
                else
                    macroLink.Mutations.AddRange(parsed);
                return;
            }

            Match gotoMatch = GotoRegex.Match(macro);

            if (gotoMatch.Success)
            {
                string target = FirstValue(gotoMatch, "double", "single");

                if (macroLink != null)
                {
                    macroLink.Target = target;
                }
                else
                {
                    passage.Links.Add(new TweeLinkData
                    {
                        Text = "Continue",
                        Target = target,
                        Condition = CurrentCondition(conditions)
                    });
                }

                return;
            }

            if (lower.StartsWith("audio "))
            {
                if (macroLink != null)
                    macroLink.ContainsAudio = true;
                return;
            }

            if (lower.StartsWith("cacheaudio ") ||
                lower.StartsWith("run "))
            {
                issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Information,
                    passage.Name,
                    "Audio setup requires Unity asset assignment."));
                return;
            }

            if (lower == "nobr" || lower == "/nobr")
                return;

            issues.Add(new TweeImportIssue(
                TweeImportIssueSeverity.Warning,
                passage.Name,
                $"Unsupported macro ignored: <<{macro}>>"));
        }

        private static TweeLinkData ParseWikiLink(
            string token,
            string condition,
            string passage,
            List<TweeImportIssue> issues)
        {
            string inner = token.Substring(2, token.Length - 4).Trim();
            string setters = string.Empty;
            int setterStart = inner.IndexOf("][", StringComparison.Ordinal);

            if (setterStart >= 0)
            {
                setters = inner.Substring(setterStart + 2).Trim();
                inner = inner.Substring(0, setterStart).Trim();
            }

            string text;
            string target;
            int separator = inner.IndexOf('|');

            if (separator >= 0)
            {
                text = inner.Substring(0, separator).Trim();
                target = inner.Substring(separator + 1).TrimStart('|').Trim();
            }
            else
            {
                int forward = inner.IndexOf("->", StringComparison.Ordinal);
                int backward = inner.IndexOf("<-", StringComparison.Ordinal);

                if (forward >= 0)
                {
                    text = inner.Substring(0, forward).Trim();
                    target = inner.Substring(forward + 2).Trim();
                }
                else if (backward >= 0)
                {
                    target = inner.Substring(0, backward).Trim();
                    text = inner.Substring(backward + 2).Trim();
                }
                else
                {
                    text = inner;
                    target = inner;
                }
            }

            text = ConvertToUnityRichText(text).Trim('"', '\'');
            target = target.Trim('"', '\'');

            if (string.IsNullOrWhiteSpace(target))
            {
                issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Warning,
                    passage,
                    $"A link named '{text}' has no target."));
                return null;
            }

            TweeLinkData link = new TweeLinkData
            {
                Text = string.IsNullOrWhiteSpace(text) ? target : text,
                Target = target,
                Condition = condition
            };

            if (!string.IsNullOrWhiteSpace(setters))
            {
                link.Mutations.AddRange(ParseMutations(
                    setters, passage, issues));
            }

            return link;
        }

        private static List<TweeMutationData> ParseMutations(
            string source,
            string passage,
            List<TweeImportIssue> issues)
        {
            List<TweeMutationData> result = new List<TweeMutationData>();

            foreach (string part in SplitTopLevel(source))
            {
                Match assignment = AssignmentRegex.Match(part.Trim());

                if (!assignment.Success)
                {
                    issues.Add(new TweeImportIssue(
                        TweeImportIssueSeverity.Warning,
                        passage,
                        $"Unsupported assignment ignored: {part.Trim()}"));
                    continue;
                }

                string rawValue = assignment.Groups["value"].Value.Trim();
                TweeMutationData mutation = new TweeMutationData
                {
                    VariableName = assignment.Groups["name"].Value,
                    Operator = assignment.Groups["operator"].Value
                        .ToLowerInvariant() == "to"
                        ? "="
                        : assignment.Groups["operator"].Value,
                    RawValue = rawValue
                };
                Match random = RandomRegex.Match(rawValue);

                if (random.Success)
                {
                    mutation.IsRandom = true;
                    mutation.RandomMinimum = int.Parse(
                        random.Groups["minimum"].Value,
                        CultureInfo.InvariantCulture);
                    mutation.RandomMaximum = int.Parse(
                        random.Groups["maximum"].Value,
                        CultureInfo.InvariantCulture);
                }

                result.Add(mutation);
            }

            return result;
        }

        private static List<string> SplitTopLevel(string source)
        {
            List<string> result = new List<string>();
            int start = 0;
            int depth = 0;
            char quote = '\0';

            for (int i = 0; i < source.Length; i++)
            {
                char current = source[i];

                if (quote != '\0')
                {
                    if (current == quote &&
                        (i == 0 || source[i - 1] != '\\'))
                    {
                        quote = '\0';
                    }

                    continue;
                }

                if (current == '"' || current == '\'')
                    quote = current;
                else if (current == '(')
                    depth++;
                else if (current == ')')
                    depth = Math.Max(0, depth - 1);
                else if ((current == ',' || current == ';') && depth == 0)
                {
                    result.Add(source.Substring(start, i - start));
                    start = i + 1;
                }
            }

            if (start <= source.Length)
                result.Add(source.Substring(start));

            return result;
        }

        private static void AddText(
            TweePassageData passage,
            string text,
            string condition)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (passage.TextSegments.Count > 0)
            {
                TweeTextSegmentData previous =
                    passage.TextSegments[passage.TextSegments.Count - 1];

                if (previous.Condition == condition)
                {
                    previous.Text += text;
                    return;
                }
            }

            passage.TextSegments.Add(new TweeTextSegmentData
            {
                Text = text,
                Condition = condition
            });
        }

        private static string CurrentCondition(
            List<ConditionFrame> conditions)
        {
            List<string> active = new List<string>();

            foreach (ConditionFrame frame in conditions)
            {
                if (!string.IsNullOrWhiteSpace(frame.Current))
                    active.Add("(" + frame.Current + ")");
            }

            return string.Join(" and ", active);
        }

        private static string FirstValue(
            Match match,
            string first,
            string second)
        {
            return match.Groups[first].Success
                ? match.Groups[first].Value
                : match.Groups[second].Value;
        }

        private static bool TryParsePosition(
            string value,
            out Vector2 position)
        {
            position = Vector2.zero;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] parts = value.Split(',');

            if (parts.Length != 2 ||
                !float.TryParse(
                    parts[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float x) ||
                !float.TryParse(
                    parts[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float y))
            {
                return false;
            }

            position = new Vector2(x, y);
            return true;
        }
    }
}
