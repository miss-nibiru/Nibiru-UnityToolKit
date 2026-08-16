using MissNibiru.Information.Data;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Information.Editor
{
    public sealed partial class InformationOrganizerWindow
    {
        private void DrawSelectedEntryEditor()
        {
            if (selectedEntry == null)
                return;

            requestedPageAction = PageAction.None;
            requestedPageIndex = -1;

            SerializedObject serializedEntry =
                new SerializedObject(selectedEntry);

            serializedEntry.UpdateIfRequiredOrScript();

            SerializedProperty id =
                serializedEntry.FindProperty("id");

            SerializedProperty displayName =
                serializedEntry.FindProperty("displayName");

            SerializedProperty informationType =
                serializedEntry.FindProperty(
                    "informationType");

            SerializedProperty category =
                serializedEntry.FindProperty("category");

            SerializedProperty summary =
                serializedEntry.FindProperty("summary");

            SerializedProperty icon =
                serializedEntry.FindProperty("icon");

            SerializedProperty image =
                serializedEntry.FindProperty("image");

            SerializedProperty pages =
                serializedEntry.FindProperty("pages");

            SerializedProperty relatedAsset =
                serializedEntry.FindProperty(
                    "relatedAsset");

            EditorGUILayout.LabelField(
                InformationOrganizerUtility
                    .DisplayName(selectedEntry),
                previewTitleStyle);

            EditorGUILayout.Space(4f);
            DrawWordLimitSettings();
            EditorGUILayout.Space(6f);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                displayName,
                new GUIContent(
                    "Display Name",
                    "Name shown to players."));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(
                    id,
                    new GUIContent(
                        "Stable ID",
                        "Stable save-friendly identifier."));

                if (GUILayout.Button(
                        new GUIContent(
                            "Generate",
                            "Generate a unique ID."),
                        GUILayout.Width(72f)))
                {
                    serializedEntry.ApplyModifiedProperties();
                    GenerateSelectedId();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Classification",
                EditorStyles.boldLabel);

            DrawReferenceWithCreateButton(
                serializedEntry,
                informationType,
                new GUIContent(
                    "Type",
                    "Broad class, such as Item."),
                "Create a broad reusable class.",
                CreateAndAssignType);

            DrawReferenceWithCreateButton(
                serializedEntry,
                category,
                new GUIContent(
                    "Category",
                    "Narrow group, such as Potion."),
                "Create a reusable content group.",
                CreateAndAssignCategory);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                new GUIContent(
                    "Summary",
                    "Short entry description."),
                EditorStyles.boldLabel);

            summary.stringValue = EditorGUILayout.TextArea(
                summary.stringValue,
                GUILayout.MinHeight(70f));

            DrawWordCount(
                summary.stringValue,
                summaryWordLimit);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Presentation",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                icon,
                new GUIContent(
                    "Icon",
                    "Small list image."));

            EditorGUILayout.PropertyField(
                image,
                new GUIContent(
                    "Image",
                    "Main entry image."));

            EditorGUILayout.PropertyField(
                relatedAsset,
                new GUIContent(
                    "Related Asset",
                    "Link optional gameplay data."));

            EditorGUILayout.Space(6f);
            DrawPages(pages);

            bool guiChanged = EditorGUI.EndChangeCheck();
            bool applied =
                serializedEntry.ApplyModifiedProperties();

            if (guiChanged || applied)
            {
                EditorUtility.SetDirty(selectedEntry);
                RebuildBrowser();
                RefreshValidation(false);

                SetStatus(
                    "Entry updated.",
                    InformationOrganizerSeverity.Warning);
            }

            if (requestedPageAction != PageAction.None)
            {
                ApplyPageAction(
                    requestedPageAction,
                    requestedPageIndex);

                GUIUtility.ExitGUI();
            }
        }

        private void DrawWordLimitSettings()
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Word Warnings",
                    EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();

                using (new EditorGUILayout.HorizontalScope())
                {
                    summaryWordLimit = Mathf.Max(
                        0,
                        EditorGUILayout.IntField(
                            new GUIContent(
                                "Summary",
                                "Zero disables this warning."),
                            summaryWordLimit));

                    pageWordLimit = Mathf.Max(
                        0,
                        EditorGUILayout.IntField(
                            new GUIContent(
                                "Page",
                                "Zero disables this warning."),
                            pageWordLimit));
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(
                        SummaryLimitPreference,
                        summaryWordLimit);

                    EditorPrefs.SetInt(
                        PageLimitPreference,
                        pageWordLimit);
                }
            }
        }

        private void DrawReferenceWithCreateButton(
            SerializedObject serializedEntry,
            SerializedProperty property,
            GUIContent label,
            string buttonTooltip,
            System.Action createAction)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(
                    property,
                    label);

                if (!GUILayout.Button(
                        new GUIContent("+", buttonTooltip),
                        GUILayout.Width(24f)))
                {
                    return;
                }

                serializedEntry.ApplyModifiedProperties();
                createAction();
            }
        }

        private void DrawPages(
            SerializedProperty pages)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"Pages ({pages.arraySize})",
                    EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        new GUIContent(
                            "Add Page",
                            "Add a new page."),
                        GUILayout.Width(76f)))
                {
                    requestedPageAction = PageAction.Add;
                }
            }

            if (pages.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Pages are optional.",
                    MessageType.Info);

                return;
            }

            for (int index = 0;
                 index < pages.arraySize;
                 index++)
            {
                SerializedProperty page =
                    pages.GetArrayElementAtIndex(index);

                SerializedProperty heading =
                    page.FindPropertyRelative("heading");

                SerializedProperty body =
                    page.FindPropertyRelative("body");

                SerializedProperty pageImage =
                    page.FindPropertyRelative("image");

                using (new EditorGUILayout.VerticalScope(
                           EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            $"Page {index + 1}",
                            EditorStyles.boldLabel);

                        GUILayout.FlexibleSpace();

                        using (new EditorGUI.DisabledScope(
                                   index == 0))
                        {
                            if (GUILayout.Button(
                                    new GUIContent(
                                        "↑",
                                        "Move page up."),
                                    GUILayout.Width(26f)))
                            {
                                RequestPageAction(
                                    PageAction.MoveUp,
                                    index);
                            }
                        }

                        using (new EditorGUI.DisabledScope(
                                   index >=
                                   pages.arraySize - 1))
                        {
                            if (GUILayout.Button(
                                    new GUIContent(
                                        "↓",
                                        "Move page down."),
                                    GUILayout.Width(26f)))
                            {
                                RequestPageAction(
                                    PageAction.MoveDown,
                                    index);
                            }
                        }

                        if (GUILayout.Button(
                                new GUIContent(
                                    "×",
                                    "Remove this page."),
                                GUILayout.Width(26f)))
                        {
                            RequestPageAction(
                                PageAction.Delete,
                                index);
                        }
                    }

                    if (heading == null || body == null)
                    {
                        EditorGUILayout.HelpBox(
                            "This page data is missing.",
                            MessageType.Error);

                        continue;
                    }

                    EditorGUILayout.PropertyField(
                        heading,
                        new GUIContent(
                            "Heading",
                            "Page title."));

                    EditorGUILayout.LabelField(
                        new GUIContent(
                            "Body",
                            "Page text."),
                        EditorStyles.miniLabel);

                    body.stringValue = EditorGUILayout.TextArea(
                        body.stringValue,
                        GUILayout.MinHeight(80f));

                    DrawWordCount(
                        body.stringValue,
                        pageWordLimit);

                    if (pageImage != null)
                    {
                        EditorGUILayout.PropertyField(
                            pageImage,
                            new GUIContent(
                                "Image",
                                "Optional page image."));
                    }
                }
            }
        }

        private void RequestPageAction(
            PageAction action,
            int index)
        {
            requestedPageAction = action;
            requestedPageIndex = index;
        }

        private void ApplyPageAction(
            PageAction action,
            int index)
        {
            if (selectedEntry == null)
                return;

            if (action == PageAction.Delete &&
                !EditorUtility.DisplayDialog(
                    "Remove Information Page",
                    $"Remove page {index + 1}?",
                    "Remove",
                    "Cancel"))
            {
                return;
            }

            Undo.RecordObject(
                selectedEntry,
                "Edit Information Pages");

            SerializedObject serializedEntry =
                new SerializedObject(selectedEntry);

            serializedEntry.Update();

            SerializedProperty pages =
                serializedEntry.FindProperty("pages");

            switch (action)
            {
                case PageAction.Add:
                    AddPage(pages);
                    break;

                case PageAction.MoveUp:
                    if (index > 0 && index < pages.arraySize)
                    {
                        pages.MoveArrayElement(
                            index,
                            index - 1);
                    }
                    break;

                case PageAction.MoveDown:
                    if (index >= 0 &&
                        index < pages.arraySize - 1)
                    {
                        pages.MoveArrayElement(
                            index,
                            index + 1);
                    }
                    break;

                case PageAction.Delete:
                    if (index >= 0 && index < pages.arraySize)
                        pages.DeleteArrayElementAtIndex(index);
                    break;
            }

            serializedEntry.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedEntry);
            RefreshValidation(false);

            SetStatus(
                "Pages updated.",
                InformationOrganizerSeverity.Warning);
        }

        private static void AddPage(
            SerializedProperty pages)
        {
            int index = pages.arraySize;
            pages.InsertArrayElementAtIndex(index);

            SerializedProperty page =
                pages.GetArrayElementAtIndex(index);

            SerializedProperty heading =
                page.FindPropertyRelative("heading");

            SerializedProperty body =
                page.FindPropertyRelative("body");

            SerializedProperty image =
                page.FindPropertyRelative("image");

            if (heading != null)
                heading.stringValue = string.Empty;

            if (body != null)
                body.stringValue = string.Empty;

            if (image != null)
                image.objectReferenceValue = null;
        }

        private void DrawSelectedEntryPreview()
        {
            if (selectedEntry == null)
                return;

            EditorGUILayout.LabelField(
                InformationOrganizerUtility
                    .DisplayName(selectedEntry),
                previewTitleStyle);

            EditorGUILayout.LabelField(
                string.IsNullOrWhiteSpace(selectedEntry.Id)
                    ? "Missing ID"
                    : selectedEntry.Id,
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                InformationOrganizerUtility
                    .DisplayType(selectedEntry) +
                "  •  " +
                InformationOrganizerUtility
                    .DisplayCategory(selectedEntry),
                EditorStyles.boldLabel);

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(
                           GUILayout.Width(120f)))
                {
                    EditorGUILayout.LabelField(
                        "Icon",
                        EditorStyles.boldLabel);

                    DrawSpritePreview(
                        selectedEntry.Icon,
                        96f,
                        "No icon");
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(
                        "Main Image",
                        EditorStyles.boldLabel);

                    DrawSpritePreview(
                        selectedEntry.Image,
                        160f,
                        "No image");
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Summary",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                string.IsNullOrWhiteSpace(selectedEntry.Summary)
                    ? "No summary."
                    : selectedEntry.Summary,
                wrapLabelStyle);

            DrawWordCount(
                selectedEntry.Summary,
                summaryWordLimit);

            if (selectedEntry.RelatedAsset != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Related Asset",
                        "Linked game-specific data."),
                    selectedEntry.RelatedAsset,
                    typeof(Object),
                    false);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(
                $"Pages ({selectedEntry.Pages.Count})",
                EditorStyles.boldLabel);

            if (selectedEntry.Pages.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This entry has no pages.",
                    MessageType.Info);

                return;
            }

            for (int index = 0;
                 index < selectedEntry.Pages.Count;
                 index++)
            {
                InformationPage page =
                    selectedEntry.Pages[index];

                using (new EditorGUILayout.VerticalScope(
                           EditorStyles.helpBox))
                {
                    if (page == null)
                    {
                        EditorGUILayout.HelpBox(
                            $"Page {index + 1} is missing.",
                            MessageType.Error);

                        continue;
                    }

                    EditorGUILayout.LabelField(
                        string.IsNullOrWhiteSpace(page.Heading)
                            ? $"Page {index + 1}"
                            : page.Heading,
                        EditorStyles.boldLabel);

                    if (page.Image != null)
                    {
                        DrawSpritePreview(
                            page.Image,
                            140f,
                            string.Empty);
                    }

                    EditorGUILayout.LabelField(
                        string.IsNullOrWhiteSpace(page.Body)
                            ? "No page text."
                            : page.Body,
                        wrapLabelStyle);

                    DrawWordCount(
                        page.Body,
                        pageWordLimit);
                }
            }
        }

        private void DrawSpritePreview(
            Sprite sprite,
            float height,
            string emptyMessage)
        {
            if (sprite == null)
            {
                Rect empty = GUILayoutUtility.GetRect(
                    80f,
                    height,
                    GUILayout.ExpandWidth(true));

                GUI.Box(empty, emptyMessage);
                return;
            }

            Texture2D preview =
                AssetPreview.GetAssetPreview(sprite);

            if (preview == null)
                preview = AssetPreview.GetMiniThumbnail(sprite);

            if (preview == null)
                preview = sprite.texture;

            Rect rect = GUILayoutUtility.GetRect(
                80f,
                height,
                GUILayout.ExpandWidth(true));

            GUI.Box(rect, GUIContent.none);

            GUI.DrawTexture(
                rect,
                preview,
                ScaleMode.ScaleToFit,
                true);
        }

        private void DrawWordCount(
            string value,
            int limit)
        {
            int count =
                InformationOrganizerUtility.CountWords(value);

            bool overLimit = limit > 0 && count > limit;
            Color previous = GUI.contentColor;

            GUI.contentColor = overLimit
                ? WarningColour
                : new Color(0.65f, 0.65f, 0.68f);

            EditorGUILayout.LabelField(
                limit <= 0
                    ? $"{count} words"
                    : $"{count}/{limit} words",
                EditorStyles.miniLabel);

            GUI.contentColor = previous;
        }
    }
}
