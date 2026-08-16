using System;
using System.Collections.Generic;
using MissNibiru.Information.Data;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Information.Editor
{
    public enum InformationOrganizerSeverity
    {
        Success,
        Warning,
        Error
    }

    public sealed class InformationOrganizerIssue
    {
        public InformationOrganizerSeverity Severity
        {
            get;
        }

        public string Message { get; }
        public UnityEngine.Object Context { get; }

        public InformationOrganizerIssue(
            InformationOrganizerSeverity severity,
            string message,
            UnityEngine.Object context = null)
        {
            Severity = severity;
            Message = message;
            Context = context;
        }
    }

    public static class InformationOrganizerValidator
    {
        public static List<InformationOrganizerIssue>
            Validate(
                InformationDatabase database,
                bool includeUnregisteredEntries = true)
        {
            List<InformationOrganizerIssue> issues =
                new List<InformationOrganizerIssue>();

            if (database == null)
            {
                Add(
                    issues,
                    InformationOrganizerSeverity.Error,
                    "No database selected.");

                return issues;
            }

            Dictionary<string, InformationEntry> ids =
                new Dictionary<string, InformationEntry>(
                    StringComparer.Ordinal);

            HashSet<InformationEntry> registered =
                new HashSet<InformationEntry>();

            for (int index = 0;
                 index < database.Entries.Count;
                 index++)
            {
                InformationEntry entry =
                    database.Entries[index];

                if (entry == null)
                {
                    Add(
                        issues,
                        InformationOrganizerSeverity.Error,
                        $"Entry {index + 1} is missing.",
                        database);

                    continue;
                }

                registered.Add(entry);
                ValidateEntry(issues, entry, ids);
            }

            if (includeUnregisteredEntries)
            {
                ValidateUnregisteredEntries(
                    issues,
                    registered);
            }

            return issues;
        }

        private static void ValidateEntry(
            List<InformationOrganizerIssue> issues,
            InformationEntry entry,
            Dictionary<string, InformationEntry> ids)
        {
            string label =
                InformationOrganizerUtility.DisplayName(entry);

            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                Add(
                    issues,
                    InformationOrganizerSeverity.Error,
                    $"'{label}' has no ID.",
                    entry);
            }
            else if (ids.TryGetValue(
                         entry.Id,
                         out InformationEntry first))
            {
                Add(
                    issues,
                    InformationOrganizerSeverity.Error,
                    $"Duplicate ID '{entry.Id}'.",
                    first);

                Add(
                    issues,
                    InformationOrganizerSeverity.Error,
                    $"Duplicate ID '{entry.Id}'.",
                    entry);
            }
            else
            {
                ids.Add(entry.Id, entry);
            }

            if (string.IsNullOrWhiteSpace(
                    entry.DisplayName))
            {
                Add(
                    issues,
                    InformationOrganizerSeverity.Warning,
                    $"'{entry.name}' needs a display name.",
                    entry);
            }

            if (string.IsNullOrWhiteSpace(entry.Summary))
            {
                Add(
                    issues,
                    InformationOrganizerSeverity.Warning,
                    $"'{label}' has no summary.",
                    entry);
            }

            if (entry.Type == null)
            {
                Add(
                    issues,
                    InformationOrganizerSeverity.Warning,
                    $"'{label}' has no type.",
                    entry);
            }

            if (entry.Category == null)
            {
                Add(
                    issues,
                    InformationOrganizerSeverity.Warning,
                    $"'{label}' has no category.",
                    entry);
            }

            ValidatePages(issues, entry, label);
            ValidateBrokenReferences(issues, entry, label);
        }

        private static void ValidatePages(
            List<InformationOrganizerIssue> issues,
            InformationEntry entry,
            string label)
        {
            for (int index = 0;
                 index < entry.Pages.Count;
                 index++)
            {
                InformationPage page = entry.Pages[index];

                if (page == null)
                {
                    Add(
                        issues,
                        InformationOrganizerSeverity.Error,
                        $"'{label}' page {index + 1} is missing.",
                        entry);

                    continue;
                }

                if (string.IsNullOrWhiteSpace(page.Heading))
                {
                    Add(
                        issues,
                        InformationOrganizerSeverity.Warning,
                        $"'{label}' page {index + 1} needs a heading.",
                        entry);
                }

                if (string.IsNullOrWhiteSpace(page.Body))
                {
                    Add(
                        issues,
                        InformationOrganizerSeverity.Warning,
                        $"'{label}' page {index + 1} has no text.",
                        entry);
                }
            }
        }

        private static void ValidateBrokenReferences(
            List<InformationOrganizerIssue> issues,
            InformationEntry entry,
            string label)
        {
            SerializedObject serializedEntry =
                new SerializedObject(entry);

            CheckReference(
                issues,
                serializedEntry.FindProperty("informationType"),
                $"'{label}' has a broken type reference.",
                entry);

            CheckReference(
                issues,
                serializedEntry.FindProperty("category"),
                $"'{label}' has a broken category reference.",
                entry);

            CheckReference(
                issues,
                serializedEntry.FindProperty("icon"),
                $"'{label}' has a broken icon reference.",
                entry);

            CheckReference(
                issues,
                serializedEntry.FindProperty("image"),
                $"'{label}' has a broken image reference.",
                entry);

            CheckReference(
                issues,
                serializedEntry.FindProperty("relatedAsset"),
                $"'{label}' has a broken related asset.",
                entry);

            SerializedProperty pages =
                serializedEntry.FindProperty("pages");

            if (pages == null || !pages.isArray)
                return;

            for (int index = 0;
                 index < pages.arraySize;
                 index++)
            {
                SerializedProperty page =
                    pages.GetArrayElementAtIndex(index);

                CheckReference(
                    issues,
                    page.FindPropertyRelative("image"),
                    $"'{label}' page {index + 1} has a broken image.",
                    entry);
            }
        }

        private static void CheckReference(
            List<InformationOrganizerIssue> issues,
            SerializedProperty property,
            string message,
            UnityEngine.Object context)
        {
            if (property == null ||
                property.propertyType !=
                    SerializedPropertyType.ObjectReference)
            {
                return;
            }

            if (property.objectReferenceValue == null &&
                property.objectReferenceInstanceIDValue != 0)
            {
                Add(
                    issues,
                    InformationOrganizerSeverity.Error,
                    message,
                    context);
            }
        }

        private static void ValidateUnregisteredEntries(
            List<InformationOrganizerIssue> issues,
            HashSet<InformationEntry> registered)
        {
            foreach (
                string guid in AssetDatabase.FindAssets(
                    "t:InformationEntry"))
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(guid);

                InformationEntry entry =
                    AssetDatabase.LoadAssetAtPath<
                        InformationEntry>(path);

                if (entry == null || registered.Contains(entry))
                    continue;

                Add(
                    issues,
                    InformationOrganizerSeverity.Warning,
                    $"'{InformationOrganizerUtility.DisplayName(entry)}' " +
                    "is not registered here.",
                    entry);
            }
        }

        private static void Add(
            List<InformationOrganizerIssue> issues,
            InformationOrganizerSeverity severity,
            string message,
            UnityEngine.Object context = null)
        {
            issues.Add(
                new InformationOrganizerIssue(
                    severity,
                    message,
                    context));
        }
    }
}
