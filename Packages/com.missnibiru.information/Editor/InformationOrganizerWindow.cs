using System.Collections.Generic;
using MissNibiru.Information.Data;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Information.Editor
{
    public sealed partial class InformationOrganizerWindow :
        EditorWindow
    {
        private enum EntrySortMode
        {
            NameAscending,
            NameDescending,
            IdAscending,
            IdDescending,
            TypeAscending,
            TypeDescending,
            CategoryAscending,
            CategoryDescending
        }

        private enum OrganizerView
        {
            Edit,
            Preview
        }

        private enum WorkspaceView
        {
            Organizer,
            Faq
        }

        private enum PageAction
        {
            None,
            Add,
            MoveUp,
            MoveDown,
            Delete
        }

        private const float MinimumWindowWidth = 820f;
        private const float MinimumWindowHeight = 540f;
        private const float MinimumBrowserWidth = 310f;
        private const float MaximumBrowserWidth = 410f;
        private const string SummaryLimitPreference =
            "MissNibiru.InformationOrganizer.SummaryLimit";

        private const string PageLimitPreference =
            "MissNibiru.InformationOrganizer.PageLimit";

        private static readonly Color HeaderColour =
            new Color(0.12f, 0.10f, 0.18f);

        private static readonly Color AccentColour =
            new Color(0.69f, 0.42f, 0.92f);

        private static readonly Color SuccessColour =
            new Color(0.38f, 0.78f, 0.48f);

        private static readonly Color WarningColour =
            new Color(1f, 0.68f, 0.25f);

        private static readonly Color ErrorColour =
            new Color(1f, 0.38f, 0.35f);

        private static readonly string[] SortLabels =
        {
            "Name: A-Z",
            "Name: Z-A",
            "ID: A-Z",
            "ID: Z-A",
            "Type: A-Z",
            "Type: Z-A",
            "Category: A-Z",
            "Category: Z-A"
        };

        [SerializeField]
        private InformationDatabase database;

        [SerializeField]
        private EntrySortMode sortMode;

        [SerializeField]
        private OrganizerView organizerView;

        [SerializeField]
        private WorkspaceView workspaceView;

        [SerializeField, Min(0)]
        private int summaryWordLimit = 40;

        [SerializeField, Min(0)]
        private int pageWordLimit = 150;

        [SerializeField]
        private bool validationExpanded = true;

        private readonly List<InformationEntry>
            visibleEntries =
                new List<InformationEntry>();

        private readonly List<InformationType>
            typeOptions =
                new List<InformationType>();

        private readonly List<InformationCategory>
            categoryOptions =
                new List<InformationCategory>();

        private readonly List<InformationOrganizerIssue>
            validationIssues =
                new List<InformationOrganizerIssue>();

        private InformationEntry selectedEntry;
        private InformationType selectedType;
        private InformationCategory selectedCategory;
        private string searchQuery = string.Empty;

        private Vector2 browserScroll;
        private Vector2 editorScroll;
        private Vector2 faqScroll;

        private string statusMessage =
            "Select or create a database.";

        private InformationOrganizerSeverity statusSeverity =
            InformationOrganizerSeverity.Warning;

        private PageAction requestedPageAction;
        private int requestedPageIndex = -1;

        private GUIStyle headerTitleStyle;
        private GUIStyle headerSubtitleStyle;
        private GUIStyle entryButtonStyle;
        private GUIStyle selectedEntryButtonStyle;
        private GUIStyle wrapLabelStyle;
        private GUIStyle previewTitleStyle;

        [MenuItem(
            "Tools/Miss Nibiru/Information Organizer")]
        public static void Open()
        {
            InformationOrganizerWindow window =
                GetWindow<InformationOrganizerWindow>();

            window.titleContent =
                new GUIContent("Information Organizer");

            window.minSize =
                new Vector2(
                    MinimumWindowWidth,
                    MinimumWindowHeight);

            window.Show();
        }

        private void OnEnable()
        {
            summaryWordLimit = EditorPrefs.GetInt(
                SummaryLimitPreference,
                summaryWordLimit);

            pageWordLimit = EditorPrefs.GetInt(
                PageLimitPreference,
                pageWordLimit);

            Undo.undoRedoPerformed += HandleUndoRedo;
            RebuildBrowser();
            RefreshValidation();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();

            if (workspaceView == WorkspaceView.Faq)
            {
                DrawFaqPage();
                DrawStatusBar();
                return;
            }

            DrawDatabaseToolbar();

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                float browserWidth = Mathf.Clamp(
                    position.width * 0.36f,
                    MinimumBrowserWidth,
                    MaximumBrowserWidth);

                DrawBrowser(browserWidth);
                DrawSelectedPanel();
            }

            DrawStatusBar();
        }

        private void DrawHeader()
        {
            Rect header = GUILayoutUtility.GetRect(
                0f,
                66f,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(header, HeaderColour);

            Rect title = new Rect(
                header.x + 16f,
                header.y + 10f,
                header.width - 150f,
                26f);

            Rect subtitle = new Rect(
                header.x + 16f,
                header.y + 38f,
                header.width - 150f,
                20f);

            Rect faqButton = new Rect(
                header.xMax - 118f,
                header.y + 18f,
                102f,
                30f);

            GUI.Label(
                title,
                "Information Organizer",
                headerTitleStyle);

            GUI.Label(
                subtitle,
                "Create, classify and validate reusable content.",
                headerSubtitleStyle);

            string faqLabel = workspaceView == WorkspaceView.Faq
                ? "Back to Entries"
                : "?  FAQ";

            string faqTooltip = workspaceView == WorkspaceView.Faq
                ? "Return to your entries."
                : "Learn what each part does.";

            if (GUI.Button(
                    faqButton,
                    new GUIContent(faqLabel, faqTooltip)))
            {
                workspaceView = workspaceView == WorkspaceView.Faq
                    ? WorkspaceView.Organizer
                    : WorkspaceView.Faq;

                GUI.FocusControl(null);
                Repaint();
            }
        }

        private void DrawDatabaseToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                InformationDatabase chosen =
                    (InformationDatabase)
                    EditorGUILayout.ObjectField(
                        new GUIContent(
                            "Database",
                            "Master list used by the game."),
                        database,
                        typeof(InformationDatabase),
                        false);

                if (EditorGUI.EndChangeCheck())
                    SetDatabase(chosen);

                if (GUILayout.Button(
                        new GUIContent(
                            "New Database",
                            "Create a master entry list."),
                        GUILayout.Width(100f)))
                {
                    CreateDatabase();
                }

                using (new EditorGUI.DisabledScope(
                           database == null))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "Locate",
                                "Show the database asset."),
                            GUILayout.Width(58f)))
                    {
                        PingObject(database);
                    }

                    if (GUILayout.Button(
                            new GUIContent(
                                "Validate",
                                "Check the selected database."),
                            GUILayout.Width(66f)))
                    {
                        RefreshValidation();
                        SetValidationStatus();
                    }

                    if (GUILayout.Button(
                            new GUIContent(
                                "Save",
                                "Save all modified assets."),
                            GUILayout.Width(48f)))
                    {
                        SaveAll();
                    }
                }
            }
        }

        private void DrawBrowser(
            float width)
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox,
                       GUILayout.Width(width),
                       GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        "Entries",
                        EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();

                    string count = database == null
                        ? "0"
                        : $"{visibleEntries.Count}/" +
                          database.Entries.Count;

                    Color previous = GUI.contentColor;
                    GUI.contentColor = AccentColour;
                    GUILayout.Label(count);
                    GUI.contentColor = previous;
                }

                EditorGUI.BeginChangeCheck();

                searchQuery = EditorGUILayout.TextField(
                    new GUIContent(
                        "Search",
                        "Search name or ID."),
                    searchQuery);

                if (EditorGUI.EndChangeCheck())
                    RebuildBrowser();

                DrawFilters();

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(
                               database == null))
                    {
                        if (GUILayout.Button(
                                new GUIContent(
                                    "New Entry",
                                    "Create and register an entry.")))
                        {
                            CreateEntry();
                        }
                    }

                    using (new EditorGUI.DisabledScope(
                               selectedEntry == null))
                    {
                        if (GUILayout.Button(
                                new GUIContent(
                                    "Duplicate",
                                    "Copy the selected entry.")))
                        {
                            DuplicateEntry();
                        }

                        if (GUILayout.Button(
                                new GUIContent(
                                    "Delete",
                                    "Move entry to Trash.")))
                        {
                            DeleteSelectedEntry();
                        }
                    }
                }

                EditorGUILayout.Space(3f);

                using (EditorGUILayout.ScrollViewScope scroll =
                       new EditorGUILayout.ScrollViewScope(
                           browserScroll,
                           GUILayout.ExpandHeight(true)))
                {
                    browserScroll = scroll.scrollPosition;

                    if (database == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Select or create a database.",
                            MessageType.Info);
                    }
                    else if (visibleEntries.Count == 0)
                    {
                        EditorGUILayout.HelpBox(
                            database.Entries.Count == 0
                                ? "This database has no entries."
                                : "No entries match these filters.",
                            MessageType.Info);
                    }
                    else
                    {
                        foreach (
                            InformationEntry entry in visibleEntries)
                        {
                            DrawEntryButton(entry);
                        }
                    }
                }
            }
        }

        private void DrawFilters()
        {
            string[] typeLabels = BuildTypeLabels();
            string[] categoryLabels = BuildCategoryLabels();

            int typeIndex = selectedType == null
                ? 0
                : typeOptions.IndexOf(selectedType) + 1;

            int categoryIndex = selectedCategory == null
                ? 0
                : categoryOptions.IndexOf(
                      selectedCategory) + 1;

            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                typeIndex = EditorGUILayout.Popup(
                    typeIndex,
                    typeLabels);

                categoryIndex = EditorGUILayout.Popup(
                    categoryIndex,
                    categoryLabels);
            }

            int sortIndex = EditorGUILayout.Popup(
                new GUIContent(
                    "Sort",
                    "Choose the entry order."),
                (int)sortMode,
                SortLabels);

            if (!EditorGUI.EndChangeCheck())
                return;

            selectedType = typeIndex <= 0
                ? null
                : typeOptions[typeIndex - 1];

            selectedCategory = categoryIndex <= 0
                ? null
                : categoryOptions[categoryIndex - 1];

            sortMode = (EntrySortMode)sortIndex;
            RebuildBrowser();
        }

        private void DrawEntryButton(
            InformationEntry entry)
        {
            if (entry == null)
            {
                EditorGUILayout.HelpBox(
                    "Missing entry reference.",
                    MessageType.Error);

                return;
            }

            string name =
                InformationOrganizerUtility.DisplayName(entry);

            string identity =
                string.IsNullOrWhiteSpace(entry.Id)
                    ? "Missing ID"
                    : entry.Id;

            string classification =
                InformationOrganizerUtility.DisplayType(entry) +
                " / " +
                InformationOrganizerUtility
                    .DisplayCategory(entry);

            GUIContent content = new GUIContent(
                $"{name}\n{identity}  •  {classification}",
                "Open this entry.");

            GUIStyle style = entry == selectedEntry
                ? selectedEntryButtonStyle
                : entryButtonStyle;

            if (GUILayout.Button(content, style))
                SelectEntry(entry);
        }

        private void DrawSelectedPanel()
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox,
                       GUILayout.ExpandWidth(true),
                       GUILayout.ExpandHeight(true)))
            {
                DrawViewToolbar();

                using (EditorGUILayout.ScrollViewScope scroll =
                       new EditorGUILayout.ScrollViewScope(
                           editorScroll,
                           GUILayout.ExpandHeight(true)))
                {
                    editorScroll = scroll.scrollPosition;

                    if (selectedEntry == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Select an entry to edit or preview it.",
                            MessageType.Info);
                    }
                    else if (organizerView == OrganizerView.Edit)
                    {
                        DrawSelectedEntryEditor();
                    }
                    else
                    {
                        DrawSelectedEntryPreview();
                    }

                    EditorGUILayout.Space(10f);
                    DrawValidationPanel();
                }
            }
        }

        private void DrawViewToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.toolbar))
            {
                int selectedView = organizerView ==
                                   OrganizerView.Edit
                    ? 0
                    : 1;

                int chosenView = GUILayout.Toolbar(
                    selectedView,
                    new[]
                    {
                        new GUIContent(
                            "Edit Fields",
                            "Change the selected entry."),
                        new GUIContent(
                            "Preview Entry",
                            "See the player-facing content.")
                    },
                    EditorStyles.toolbarButton,
                    GUILayout.Width(190f));

                organizerView = chosenView == 0
                    ? OrganizerView.Edit
                    : OrganizerView.Preview;

                GUILayout.FlexibleSpace();

                if (selectedEntry != null &&
                    GUILayout.Button(
                        new GUIContent(
                            "Locate Asset",
                            "Show the selected asset."),
                        EditorStyles.toolbarButton))
                {
                    PingObject(selectedEntry);
                }
            }
        }

        private void DrawValidationPanel()
        {
            int errors = CountIssues(
                InformationOrganizerSeverity.Error);

            int warnings = CountIssues(
                InformationOrganizerSeverity.Warning);

            string label = errors == 0 && warnings == 0
                ? "Validation: Ready"
                : $"Validation: {errors} errors, " +
                  $"{warnings} warnings";

            validationExpanded = EditorGUILayout.Foldout(
                validationExpanded,
                label,
                true,
                EditorStyles.foldoutHeader);

            if (!validationExpanded)
                return;

            if (database == null)
            {
                DrawIssue(
                    InformationOrganizerSeverity.Warning,
                    "Select a database first.",
                    null);

                return;
            }

            if (validationIssues.Count == 0)
            {
                DrawIssue(
                    InformationOrganizerSeverity.Success,
                    "Database is valid.",
                    database);

                return;
            }

            foreach (
                InformationOrganizerIssue issue in
                    validationIssues)
            {
                DrawIssue(
                    issue.Severity,
                    issue.Message,
                    issue.Context);
            }
        }

        private void DrawIssue(
            InformationOrganizerSeverity severity,
            string message,
            Object context)
        {
            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.helpBox))
            {
                Color previous = GUI.contentColor;
                GUI.contentColor = ColourFor(severity);

                GUILayout.Label(
                    SymbolFor(severity),
                    GUILayout.Width(18f));

                GUI.contentColor = previous;

                GUILayout.Label(
                    message,
                    wrapLabelStyle,
                    GUILayout.ExpandWidth(true));

                if (context != null &&
                    GUILayout.Button(
                        new GUIContent(
                            "Locate",
                            "Show the problem asset."),
                        GUILayout.Width(52f)))
                {
                    PingObject(context);
                }
            }
        }

        private void DrawStatusBar()
        {
            bool dirty =
                database != null &&
                    EditorUtility.IsDirty(database) ||
                selectedEntry != null &&
                    EditorUtility.IsDirty(selectedEntry);

            InformationOrganizerSeverity severity = dirty
                ? InformationOrganizerSeverity.Warning
                : statusSeverity;

            string message = dirty
                ? "Unsaved changes."
                : statusMessage;

            Rect line = GUILayoutUtility.GetRect(
                0f,
                22f,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(
                line,
                new Color(0.10f, 0.10f, 0.11f));

            Color previous = GUI.contentColor;
            GUI.contentColor = ColourFor(severity);

            GUI.Label(
                new Rect(
                    line.x + 8f,
                    line.y + 2f,
                    line.width - 16f,
                    18f),
                $"{SymbolFor(severity)}  {message}",
                EditorStyles.miniLabel);

            GUI.contentColor = previous;
        }

        private int CountIssues(
            InformationOrganizerSeverity severity)
        {
            int count = 0;

            foreach (
                InformationOrganizerIssue issue in
                    validationIssues)
            {
                if (issue.Severity == severity)
                    count++;
            }

            return count;
        }

        private void EnsureStyles()
        {
            if (headerTitleStyle != null)
                return;

            headerTitleStyle =
                new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 20
                };

            headerTitleStyle.normal.textColor = Color.white;

            headerSubtitleStyle =
                new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11
                };

            headerSubtitleStyle.normal.textColor =
                new Color(0.78f, 0.74f, 0.84f);

            entryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 58f,
                wordWrap = true,
                fontSize = 10,
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(2, 2, 2, 3)
            };

            selectedEntryButtonStyle =
                new GUIStyle(entryButtonStyle);

            selectedEntryButtonStyle.fontStyle = FontStyle.Bold;
            selectedEntryButtonStyle.normal.textColor =
                AccentColour;

            wrapLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            previewTitleStyle =
                new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 22,
                    wordWrap = true
                };
        }

        private static Color ColourFor(
            InformationOrganizerSeverity severity)
        {
            switch (severity)
            {
                case InformationOrganizerSeverity.Success:
                    return SuccessColour;

                case InformationOrganizerSeverity.Error:
                    return ErrorColour;

                default:
                    return WarningColour;
            }
        }

        private static string SymbolFor(
            InformationOrganizerSeverity severity)
        {
            switch (severity)
            {
                case InformationOrganizerSeverity.Success:
                    return "✓";

                case InformationOrganizerSeverity.Error:
                    return "×";

                default:
                    return "!";
            }
        }
    }
}
