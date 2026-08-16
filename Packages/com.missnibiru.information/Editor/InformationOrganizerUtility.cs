using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MissNibiru.Information.Data;

namespace MissNibiru.Information.Editor
{
    public static class InformationOrganizerUtility
    {
        public static string CreateStableId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "information";

            string normalized =
                value.Trim().Normalize(
                    NormalizationForm.FormD);

            StringBuilder builder =
                new StringBuilder(normalized.Length);

            bool pendingSeparator = false;

            for (int index = 0;
                 index < normalized.Length;
                 index++)
            {
                char character = normalized[index];

                if (CharUnicodeInfo.GetUnicodeCategory(
                        character) ==
                    UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (!char.IsLetterOrDigit(character))
                {
                    pendingSeparator = builder.Length > 0;
                    continue;
                }

                bool beginsCamelCaseWord =
                    char.IsUpper(character) &&
                    index > 0 &&
                    char.IsLower(normalized[index - 1]);

                if ((pendingSeparator ||
                     beginsCamelCaseWord) &&
                    builder.Length > 0 &&
                    builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }

                builder.Append(
                    char.ToLowerInvariant(character));

                pendingSeparator = false;
            }

            string result =
                builder.ToString().Trim('_');

            return string.IsNullOrWhiteSpace(result)
                ? "information"
                : result;
        }

        public static string GenerateUniqueId(
            InformationDatabase database,
            string requestedValue,
            InformationEntry excludedEntry = null)
        {
            string baseId = CreateStableId(requestedValue);

            HashSet<string> usedIds =
                new HashSet<string>(
                    System.StringComparer.Ordinal);

            if (database != null)
            {
                foreach (
                    InformationEntry entry in database.Entries)
                {
                    if (entry == null ||
                        entry == excludedEntry ||
                        string.IsNullOrWhiteSpace(entry.Id))
                    {
                        continue;
                    }

                    usedIds.Add(entry.Id);
                }
            }

            if (!usedIds.Contains(baseId))
                return baseId;

            int suffix = 2;
            string candidate;

            do
            {
                candidate = $"{baseId}_{suffix}";
                suffix++;
            }
            while (usedIds.Contains(candidate));

            return candidate;
        }

        public static int CountWords(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            int count = 0;
            bool insideWord = false;

            foreach (char character in value)
            {
                bool wordCharacter =
                    char.IsLetterOrDigit(character) ||
                    character == '\'';

                if (wordCharacter && !insideWord)
                    count++;

                insideWord = wordCharacter;
            }

            return count;
        }

        public static bool MatchesSearch(
            InformationEntry entry,
            string search)
        {
            if (entry == null)
                return false;

            if (string.IsNullOrWhiteSpace(search))
                return true;

            string query = search.Trim();

            return ContainsIgnoreCase(
                       DisplayName(entry),
                       query) ||
                   ContainsIgnoreCase(entry.Id, query);
        }

        public static string DisplayName(
            InformationEntry entry)
        {
            if (entry == null)
                return "Missing Entry";

            return string.IsNullOrWhiteSpace(
                    entry.DisplayName)
                ? entry.name
                : entry.DisplayName;
        }

        public static string DisplayType(
            InformationEntry entry)
        {
            if (entry == null || entry.Type == null)
                return "No type";

            return DisplayType(entry.Type);
        }

        public static string DisplayType(
            InformationType informationType)
        {
            if (informationType == null)
                return "No type";

            return string.IsNullOrWhiteSpace(
                    informationType.DisplayName)
                ? informationType.name
                : informationType.DisplayName;
        }

        public static string DisplayCategory(
            InformationEntry entry)
        {
            if (entry == null || entry.Category == null)
                return "No category";

            return DisplayCategory(entry.Category);
        }

        public static string DisplayCategory(
            InformationCategory category)
        {
            if (category == null)
                return "No category";

            return string.IsNullOrWhiteSpace(
                    category.DisplayName)
                ? category.name
                : category.DisplayName;
        }

        public static string ToDisplayName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "New Information";

            StringBuilder builder = new StringBuilder();
            bool beginWord = true;

            for (int index = 0;
                 index < value.Length;
                 index++)
            {
                char character = value[index];

                if (!char.IsLetterOrDigit(character))
                {
                    if (builder.Length > 0 &&
                        builder[builder.Length - 1] != ' ')
                    {
                        builder.Append(' ');
                    }

                    beginWord = true;
                    continue;
                }

                bool camelCaseBoundary =
                    index > 0 &&
                    char.IsUpper(character) &&
                    char.IsLower(value[index - 1]);

                if (camelCaseBoundary &&
                    builder.Length > 0 &&
                    builder[builder.Length - 1] != ' ')
                {
                    builder.Append(' ');
                    beginWord = true;
                }

                builder.Append(
                    beginWord
                        ? char.ToUpperInvariant(character)
                        : character);

                beginWord = false;
            }

            return builder.ToString().Trim();
        }

        private static bool ContainsIgnoreCase(
            string value,
            string search)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(
                       search,
                       System.StringComparison
                           .OrdinalIgnoreCase) >= 0;
        }
    }
}
