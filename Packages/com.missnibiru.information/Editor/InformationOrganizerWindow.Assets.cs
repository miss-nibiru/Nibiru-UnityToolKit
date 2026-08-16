using System;
using System.Collections.Generic;
using System.IO;
using MissNibiru.Information.Data;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Information.Editor
{
    public sealed partial class InformationOrganizerWindow
    {
        private void OnProjectChange()
        {
            RebuildBrowser();
            RefreshValidation();
            Repaint();
        }

        private void HandleUndoRedo()
        {
            RebuildBrowser();
            RefreshValidation(false);
            SetStatus(
                "Undo or redo completed.",
                InformationOrganizerSeverity.Success);

            Repaint();
        }

        private void SetDatabase(
            InformationDatabase selectedDatabase)
        {
            database = selectedDatabase;
            selectedEntry = null;
            selectedType = null;
            selectedCategory = null;
            searchQuery = string.Empty;

            RebuildBrowser();
            RefreshValidation();

            SetStatus(
                database == null
                    ? "Select or create a database."
                    : "Database ready.",
                database == null
                    ? InformationOrganizerSeverity.Warning
                    : InformationOrganizerSeverity.Success);

            Repaint();
        }

        private void SelectEntry(
            InformationEntry entry)
        {
            selectedEntry = entry;
            organizerView = OrganizerView.Edit;
            editorScroll = Vector2.zero;

            SetStatus(
                entry == null
                    ? "Select an entry."
                    : "Entry selected.",
                entry == null
                    ? InformationOrganizerSeverity.Warning
                    : InformationOrganizerSeverity.Success);

            Repaint();
        }

        private void RebuildBrowser()
        {
            RefreshClassificationOptions();
            visibleEntries.Clear();

            if (database == null)
                return;

            foreach (
                InformationEntry entry in database.Entries)
            {
                if (entry == null)
                {
                    if (selectedType == null &&
                        selectedCategory == null &&
                        string.IsNullOrWhiteSpace(
                            searchQuery))
                    {
                        visibleEntries.Add(null);
                    }

                    continue;
                }

                if (selectedType != null &&
                    entry.Type != selectedType)
                {
                    continue;
                }

                if (selectedCategory != null &&
                    entry.Category != selectedCategory)
                {
                    continue;
                }

                if (!InformationOrganizerUtility
                        .MatchesSearch(entry, searchQuery))
                {
                    continue;
                }

                visibleEntries.Add(entry);
            }

            visibleEntries.Sort(CompareEntries);
        }

        private void RefreshClassificationOptions()
        {
            typeOptions.Clear();
            categoryOptions.Clear();

            if (database == null)
                return;

            foreach (
                InformationEntry entry in database.Entries)
            {
                if (entry == null)
                    continue;

                if (entry.Type != null &&
                    !typeOptions.Contains(entry.Type))
                {
                    typeOptions.Add(entry.Type);
                }

                if (entry.Category != null &&
                    !categoryOptions.Contains(
                        entry.Category))
                {
                    categoryOptions.Add(entry.Category);
                }
            }

            typeOptions.Sort(
                (left, right) =>
                    StringComparer.OrdinalIgnoreCase.Compare(
                        TypeFilterLabel(left),
                        TypeFilterLabel(right)));

            categoryOptions.Sort(
                (left, right) =>
                    StringComparer.OrdinalIgnoreCase.Compare(
                        CategoryFilterLabel(left),
                        CategoryFilterLabel(right)));

            if (selectedType != null &&
                !typeOptions.Contains(selectedType))
            {
                selectedType = null;
            }

            if (selectedCategory != null &&
                !categoryOptions.Contains(selectedCategory))
            {
                selectedCategory = null;
            }
        }

        private string[] BuildTypeLabels()
        {
            string[] labels =
                new string[typeOptions.Count + 1];

            labels[0] = "All Types";

            for (int index = 0;
                 index < typeOptions.Count;
                 index++)
            {
                labels[index + 1] =
                    TypeFilterLabel(typeOptions[index]);
            }

            return labels;
        }

        private string[] BuildCategoryLabels()
        {
            string[] labels =
                new string[categoryOptions.Count + 1];

            labels[0] = "All Categories";

            for (int index = 0;
                 index < categoryOptions.Count;
                 index++)
            {
                labels[index + 1] =
                    CategoryFilterLabel(
                        categoryOptions[index]);
            }

            return labels;
        }

        private int CompareEntries(
            InformationEntry left,
            InformationEntry right)
        {
            if (ReferenceEquals(left, right))
                return 0;

            if (left == null)
                return 1;

            if (right == null)
                return -1;

            string leftKey = SortKey(left);
            string rightKey = SortKey(right);

            int comparison =
                StringComparer.OrdinalIgnoreCase.Compare(
                    leftKey,
                    rightKey);

            if (IsDescendingSort())
                comparison = -comparison;

            if (comparison != 0)
                return comparison;

            comparison =
                StringComparer.OrdinalIgnoreCase.Compare(
                    InformationOrganizerUtility
                        .DisplayName(left),
                    InformationOrganizerUtility
                        .DisplayName(right));

            if (comparison != 0)
                return comparison;

            return StringComparer.OrdinalIgnoreCase.Compare(
                left.Id,
                right.Id);
        }

        private string SortKey(
            InformationEntry entry)
        {
            switch (sortMode)
            {
                case EntrySortMode.IdAscending:
                case EntrySortMode.IdDescending:
                    return entry.Id;

                case EntrySortMode.TypeAscending:
                case EntrySortMode.TypeDescending:
                    return InformationOrganizerUtility
                        .DisplayType(entry);

                case EntrySortMode.CategoryAscending:
                case EntrySortMode.CategoryDescending:
                    return InformationOrganizerUtility
                        .DisplayCategory(entry);

                default:
                    return InformationOrganizerUtility
                        .DisplayName(entry);
            }
        }

        private bool IsDescendingSort()
        {
            return sortMode ==
                       EntrySortMode.NameDescending ||
                   sortMode ==
                       EntrySortMode.IdDescending ||
                   sortMode ==
                       EntrySortMode.TypeDescending ||
                   sortMode ==
                       EntrySortMode.CategoryDescending;
        }

        private void CreateDatabase()
        {
            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Create Information Database",
                    "InformationDatabase",
                    "asset",
                    "Choose the database location.");

            if (string.IsNullOrWhiteSpace(path))
                return;

            path = AssetDatabase.GenerateUniqueAssetPath(path);

            InformationDatabase created =
                CreateInstance<InformationDatabase>();

            AssetDatabase.CreateAsset(created, path);
            Undo.RegisterCreatedObjectUndo(
                created,
                "Create Information Database");

            SetDatabase(created);
            PingObject(created);
            GUIUtility.ExitGUI();
        }

        private void CreateEntry()
        {
            if (database == null)
                return;

            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Create Information Entry",
                    "New Information Entry",
                    "asset",
                    "Choose the entry location.");

            if (string.IsNullOrWhiteSpace(path))
                return;

            path = AssetDatabase.GenerateUniqueAssetPath(path);

            string assetName =
                Path.GetFileNameWithoutExtension(path);

            string displayName =
                InformationOrganizerUtility.ToDisplayName(
                    assetName);

            string id =
                InformationOrganizerUtility.GenerateUniqueId(
                    database,
                    displayName);

            InformationEntry created =
                CreateInstance<InformationEntry>();

            created.Configure(
                id,
                displayName,
                string.Empty);

            AssetDatabase.CreateAsset(created, path);
            Undo.RegisterCreatedObjectUndo(
                created,
                "Create Information Entry");

            RegisterEntry(created);
            SelectEntry(created);
            RebuildBrowser();
            RefreshValidation(false);

            SetStatus(
                "Entry created and registered.",
                InformationOrganizerSeverity.Success);

            PingObject(created);
            GUIUtility.ExitGUI();
        }

        private void DuplicateEntry()
        {
            if (database == null || selectedEntry == null)
                return;

            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Duplicate Information Entry",
                    selectedEntry.name + " Copy",
                    "asset",
                    "Choose the duplicate location.");

            if (string.IsNullOrWhiteSpace(path))
                return;

            path = AssetDatabase.GenerateUniqueAssetPath(path);

            InformationEntry duplicate =
                Instantiate(selectedEntry);

            duplicate.name =
                Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(duplicate, path);
            Undo.RegisterCreatedObjectUndo(
                duplicate,
                "Duplicate Information Entry");

            SerializedObject serializedDuplicate =
                new SerializedObject(duplicate);

            serializedDuplicate.Update();

            SerializedProperty displayName =
                serializedDuplicate.FindProperty(
                    "displayName");

            SerializedProperty id =
                serializedDuplicate.FindProperty("id");

            string copiedName =
                InformationOrganizerUtility
                    .DisplayName(selectedEntry) +
                " Copy";

            displayName.stringValue = copiedName;
            id.stringValue =
                InformationOrganizerUtility.GenerateUniqueId(
                    database,
                    copiedName);

            serializedDuplicate.ApplyModifiedProperties();

            RegisterEntry(duplicate);
            SelectEntry(duplicate);
            RebuildBrowser();
            RefreshValidation(false);

            SetStatus(
                "Entry duplicated and registered.",
                InformationOrganizerSeverity.Success);

            PingObject(duplicate);
            GUIUtility.ExitGUI();
        }

        private void DeleteSelectedEntry()
        {
            if (database == null || selectedEntry == null)
                return;

            string name =
                InformationOrganizerUtility
                    .DisplayName(selectedEntry);

            bool confirmed =
                EditorUtility.DisplayDialog(
                    "Delete Information Entry",
                    $"Move '{name}' to the system Trash?",
                    "Move to Trash",
                    "Cancel");

            if (!confirmed)
                return;

            InformationEntry deleting = selectedEntry;
            string path = AssetDatabase.GetAssetPath(deleting);

            UnregisterEntry(deleting);

            if (!AssetDatabase.MoveAssetToTrash(path))
            {
                RegisterEntry(deleting);

                SetStatus(
                    "The entry could not be deleted.",
                    InformationOrganizerSeverity.Error);

                return;
            }

            selectedEntry = null;
            RebuildBrowser();
            RefreshValidation();

            SetStatus(
                "Entry moved to Trash.",
                InformationOrganizerSeverity.Success);

            GUIUtility.ExitGUI();
        }

        private void RegisterEntry(
            InformationEntry entry)
        {
            if (database == null || entry == null)
                return;

            foreach (
                InformationEntry existing in database.Entries)
            {
                if (existing == entry)
                    return;
            }

            Undo.RecordObject(
                database,
                "Register Information Entry");

            SerializedObject serializedDatabase =
                new SerializedObject(database);

            serializedDatabase.Update();

            SerializedProperty entries =
                serializedDatabase.FindProperty("entries");

            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            entries.GetArrayElementAtIndex(index)
                .objectReferenceValue = entry;

            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
        }

        private void UnregisterEntry(
            InformationEntry entry)
        {
            if (database == null || entry == null)
                return;

            Undo.RecordObject(
                database,
                "Unregister Information Entry");

            SerializedObject serializedDatabase =
                new SerializedObject(database);

            serializedDatabase.Update();

            SerializedProperty entries =
                serializedDatabase.FindProperty("entries");

            for (int index = entries.arraySize - 1;
                 index >= 0;
                 index--)
            {
                SerializedProperty element =
                    entries.GetArrayElementAtIndex(index);

                if (element.objectReferenceValue != entry)
                    continue;

                entries.DeleteArrayElementAtIndex(index);

                if (index < entries.arraySize &&
                    entries.GetArrayElementAtIndex(index)
                        .objectReferenceValue == null)
                {
                    entries.DeleteArrayElementAtIndex(index);
                }
            }

            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
        }

        private void GenerateSelectedId()
        {
            if (selectedEntry == null)
                return;

            SerializedObject serializedEntry =
                new SerializedObject(selectedEntry);

            serializedEntry.Update();

            SerializedProperty displayName =
                serializedEntry.FindProperty("displayName");

            SerializedProperty id =
                serializedEntry.FindProperty("id");

            string source =
                string.IsNullOrWhiteSpace(
                    displayName.stringValue)
                    ? selectedEntry.name
                    : displayName.stringValue;

            id.stringValue =
                InformationOrganizerUtility.GenerateUniqueId(
                    database,
                    source,
                    selectedEntry);

            serializedEntry.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedEntry);

            RebuildBrowser();
            RefreshValidation(false);

            SetStatus(
                "Unique ID generated.",
                InformationOrganizerSeverity.Success);
        }

        private void CreateAndAssignType()
        {
            if (selectedEntry == null)
                return;

            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Create Information Type",
                    "New Information Type",
                    "asset",
                    "Choose the type location.");

            if (string.IsNullOrWhiteSpace(path))
                return;

            path = AssetDatabase.GenerateUniqueAssetPath(path);

            string displayName =
                InformationOrganizerUtility.ToDisplayName(
                    Path.GetFileNameWithoutExtension(path));

            InformationType created =
                CreateInstance<InformationType>();

            created.Configure(
                GenerateUniqueClassificationId<
                    InformationType>(displayName),
                displayName);

            AssetDatabase.CreateAsset(created, path);
            Undo.RegisterCreatedObjectUndo(
                created,
                "Create Information Type");

            AssignReference("informationType", created);
            RebuildBrowser();
            RefreshValidation(false);

            SetStatus(
                "Type created and assigned.",
                InformationOrganizerSeverity.Success);

            PingObject(created);
            GUIUtility.ExitGUI();
        }

        private void CreateAndAssignCategory()
        {
            if (selectedEntry == null)
                return;

            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Create Information Category",
                    "New Information Category",
                    "asset",
                    "Choose the category location.");

            if (string.IsNullOrWhiteSpace(path))
                return;

            path = AssetDatabase.GenerateUniqueAssetPath(path);

            string displayName =
                InformationOrganizerUtility.ToDisplayName(
                    Path.GetFileNameWithoutExtension(path));

            InformationCategory created =
                CreateInstance<InformationCategory>();

            created.Configure(
                GenerateUniqueClassificationId<
                    InformationCategory>(displayName),
                displayName);

            AssetDatabase.CreateAsset(created, path);
            Undo.RegisterCreatedObjectUndo(
                created,
                "Create Information Category");

            AssignReference("category", created);
            RebuildBrowser();
            RefreshValidation(false);

            SetStatus(
                "Category created and assigned.",
                InformationOrganizerSeverity.Success);

            PingObject(created);
            GUIUtility.ExitGUI();
        }

        private void AssignReference(
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serializedEntry =
                new SerializedObject(selectedEntry);

            serializedEntry.Update();

            serializedEntry.FindProperty(propertyName)
                .objectReferenceValue = value;

            serializedEntry.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedEntry);
        }

        private string GenerateUniqueClassificationId<T>(
            string requested)
            where T : UnityEngine.Object
        {
            string baseId =
                InformationOrganizerUtility.CreateStableId(
                    requested);

            HashSet<string> usedIds =
                new HashSet<string>(
                    StringComparer.Ordinal);

            foreach (
                string guid in AssetDatabase.FindAssets(
                    $"t:{typeof(T).Name}"))
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(guid);

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset is InformationType type &&
                    !string.IsNullOrWhiteSpace(type.Id))
                {
                    usedIds.Add(type.Id);
                }
                else if (
                    asset is InformationCategory category &&
                    !string.IsNullOrWhiteSpace(category.Id))
                {
                    usedIds.Add(category.Id);
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

        private void SaveAll()
        {
            AssetDatabase.SaveAssets();

            SetStatus(
                "All changes saved.",
                InformationOrganizerSeverity.Success);

            RefreshValidation();
        }

        private void RefreshValidation(
            bool includeUnregisteredEntries = true)
        {
            validationIssues.Clear();

            validationIssues.AddRange(
                InformationOrganizerValidator.Validate(
                    database,
                    includeUnregisteredEntries));

            Repaint();
        }

        private void SetValidationStatus()
        {
            int errors = 0;
            int warnings = 0;

            foreach (
                InformationOrganizerIssue issue in
                    validationIssues)
            {
                if (issue.Severity ==
                    InformationOrganizerSeverity.Error)
                {
                    errors++;
                }
                else if (issue.Severity ==
                         InformationOrganizerSeverity.Warning)
                {
                    warnings++;
                }
            }

            if (errors > 0)
            {
                SetStatus(
                    $"Validation found {errors} errors.",
                    InformationOrganizerSeverity.Error);
            }
            else if (warnings > 0)
            {
                SetStatus(
                    $"Validation found {warnings} warnings.",
                    InformationOrganizerSeverity.Warning);
            }
            else
            {
                SetStatus(
                    "Database validation passed.",
                    InformationOrganizerSeverity.Success);
            }
        }

        private void SetStatus(
            string message,
            InformationOrganizerSeverity severity)
        {
            statusMessage = message;
            statusSeverity = severity;
            Repaint();
        }

        private static void PingObject(
            UnityEngine.Object target)
        {
            if (target == null)
                return;

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private static string TypeFilterLabel(
            InformationType type)
        {
            string name =
                InformationOrganizerUtility.DisplayType(type);

            return string.IsNullOrWhiteSpace(type.Id)
                ? name
                : $"{name} ({type.Id})";
        }

        private static string CategoryFilterLabel(
            InformationCategory category)
        {
            string name =
                InformationOrganizerUtility
                    .DisplayCategory(category);

            return string.IsNullOrWhiteSpace(category.Id)
                ? name
                : $"{name} ({category.Id})";
        }
    }
}
