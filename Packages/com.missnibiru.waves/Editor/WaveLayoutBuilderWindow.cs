using System;
using System.Collections.Generic;
using MissNibiru.Waves.Execution;
using MissNibiru.Waves.Layouts;
using MissNibiru.Waves.Planning;
using MissNibiru.Waves.Spawning;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Waves.Editor
{
    public sealed class WaveLayoutBuilderWindow : EditorWindow
    {
        private enum Page
        {
            Builder,
            EditSpawnable,
            FAQ
        }

        private enum PalettePage
        {
            Spawnables,
            Formations
        }

        private const float LeftWidth = 235f;
        private const float RightWidth = 305f;
        private const float GridPadding = 24f;
        private const string SpawnableDragKey =
            "MissNibiru.Waves.Spawnable";
        private const string FormationDragKey =
            "MissNibiru.Waves.Formation";
        private const string BrandBannerPath =
            "Packages/com.missnibiru.core/Editor/Branding/" +
            "NibiruMainBanner.png";

        private static readonly Color HeaderColour =
            new Color(0.11f, 0.075f, 0.17f);

        private static readonly Color AccentColour =
            new Color(0.72f, 0.32f, 0.95f);

        private static readonly Color ValidColour =
            new Color(0.18f, 0.78f, 0.42f, 0.58f);

        private static readonly Color ErrorColour =
            new Color(0.92f, 0.24f, 0.28f, 0.68f);

        private static readonly Color CellColour =
            new Color(0.31f, 0.34f, 0.39f, 0.75f);

        [SerializeField]
        private WaveLayoutData layout;

        [SerializeField]
        private WaveRunner runner;

        [SerializeField]
        private Transform previewOrigin;

        [SerializeField]
        private MonoBehaviour previewSpawner;

        [SerializeField]
        private Page page;

        [SerializeField]
        private PalettePage palettePage;

        [SerializeField]
        private string search = string.Empty;

        [SerializeField]
        private int kindFilter;

        [SerializeField]
        private int currentWaveIndex;

        [SerializeField]
        private float zoom = 1f;

        [SerializeField]
        private Vector2 pan;

        [SerializeField]
        private SpawnableDefinition selectedPaletteSpawnable;

        [SerializeField]
        private SpawnFormationDefinition selectedFormation;

        private readonly HashSet<string> _selectedIds =
            new HashSet<string>();

        private readonly Dictionary<string, Vector2Int>
            _moveStartCells =
                new Dictionary<string, Vector2Int>();

        private readonly List<WaveLayoutValidationIssue>
            _validationIssues =
                new List<WaveLayoutValidationIssue>();

        private Vector2 _paletteScroll;
        private Vector2 _inspectorScroll;
        private Vector2 _spawnableEditorScroll;
        private Vector2 _faqScroll;
        private Vector2Int _moveStartCell;
        private bool _moving;
        private bool _panning;
        private bool _gridFocused;
        private string _primarySelectedId;
        private string _status = "Ready.";
        private WaveLayoutValidationSeverity _statusSeverity =
            WaveLayoutValidationSeverity.Success;

        private GUIStyle _headerTitle;
        private GUIStyle _headerSubtitle;
        private GUIStyle _paletteItem;
        private GUIStyle _wrap;
        private GUIStyle _centeredMini;
        private Texture2D _brandBanner;

        public static WaveLayoutBuilderWindow ActiveWindow
        {
            get;
            private set;
        }

        public WaveLayoutData ActiveLayout => layout;
        public int ActiveWaveIndex => currentWaveIndex;
        public Transform ActiveOrigin => previewOrigin;

        [MenuItem("Tools/Miss Nibiru/Wave Layout Builder")]
        public static void Open()
        {
            WaveLayoutBuilderWindow window =
                GetWindow<WaveLayoutBuilderWindow>();

            window.titleContent = new GUIContent(
                "Wave Layout Builder");
            window.minSize = new Vector2(980f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            ActiveWindow = this;
            Undo.undoRedoPerformed += HandleUndoRedo;
            _brandBanner = AssetDatabase.LoadAssetAtPath<Texture2D>(
                BrandBannerPath);

            if (runner != null)
            {
                previewOrigin = runner.AuthoredLayoutOrigin;
                previewSpawner = runner.SpawnerSource != null
                    ? runner.SpawnerSource
                    : runner.GetComponent<WaveSpawner>();

                if (layout == null)
                    layout = runner.AuthoredLayout;
            }
        }

        private void OnFocus()
        {
            ActiveWindow = this;
            SceneView.RepaintAll();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;

            if (ActiveWindow == this)
                ActiveWindow = null;

            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();

            if (page == Page.FAQ)
                DrawFaq();
            else if (page == Page.EditSpawnable)
                DrawSpawnableEditor();
            else
                DrawBuilder();

            DrawStatusBar();
        }

        private void DrawHeader()
        {
            Rect header = GUILayoutUtility.GetRect(
                0f,
                104f,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(header, HeaderColour);

            float bannerWidth = Mathf.Clamp(
                header.width * 0.30f,
                220f,
                310f);

            Rect banner = new Rect(
                header.x + 8f,
                header.y + 4f,
                bannerWidth,
                96f);

            if (_brandBanner != null)
            {
                GUI.DrawTexture(
                    banner,
                    _brandBanner,
                    ScaleMode.ScaleToFit,
                    true);
            }

            float titleX = _brandBanner == null
                ? header.x + 16f
                : banner.xMax + 14f;

            Rect tabs = new Rect(
                header.xMax - 284f,
                header.y + 39f,
                268f,
                27f);

            float titleWidth = Mathf.Max(
                90f,
                tabs.x - titleX - 10f);

            GUI.Label(
                new Rect(
                    titleX,
                    header.y + 23f,
                    titleWidth,
                    28f),
                "Wave Layout Builder",
                _headerTitle);

            GUI.Label(
                new Rect(
                    titleX + 1f,
                    header.y + 55f,
                    titleWidth,
                    20f),
                "Design reusable spawn encounters.",
                _headerSubtitle);

            page = (Page)GUI.Toolbar(
                tabs,
                (int)page,
                new[]
                {
                    "Builder",
                    SpawnableEditorTabLabel(),
                    "FAQ"
                });
        }

        private void DrawBuilder()
        {
            DrawAssetToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(
                           GUILayout.Width(LeftWidth)))
                {
                    DrawPalette();
                }

                DrawDivider();

                using (new EditorGUILayout.VerticalScope(
                           GUILayout.ExpandWidth(true),
                           GUILayout.ExpandHeight(true)))
                {
                    DrawWaveTabs();
                    DrawGridToolbar();
                    DrawGrid();
                }

                DrawDivider();

                using (new EditorGUILayout.VerticalScope(
                           GUILayout.Width(RightWidth)))
                {
                    DrawInspector();
                }
            }
        }

        private void DrawSpawnableEditor()
        {
            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(
                       _spawnableEditorScroll,
                       GUILayout.ExpandHeight(true)))
            {
                _spawnableEditorScroll = scroll.scrollPosition;
                EditorGUILayout.Space(10f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    float editorWidth = Mathf.Min(
                        720f,
                        Mathf.Max(420f, position.width - 48f));

                    using (new EditorGUILayout.VerticalScope(
                               EditorStyles.helpBox,
                               GUILayout.Width(editorWidth)))
                    {
                        GUILayout.Label(
                            SpawnableEditorHeading(),
                            _headerTitle);

                        SpawnableDefinition chosen =
                            (SpawnableDefinition)
                            EditorGUILayout.ObjectField(
                                new GUIContent(
                                    "Asset",
                                    "Select a spawnable."),
                                selectedPaletteSpawnable,
                                typeof(SpawnableDefinition),
                                false);

                        if (chosen != selectedPaletteSpawnable)
                        {
                            selectedPaletteSpawnable = chosen;

                            if (chosen != null)
                                Selection.activeObject = chosen;
                        }

                        if (selectedPaletteSpawnable == null)
                        {
                            EditorGUILayout.HelpBox(
                                "Select or create a spawnable in Builder.",
                                MessageType.Info);

                            if (GUILayout.Button("Back to Builder"))
                                page = Page.Builder;

                            GUILayout.FlexibleSpace();
                            return;
                        }

                        EditorGUILayout.Space(4f);
                        DrawSpawnablePreview(selectedPaletteSpawnable);
                        EditorGUILayout.Space(6f);

                        SerializedObject serialized =
                            new SerializedObject(
                                selectedPaletteSpawnable);

                        serialized.Update();

                        EditorGUILayout.PropertyField(
                            serialized.FindProperty("displayName"),
                            new GUIContent(
                                "Display Name",
                                "Name shown in tools."));

                        EditorGUILayout.PropertyField(
                            serialized.FindProperty("prefab"),
                            new GUIContent(
                                "Prefab",
                                "Spawned at runtime."));

                        EditorGUILayout.PropertyField(
                            serialized.FindProperty("icon"),
                            new GUIContent(
                                "Icon",
                                "Shown on grid."));

                        EditorGUILayout.PropertyField(
                            serialized.FindProperty("kind"),
                            new GUIContent(
                                "Kind",
                                "Used by filters."));

                        EditorGUILayout.PropertyField(
                            serialized.FindProperty("tags"),
                            new GUIContent(
                                "Tags",
                                "Labels for search."),
                            true);

                        EditorGUILayout.PropertyField(
                            serialized.FindProperty("gridFootprint"),
                            new GUIContent(
                                "Grid Footprint",
                                "Cells occupied."));

                        EditorGUILayout.PropertyField(
                            serialized.FindProperty("footprintPivot"),
                            new GUIContent(
                                "Footprint Pivot",
                                "Anchor cell."));

                        if (serialized.ApplyModifiedProperties())
                        {
                            EditorUtility.SetDirty(
                                selectedPaletteSpawnable);
                            SetStatus(
                                "Spawnable updated.",
                                WaveLayoutValidationSeverity.Success);
                            SceneView.RepaintAll();
                        }

                        if (selectedPaletteSpawnable.Prefab == null)
                        {
                            EditorGUILayout.HelpBox(
                                "Assign the prefab spawned at runtime.",
                                MessageType.Warning);
                        }

                        EditorGUILayout.Space(6f);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUI.enabled =
                                selectedPaletteSpawnable.Prefab != null;

                            if (GUILayout.Button(
                                    new GUIContent(
                                        "Open Prefab",
                                        "Edit prefab components.")))
                            {
                                AssetDatabase.OpenAsset(
                                    selectedPaletteSpawnable.Prefab);
                            }

                            GUI.enabled = true;

                            if (GUILayout.Button("Locate Asset"))
                            {
                                Selection.activeObject =
                                    selectedPaletteSpawnable;
                                EditorGUIUtility.PingObject(
                                    selectedPaletteSpawnable);
                            }

                            if (GUILayout.Button("Save"))
                            {
                                EditorUtility.SetDirty(
                                    selectedPaletteSpawnable);
                                AssetDatabase.SaveAssets();
                                SetStatus(
                                    "Spawnable saved.",
                                    WaveLayoutValidationSeverity.Success);
                            }
                        }

                        if (GUILayout.Button("Back to Builder"))
                            page = Page.Builder;
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void DrawSpawnablePreview(
            SpawnableDefinition spawnable)
        {
            Texture preview = spawnable.Icon == null
                ? null
                : spawnable.Icon.texture;

            if (preview == null && spawnable.Prefab != null)
            {
                preview = AssetPreview.GetAssetPreview(
                    spawnable.Prefab);

                if (preview == null)
                    preview = AssetPreview.GetMiniThumbnail(
                        spawnable.Prefab);

                if (AssetPreview.IsLoadingAssetPreview(
                        spawnable.Prefab.GetInstanceID()))
                {
                    Repaint();
                }
            }

            Rect previewRect = GUILayoutUtility.GetRect(
                0f,
                128f,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(
                previewRect,
                new Color(0.08f, 0.06f, 0.11f));

            if (preview != null)
            {
                GUI.DrawTexture(
                    previewRect,
                    preview,
                    ScaleMode.ScaleToFit,
                    true);
            }
            else
            {
                GUI.Label(
                    previewRect,
                    "No preview",
                    _centeredMini);
            }
        }

        private string SpawnableEditorTabLabel()
        {
            return "Edit " + SpawnableKindLabel();
        }

        private string SpawnableEditorHeading()
        {
            if (selectedPaletteSpawnable == null)
                return "Edit Spawnable";

            return SpawnableEditorTabLabel() + ": " +
                   selectedPaletteSpawnable.DisplayName;
        }

        private string SpawnableKindLabel()
        {
            if (selectedPaletteSpawnable == null)
                return "Spawnable";

            switch (selectedPaletteSpawnable.Kind)
            {
                case SpawnableKind.Enemy:
                    return "Enemy";
                case SpawnableKind.Hazard:
                    return "Hazard";
                case SpawnableKind.Pickup:
                    return "Pickup";
                default:
                    return "Spawnable";
            }
        }

        private void DrawAssetToolbar()
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    WaveLayoutData nextLayout =
                        (WaveLayoutData)EditorGUILayout.ObjectField(
                            new GUIContent(
                                "Layout",
                                "Runtime sequence asset."),
                            layout,
                            typeof(WaveLayoutData),
                            false);

                    if (nextLayout != layout)
                        SetLayout(nextLayout);

                    if (GUILayout.Button("New", GUILayout.Width(48f)))
                        CreateLayout();

                    if (GUILayout.Button(
                            new GUIContent("Save", "Save asset changes."),
                            GUILayout.Width(48f)))
                    {
                        WaveLayoutEditorUtility.Save(layout);
                        SetStatus(
                            "Layout saved.",
                            WaveLayoutValidationSeverity.Success);
                    }

                    if (GUILayout.Button(
                            new GUIContent(
                                "Validate",
                                "Check layout problems."),
                            GUILayout.Width(68f)))
                    {
                        RunValidation();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    WaveRunner nextRunner =
                        (WaveRunner)EditorGUILayout.ObjectField(
                            new GUIContent(
                                "Runner",
                                "Scene runtime controller."),
                            runner,
                            typeof(WaveRunner),
                            true);

                    if (nextRunner != runner)
                        SetRunner(nextRunner);

                    Transform nextOrigin =
                        (Transform)EditorGUILayout.ObjectField(
                            new GUIContent(
                                "Origin",
                                "Grid world anchor."),
                            previewOrigin,
                            typeof(Transform),
                            true);

                    if (nextOrigin != previewOrigin)
                        SetOrigin(nextOrigin);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    MonoBehaviour nextSpawner =
                        (MonoBehaviour)EditorGUILayout.ObjectField(
                            new GUIContent(
                                "Spawner",
                                "Implements IWaveSpawner."),
                            previewSpawner,
                            typeof(MonoBehaviour),
                            true);

                    if (nextSpawner != previewSpawner)
                        SetSpawner(nextSpawner);
                }
            }
        }

        private void DrawPalette()
        {
            GUILayout.Label("Palette", EditorStyles.boldLabel);

            SpawnCatalog catalog =
                layout == null ? null : layout.Catalog;

            SpawnCatalog nextCatalog =
                (SpawnCatalog)EditorGUILayout.ObjectField(
                    "Catalog",
                    catalog,
                    typeof(SpawnCatalog),
                    false);

            if (nextCatalog != catalog && layout != null)
            {
                RecordLayout("Change spawn catalog");
                layout.Catalog = nextCatalog;
                MarkLayoutDirty();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Catalog"))
                    CreateCatalog();

                if (GUILayout.Button("Add Asset"))
                {
                    if (palettePage == PalettePage.Spawnables)
                        CreateSpawnable();
                    else
                        CreateFormation();
                }
            }

            palettePage = (PalettePage)GUILayout.Toolbar(
                (int)palettePage,
                new[] { "Spawnables", "Formations" });

            search = EditorGUILayout.TextField(
                new GUIContent("Search"),
                search);

            if (palettePage == PalettePage.Spawnables)
            {
                string[] filters =
                {
                    "All",
                    "Enemies",
                    "Hazards",
                    "Pickups",
                    "Other"
                };

                kindFilter = EditorGUILayout.Popup(
                    "Filter",
                    kindFilter,
                    filters);

                if (selectedPaletteSpawnable != null &&
                    GUILayout.Button(
                        new GUIContent(
                            SpawnableEditorTabLabel(),
                            "Edit selected asset.")))
                {
                    page = Page.EditSpawnable;
                }
            }

            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(
                       _paletteScroll,
                       GUILayout.ExpandHeight(true)))
            {
                _paletteScroll = scroll.scrollPosition;

                if (catalog == null)
                {
                    EditorGUILayout.HelpBox(
                        "Create or assign a catalog.",
                        MessageType.Info);
                    return;
                }

                if (palettePage == PalettePage.Spawnables)
                    DrawSpawnablePalette(catalog);
                else
                    DrawFormationPalette(catalog);
            }
        }

        private void DrawSpawnablePalette(SpawnCatalog catalog)
        {
            foreach (SpawnableDefinition spawnable in catalog.Spawnables)
            {
                if (spawnable == null ||
                    !Matches(spawnable.DisplayName) ||
                    !MatchesKind(spawnable.Kind))
                {
                    continue;
                }

                Rect rect = GUILayoutUtility.GetRect(
                    0f,
                    48f,
                    GUILayout.ExpandWidth(true));

                DrawPaletteBox(
                    rect,
                    spawnable.DisplayName,
                    spawnable.Kind.ToString(),
                    spawnable.Icon,
                    spawnable == selectedPaletteSpawnable);

                Event current = Event.current;

                if (current.type == EventType.MouseDown &&
                    current.button == 0 &&
                    rect.Contains(current.mousePosition))
                {
                    selectedPaletteSpawnable = spawnable;
                    Selection.activeObject = spawnable;

                    if (current.clickCount > 1)
                        page = Page.EditSpawnable;

                    current.Use();
                    Repaint();
                }

                if (current.type == EventType.MouseDrag &&
                    rect.Contains(current.mousePosition))
                {
                    selectedPaletteSpawnable = spawnable;
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences =
                        new UnityEngine.Object[] { spawnable };
                    DragAndDrop.SetGenericData(
                        SpawnableDragKey,
                        spawnable);
                    DragAndDrop.StartDrag(spawnable.DisplayName);
                    current.Use();
                }
            }
        }

        private void DrawFormationPalette(SpawnCatalog catalog)
        {
            if (selectedPaletteSpawnable == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a spawnable first.",
                    MessageType.Info);
            }
            else
            {
                GUILayout.Label(
                    $"Using: {selectedPaletteSpawnable.DisplayName}",
                    EditorStyles.miniLabel);
            }

            foreach (
                SpawnFormationDefinition formation
                in catalog.Formations)
            {
                if (formation == null ||
                    !Matches(formation.DisplayName))
                {
                    continue;
                }

                int count = formation.CellOffsets == null
                    ? 0
                    : formation.CellOffsets.Length;

                Rect rect = GUILayoutUtility.GetRect(
                    0f,
                    44f,
                    GUILayout.ExpandWidth(true));

                DrawPaletteBox(
                    rect,
                    formation.DisplayName,
                    $"{count} spawn points",
                    null,
                    formation == selectedFormation);

                Event current = Event.current;

                if (current.type == EventType.MouseDown &&
                    current.button == 0 &&
                    rect.Contains(current.mousePosition))
                {
                    selectedFormation = formation;
                    Selection.activeObject = formation;

                    if (current.clickCount > 1)
                    {
                        SpawnFormationDesignerWindow.Open(
                            formation);
                    }

                    current.Use();
                    Repaint();
                }

                if (current.type == EventType.MouseDrag &&
                    rect.Contains(current.mousePosition))
                {
                    selectedFormation = formation;
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences =
                        new UnityEngine.Object[] { formation };
                    DragAndDrop.SetGenericData(
                        FormationDragKey,
                        formation);
                    DragAndDrop.StartDrag(formation.DisplayName);
                    current.Use();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = selectedFormation != null;

                if (GUILayout.Button("Edit Formation"))
                {
                    SpawnFormationDesignerWindow.Open(
                        selectedFormation);
                }

                GUI.enabled = true;
            }
        }

        private void DrawPaletteBox(
            Rect rect,
            string title,
            string subtitle,
            Sprite icon,
            bool selected)
        {
            Color background = selected
                ? new Color(0.30f, 0.18f, 0.37f)
                : new Color(0.20f, 0.20f, 0.22f);

            EditorGUI.DrawRect(rect, background);

            float textStart = rect.x + 8f;

            if (icon != null && icon.texture != null)
            {
                Rect iconRect = new Rect(
                    rect.x + 4f,
                    rect.y + 4f,
                    38f,
                    38f);

                GUI.DrawTexture(
                    iconRect,
                    icon.texture,
                    ScaleMode.ScaleToFit,
                    true);

                textStart = iconRect.xMax + 6f;
            }

            GUI.Label(
                new Rect(
                    textStart,
                    rect.y + 5f,
                    rect.xMax - textStart - 5f,
                    20f),
                title,
                EditorStyles.boldLabel);

            GUI.Label(
                new Rect(
                    textStart,
                    rect.y + 25f,
                    rect.xMax - textStart - 5f,
                    18f),
                subtitle,
                EditorStyles.miniLabel);
        }

        private void DrawWaveTabs()
        {
            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.toolbar))
            {
                if (layout == null || layout.Waves.Count == 0)
                {
                    GUILayout.Label("No waves", EditorStyles.miniLabel);

                    if (GUILayout.Button(
                            "+",
                            EditorStyles.toolbarButton,
                            GUILayout.Width(28f)))
                    {
                        AddWave();
                    }

                    return;
                }

                ClampWaveIndex();

                for (int index = 0;
                     index < layout.Waves.Count;
                     index++)
                {
                    bool selected = index == currentWaveIndex;
                    GUIStyle style = selected
                        ? EditorStyles.toolbarButton
                        : EditorStyles.toolbarButton;

                    bool next = GUILayout.Toggle(
                        selected,
                        WaveLayoutEditorUtility.DisplayName(
                            layout.Waves[index],
                            index),
                        style,
                        GUILayout.MinWidth(72f),
                        GUILayout.MaxWidth(120f));

                    if (next && !selected)
                    {
                        currentWaveIndex = index;
                        ClearSelection();
                        SceneView.RepaintAll();
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        "+",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(26f)))
                {
                    AddWave();
                }

                if (GUILayout.Button(
                        "⧉",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(28f)))
                {
                    DuplicateWave();
                }

                if (GUILayout.Button(
                        "◀",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(28f)))
                {
                    MoveWave(-1);
                }

                if (GUILayout.Button(
                        "▶",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(28f)))
                {
                    MoveWave(1);
                }

                if (GUILayout.Button(
                        "−",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(26f)))
                {
                    DeleteWave();
                }

                if (GUILayout.Button(
                        "Clear",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(42f)))
                {
                    ClearWave();
                }
            }
        }

        private void DrawGridToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.helpBox))
            {
                GUILayout.Label(
                    layout == null
                        ? "Grid"
                        : $"{layout.Columns} × {layout.Rows}  " +
                          $"{layout.GridPlane}",
                    EditorStyles.miniBoldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("−", GUILayout.Width(26f)))
                    zoom = Mathf.Clamp(zoom - 0.1f, 0.35f, 3f);

                GUILayout.Label(
                    $"{Mathf.RoundToInt(zoom * 100f)}%",
                    _centeredMini,
                    GUILayout.Width(42f));

                if (GUILayout.Button("+", GUILayout.Width(26f)))
                    zoom = Mathf.Clamp(zoom + 0.1f, 0.35f, 3f);

                if (GUILayout.Button(
                        new GUIContent("Fit", "Fit grid view."),
                        GUILayout.Width(34f)))
                {
                    zoom = 1f;
                    pan = Vector2.zero;
                }
            }
        }

        private void DrawGrid()
        {
            Rect viewRect = GUILayoutUtility.GetRect(
                200f,
                200f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            EditorGUI.DrawRect(
                viewRect,
                new Color(0.105f, 0.105f, 0.12f));

            if (layout == null ||
                layout.Columns < 1 ||
                layout.Rows < 1)
            {
                GUI.Label(
                    viewRect,
                    "Create or select a layout.",
                    _centeredMini);
                return;
            }

            float fittedCell = Mathf.Min(
                (viewRect.width - GridPadding * 2f) /
                layout.Columns,
                (viewRect.height - GridPadding * 2f) /
                layout.Rows);

            float cellSize = Mathf.Max(6f, fittedCell * zoom);
            Vector2 gridSize = new Vector2(
                layout.Columns * cellSize,
                layout.Rows * cellSize);

            Vector2 gridOrigin = viewRect.center -
                                 gridSize * 0.5f +
                                 pan;

            Rect gridRect = new Rect(
                gridOrigin.x,
                gridOrigin.y,
                gridSize.x,
                gridSize.y);

            HandleGridInput(
                viewRect,
                gridRect,
                gridOrigin,
                cellSize);

            DrawGridLines(gridRect, gridOrigin, cellSize);
            DrawPlacements(gridOrigin, cellSize);
            DrawDropPreview(gridRect, gridOrigin, cellSize);
            HandleGridShortcuts();
        }

        private void DrawGridLines(
            Rect gridRect,
            Vector2 gridOrigin,
            float cellSize)
        {
            EditorGUI.DrawRect(
                gridRect,
                new Color(0.14f, 0.14f, 0.16f));

            Handles.BeginGUI();
            Handles.color = CellColour;

            for (int column = 0;
                 column <= layout.Columns;
                 column++)
            {
                float x = gridOrigin.x + column * cellSize;
                Handles.DrawLine(
                    new Vector3(x, gridOrigin.y),
                    new Vector3(x, gridOrigin.y +
                                   layout.Rows * cellSize));
            }

            for (int row = 0; row <= layout.Rows; row++)
            {
                float y = gridOrigin.y + row * cellSize;
                Handles.DrawLine(
                    new Vector3(gridOrigin.x, y),
                    new Vector3(gridOrigin.x +
                                   layout.Columns * cellSize, y));
            }

            Handles.EndGUI();

            if (cellSize < 14f)
                return;

            for (int column = 0;
                 column < layout.Columns;
                 column++)
            {
                GUI.Label(
                    new Rect(
                        gridOrigin.x + column * cellSize,
                        gridOrigin.y - 18f,
                        cellSize,
                        16f),
                    column.ToString(),
                    _centeredMini);
            }

            for (int row = 0; row < layout.Rows; row++)
            {
                GUI.Label(
                    new Rect(
                        gridOrigin.x - 22f,
                        gridOrigin.y +
                        (layout.Rows - row - 1) * cellSize,
                        20f,
                        cellSize),
                    row.ToString(),
                    _centeredMini);
            }
        }

        private void DrawPlacements(
            Vector2 gridOrigin,
            float cellSize)
        {
            if (!HasCurrentWave())
                return;

            HashSet<string> invalid = GetInvalidPlacementIds();
            WaveLayoutWave wave = layout.Waves[currentWaveIndex];

            foreach (WaveLayoutPlacement placement in wave.Placements)
            {
                if (placement == null || placement.Spawnable == null)
                    continue;

                bool selected = _selectedIds.Contains(placement.Id);
                Color fill = invalid.Contains(placement.Id)
                    ? ErrorColour
                    : _moving && selected
                        ? ValidColour
                        : selected
                        ? new Color(0.62f, 0.25f, 0.84f, 0.72f)
                        : new Color(0.16f, 0.52f, 0.78f, 0.58f);

                IReadOnlyList<Vector2Int> formationCells =
                    WaveLayoutGeometry.GetFormationCells(placement);

                foreach (Vector2Int formationCell in formationCells)
                {
                    IReadOnlyList<Vector2Int> occupied =
                        WaveLayoutGeometry.GetOccupiedCells(
                            placement.Spawnable,
                            formationCell,
                            placement.Rotation,
                            placement.FlipHorizontal,
                            placement.FlipVertical);

                    foreach (Vector2Int cell in occupied)
                    {
                        Rect cellRect = CellRect(
                            cell,
                            gridOrigin,
                            cellSize);

                        EditorGUI.DrawRect(
                            Shrink(cellRect, 1f),
                            fill);
                    }
                }

                Rect anchor = CellRect(
                    placement.Cell,
                    gridOrigin,
                    cellSize);

                DrawPlacementLabel(anchor, placement, cellSize);
            }
        }

        private void DrawPlacementLabel(
            Rect anchor,
            WaveLayoutPlacement placement,
            float cellSize)
        {
            if (placement.Spawnable.Icon != null &&
                placement.Spawnable.Icon.texture != null)
            {
                GUI.DrawTexture(
                    Shrink(anchor, 3f),
                    placement.Spawnable.Icon.texture,
                    ScaleMode.ScaleToFit,
                    true);
            }
            else if (cellSize >= 24f)
            {
                string display = placement.Spawnable.DisplayName;
                string shortName = string.IsNullOrWhiteSpace(display)
                    ? "?"
                    : display.Substring(0, 1).ToUpperInvariant();

                GUI.Label(anchor, shortName, _centeredMini);
            }
        }

        private void HandleGridInput(
            Rect viewRect,
            Rect gridRect,
            Vector2 gridOrigin,
            float cellSize)
        {
            Event current = Event.current;

            if (!viewRect.Contains(current.mousePosition))
                return;

            if (current.type == EventType.ScrollWheel)
            {
                zoom = Mathf.Clamp(
                    zoom - current.delta.y * 0.03f,
                    0.35f,
                    3f);
                current.Use();
                Repaint();
                return;
            }

            bool panButton = current.button == 2 ||
                             (current.button == 0 && current.alt);

            if (current.type == EventType.MouseDown && panButton)
            {
                _panning = true;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag && _panning)
            {
                pan += current.delta;
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseUp && _panning)
            {
                _panning = false;
                current.Use();
                return;
            }

            if (!gridRect.Contains(current.mousePosition) ||
                !HasCurrentWave())
            {
                return;
            }

            Vector2Int mouseCell = MouseToCell(
                current.mousePosition,
                gridOrigin,
                cellSize);

            if (current.type == EventType.MouseDown &&
                current.button == 0 && !current.alt)
            {
                _gridFocused = true;
                WaveLayoutPlacement hit = FindPlacementAt(
                    current.mousePosition,
                    gridOrigin,
                    cellSize);

                if (hit == null)
                {
                    ClearSelection();
                    current.Use();
                    Repaint();
                    return;
                }

                bool additive =
                    HasActionModifier(current) || current.shift;

                if (!additive && !_selectedIds.Contains(hit.Id))
                    ClearSelection();

                if (additive && _selectedIds.Contains(hit.Id))
                {
                    _selectedIds.Remove(hit.Id);

                    if (_primarySelectedId == hit.Id)
                    {
                        _primarySelectedId = null;

                        foreach (string selectedId in _selectedIds)
                        {
                            _primarySelectedId = selectedId;
                            break;
                        }
                    }
                }
                else
                {
                    _selectedIds.Add(hit.Id);
                    _primarySelectedId = hit.Id;
                }

                BeginMove(mouseCell);
                current.Use();
                Repaint();
            }

            if (current.type == EventType.MouseDrag &&
                current.button == 0 &&
                _moving)
            {
                ApplyMovePreview(mouseCell);
                current.Use();
                Repaint();
            }

            if (current.type == EventType.MouseUp && _moving)
            {
                FinishMove();
                current.Use();
                Repaint();
            }
        }

        private void DrawDropPreview(
            Rect gridRect,
            Vector2 gridOrigin,
            float cellSize)
        {
            Event current = Event.current;

            if (!gridRect.Contains(current.mousePosition) ||
                !HasCurrentWave())
            {
                return;
            }

            SpawnableDefinition draggedSpawnable =
                DragAndDrop.GetGenericData(
                    SpawnableDragKey) as SpawnableDefinition;

            SpawnFormationDefinition draggedFormation =
                DragAndDrop.GetGenericData(
                    FormationDragKey) as
                    SpawnFormationDefinition;

            if (draggedSpawnable == null &&
                draggedFormation == null)
            {
                return;
            }

            SpawnableDefinition spawnable = draggedSpawnable != null
                ? draggedSpawnable
                : selectedPaletteSpawnable;

            Vector2Int cell = MouseToCell(
                current.mousePosition,
                gridOrigin,
                cellSize);

            WaveLayoutPlacement candidate =
                new WaveLayoutPlacement
                {
                    Spawnable = spawnable,
                    Formation = draggedFormation,
                    Cell = cell
                };

            bool valid = spawnable != null &&
                         IsCandidateValid(candidate);

            if (spawnable != null)
            {
                foreach (
                    Vector2Int formationCell
                    in WaveLayoutGeometry.GetFormationCells(candidate))
                {
                    foreach (
                        Vector2Int occupied
                        in WaveLayoutGeometry.GetOccupiedCells(
                            spawnable,
                            formationCell,
                            candidate.Rotation,
                            false,
                            false))
                    {
                        EditorGUI.DrawRect(
                            Shrink(
                                CellRect(
                                    occupied,
                                    gridOrigin,
                                    cellSize),
                                1f),
                            valid ? ValidColour : ErrorColour);
                    }
                }
            }

            if (current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = valid
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Rejected;
                current.Use();
                Repaint();
            }

            if (current.type == EventType.DragPerform)
            {
                if (valid)
                {
                    DragAndDrop.AcceptDrag();
                    AddPlacement(candidate);
                }
                else
                {
                    SetStatus(
                        spawnable == null
                            ? "Select a spawnable first."
                            : "Placement is blocked.",
                        WaveLayoutValidationSeverity.Error);
                }

                DragAndDrop.SetGenericData(SpawnableDragKey, null);
                DragAndDrop.SetGenericData(FormationDragKey, null);
                current.Use();
                Repaint();
            }
        }

        private void DrawInspector()
        {
            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(
                       _inspectorScroll,
                       GUILayout.ExpandHeight(true)))
            {
                _inspectorScroll = scroll.scrollPosition;

                GUILayout.Label("Inspector", EditorStyles.boldLabel);

                if (layout == null)
                {
                    EditorGUILayout.HelpBox(
                        "Select a layout to edit.",
                        MessageType.Info);
                    return;
                }

                DrawGridSettings();

                if (HasCurrentWave())
                {
                    EditorGUILayout.Space(5f);
                    DrawWaveSettings();
                    EditorGUILayout.Space(5f);
                    DrawPlacementSettings();
                    EditorGUILayout.Space(5f);
                    DrawCalculator();
                    EditorGUILayout.Space(5f);
                    DrawValidationResults();
                }
            }
        }

        private void DrawGridSettings()
        {
            GUILayout.Label("Grid Setup", EditorStyles.boldLabel);

            int columns = EditorGUILayout.IntSlider(
                "Columns",
                layout.Columns,
                1,
                WaveLayoutData.MaximumGridSize);

            int rows = EditorGUILayout.IntSlider(
                "Rows",
                layout.Rows,
                1,
                WaveLayoutData.MaximumGridSize);

            WaveGridPlane plane =
                (WaveGridPlane)EditorGUILayout.EnumPopup(
                    "Plane",
                    layout.GridPlane);

            float worldCellSize = EditorGUILayout.FloatField(
                "Cell Units",
                layout.CellSize);

            int budget = EditorGUILayout.IntField(
                "Enemy Budget",
                layout.ActiveEnemyBudget);

            if (columns != layout.Columns ||
                rows != layout.Rows ||
                plane != layout.GridPlane ||
                !Mathf.Approximately(
                    worldCellSize,
                    layout.CellSize) ||
                budget != layout.ActiveEnemyBudget)
            {
                RecordLayout("Edit grid settings");
                layout.Columns = Mathf.Clamp(
                    columns,
                    1,
                    WaveLayoutData.MaximumGridSize);
                layout.Rows = Mathf.Clamp(
                    rows,
                    1,
                    WaveLayoutData.MaximumGridSize);
                layout.GridPlane = plane;
                layout.CellSize = Mathf.Max(0.01f, worldCellSize);
                layout.ActiveEnemyBudget = Mathf.Max(1, budget);
                MarkLayoutDirty();
            }
        }

        private void DrawWaveSettings()
        {
            WaveLayoutWave wave = layout.Waves[currentWaveIndex];
            GUILayout.Label("Wave Settings", EditorStyles.boldLabel);

            string waveName = EditorGUILayout.TextField(
                "Name",
                wave.WaveName);

            float initialDelay = EditorGUILayout.FloatField(
                "Initial Delay",
                wave.InitialDelay);

            bool timed = EditorGUILayout.Toggle(
                "Timed Wave",
                wave.UsesDuration);

            float duration = wave.Duration;

            if (timed)
            {
                duration = EditorGUILayout.FloatField(
                    "Duration",
                    wave.Duration);
            }

            bool waitForClear = EditorGUILayout.Toggle(
                "Wait For Clear",
                wave.WaitForActiveObjectsToClear);

            bool autoProgress = EditorGUILayout.Toggle(
                "Auto Progress",
                wave.AutoProgress);

            bool cleanup = EditorGUILayout.Toggle(
                "Despawn On End",
                wave.DespawnActiveObjectsOnCompletion);

            if (waveName != wave.WaveName ||
                !Mathf.Approximately(initialDelay, wave.InitialDelay) ||
                timed != wave.UsesDuration ||
                !Mathf.Approximately(duration, wave.Duration) ||
                waitForClear !=
                wave.WaitForActiveObjectsToClear ||
                autoProgress != wave.AutoProgress ||
                cleanup !=
                wave.DespawnActiveObjectsOnCompletion)
            {
                RecordLayout("Edit wave settings");
                wave.WaveName = waveName;
                wave.InitialDelay = Mathf.Max(0f, initialDelay);
                wave.UsesDuration = timed;
                wave.Duration = Mathf.Max(0f, duration);
                wave.WaitForActiveObjectsToClear = waitForClear;
                wave.AutoProgress = autoProgress;
                wave.DespawnActiveObjectsOnCompletion = cleanup;
                MarkLayoutDirty();
            }
        }

        private void DrawPlacementSettings()
        {
            GUILayout.Label("Placement", EditorStyles.boldLabel);

            if (_selectedIds.Count == 0)
            {
                if (palettePage == PalettePage.Formations &&
                    selectedFormation != null)
                {
                    DrawSelectedFormation();
                }
                else if (selectedPaletteSpawnable != null)
                {
                    EditorGUILayout.HelpBox(
                        "A palette asset is selected.",
                        MessageType.Info);

                    if (GUILayout.Button(
                            SpawnableEditorTabLabel()))
                    {
                        page = Page.EditSpawnable;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Select a grid placement.",
                        MessageType.Info);
                }

                return;
            }

            if (_selectedIds.Count > 1)
            {
                GUILayout.Label(
                    $"{_selectedIds.Count} placements selected.");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Duplicate"))
                        DuplicateSelected();

                    if (GUILayout.Button("Delete"))
                        DeleteSelected();
                }

                return;
            }

            WaveLayoutPlacement placement = PrimaryPlacement();

            if (placement == null)
            {
                ClearSelection();
                return;
            }

            bool enabled = EditorGUILayout.Toggle(
                "Enabled",
                placement.Enabled);

            SpawnableDefinition spawnable =
                (SpawnableDefinition)EditorGUILayout.ObjectField(
                    "Spawnable",
                    placement.Spawnable,
                    typeof(SpawnableDefinition),
                    false);

            SpawnFormationDefinition formation =
                (SpawnFormationDefinition)EditorGUILayout.ObjectField(
                    "Formation",
                    placement.Formation,
                    typeof(SpawnFormationDefinition),
                    false);

            Vector2Int cell = EditorGUILayout.Vector2IntField(
                "Cell",
                placement.Cell);

            WaveGridRotation rotation =
                (WaveGridRotation)EditorGUILayout.EnumPopup(
                    "Rotation",
                    placement.Rotation);

            bool flipHorizontal = EditorGUILayout.Toggle(
                "Flip Horizontal",
                placement.FlipHorizontal);

            bool flipVertical = EditorGUILayout.Toggle(
                "Flip Vertical",
                placement.FlipVertical);

            float spawnDelay = EditorGUILayout.FloatField(
                "Spawn Delay",
                placement.SpawnDelay);

            bool sequential = EditorGUILayout.Toggle(
                "Sequential",
                placement.Sequential);

            float sequenceInterval = placement.SequenceInterval;

            if (sequential)
            {
                sequenceInterval = EditorGUILayout.FloatField(
                    "Sequence Gap",
                    placement.SequenceInterval);
            }

            int repetitions = EditorGUILayout.IntField(
                "Repetitions",
                placement.Repetitions);

            float repeatInterval = placement.RepeatInterval;

            if (repetitions > 1)
            {
                repeatInterval = EditorGUILayout.FloatField(
                    "Repeat Gap",
                    placement.RepeatInterval);
            }

            bool changed = enabled != placement.Enabled ||
                           spawnable != placement.Spawnable ||
                           formation != placement.Formation ||
                           cell != placement.Cell ||
                           rotation != placement.Rotation ||
                           flipHorizontal !=
                           placement.FlipHorizontal ||
                           flipVertical != placement.FlipVertical ||
                           !Mathf.Approximately(
                               spawnDelay,
                               placement.SpawnDelay) ||
                           sequential != placement.Sequential ||
                           !Mathf.Approximately(
                               sequenceInterval,
                               placement.SequenceInterval) ||
                           repetitions != placement.Repetitions ||
                           !Mathf.Approximately(
                               repeatInterval,
                               placement.RepeatInterval);

            if (changed)
            {
                WaveLayoutPlacement previous =
                    placement.Duplicate();
                previous.Id = placement.Id;

                RecordLayout("Edit wave placement");
                placement.Enabled = enabled;
                placement.Spawnable = spawnable;
                placement.Formation = formation;
                placement.Cell = cell;
                placement.Rotation = rotation;
                placement.FlipHorizontal = flipHorizontal;
                placement.FlipVertical = flipVertical;
                placement.SpawnDelay = Mathf.Max(0f, spawnDelay);
                placement.Sequential = sequential;
                placement.SequenceInterval = Mathf.Max(
                    0f,
                    sequenceInterval);
                placement.Repetitions = Mathf.Max(1, repetitions);
                placement.RepeatInterval = Mathf.Max(
                    0f,
                    repeatInterval);

                bool missingSpawnable =
                    placement.Enabled &&
                    placement.Spawnable == null;

                bool spatiallyValid =
                    WaveLayoutEditorUtility.IsWaveSpatiallyValid(
                        layout,
                        currentWaveIndex,
                        out string message);

                if (placement.Enabled &&
                    (missingSpawnable || !spatiallyValid))
                {
                    RestorePlacement(placement, previous);
                    SetStatus(
                        missingSpawnable
                            ? "Spawnable is required."
                            : message,
                        WaveLayoutValidationSeverity.Error);
                }

                MarkLayoutDirty();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Duplicate"))
                    DuplicateSelected();

                if (GUILayout.Button("Delete"))
                    DeleteSelected();
            }
        }

        private void DrawSelectedFormation()
        {
            GUILayout.Label(
                selectedFormation.DisplayName,
                EditorStyles.miniBoldLabel);

            int count = selectedFormation.CellOffsets == null
                ? 0
                : selectedFormation.CellOffsets.Length;

            GUILayout.Label($"{count} spawn points.");

            if (GUILayout.Button("Edit Formation"))
                SpawnFormationDesignerWindow.Open(selectedFormation);
        }

        private void DrawCalculator()
        {
            GUILayout.Label("Spawn Calculator", EditorStyles.boldLabel);

            WaveLayoutWaveStatistics wave =
                WaveLayoutCalculator.CalculateWave(
                    layout,
                    currentWaveIndex);

            WaveLayoutSequenceStatistics sequence =
                WaveLayoutCalculator.CalculateSequence(layout);

            DrawStat("Wave Spawns", wave.TotalSpawns.ToString());
            DrawStat("Sequence Spawns", sequence.TotalSpawns.ToString());
            DrawStat("Sequence Enemies", sequence.EnemySpawns.ToString());
            DrawStat("Duration", $"{wave.EstimatedDuration:0.##} s");
            DrawStat("Spawn Rate", $"{wave.SpawnRatePerSecond:0.##}/s");
            DrawStat("Spawned Together", wave.MaximumSimultaneous.ToString());
            DrawStat(
                "Simultaneous Enemies",
                wave.MaximumSimultaneousEnemies.ToString());
            DrawStat("Max Active Estimate", wave.MaximumActiveEnemies.ToString());
            DrawStat("Enemies", wave.EnemySpawns.ToString());
            DrawStat("Hazards", wave.HazardSpawns.ToString());
            DrawStat("Pickups", wave.PickupSpawns.ToString());
            DrawStat("Other", wave.OtherSpawns.ToString());

            foreach (
                KeyValuePair<SpawnableDefinition, int> pair
                in wave.SpawnCounts)
            {
                if (pair.Key != null &&
                    pair.Key.Kind == SpawnableKind.Enemy)
                {
                    DrawStat(pair.Key.DisplayName, pair.Value.ToString());
                }
            }
            DrawStat(
                "Grid Used",
                $"{wave.OccupiedCellPercentage:0.#}%");

            MessageType budgetType = wave.ExceedsBudget
                ? MessageType.Warning
                : MessageType.Info;

            EditorGUILayout.HelpBox(
                wave.ExceedsBudget
                    ? "Enemy budget exceeded."
                    : "Enemy budget is safe.",
                budgetType);
        }

        private void DrawValidationResults()
        {
            GUILayout.Label("Validation", EditorStyles.boldLabel);

            if (_validationIssues.Count == 0)
            {
                GUILayout.Label(
                    "Press Validate for results.",
                    _wrap);
                return;
            }

            foreach (WaveLayoutValidationIssue issue in _validationIssues)
            {
                MessageType type;

                switch (issue.Severity)
                {
                    case WaveLayoutValidationSeverity.Error:
                        type = MessageType.Error;
                        break;

                    case WaveLayoutValidationSeverity.Warning:
                        type = MessageType.Warning;
                        break;

                    default:
                        type = MessageType.Info;
                        break;
                }

                EditorGUILayout.HelpBox(issue.Message, type);
            }
        }

        private void DrawFaq()
        {
            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(_faqScroll))
            {
                _faqScroll = scroll.scrollPosition;

                EditorGUILayout.Space(12f);
                GUILayout.Label("Quick Guide", _headerTitle);
                DrawFaqItem(
                    "What is a layout?",
                    "A Wave Layout stores the complete grid, waves, " +
                    "timing and placements used by WaveRunner.");
                DrawFaqItem(
                    "What is a catalog?",
                    "A Spawn Catalog is the reusable palette of " +
                    "spawnables and formations available to designers.");
                DrawFaqItem(
                    "What is a spawnable?",
                    "A Spawnable Definition identifies one enemy, " +
                    "hazard, pickup or other prefab and its footprint.");
                DrawFaqItem(
                    "What is a formation?",
                    "A formation stores reusable cell offsets. Drag it " +
                    "after selecting which spawnable it should use.");
                DrawFaqItem(
                    "What is the origin?",
                    "The Origin transform maps cell zero to the game " +
                    "world. Assign it to the same WaveRunner.");
                DrawFaqItem(
                    "How do footprints work?",
                    "The pivot cell holds the prefab position. The other " +
                    "cells reserve space around it.");
                DrawFaqItem(
                    "What are the controls?",
                    "Use the mouse wheel to zoom, middle-drag or Alt-drag " +
                    "to pan, modifier-click to multi-select, Command/Ctrl-D " +
                    "to duplicate and Delete to remove.");
                DrawFaqItem(
                    "Together or sequential?",
                    "A formation spawns together by default. Enable " +
                    "Sequential to stagger its members.");
                DrawFaqItem(
                    "How do waves progress?",
                    "Auto Progress starts the next wave. Wait For Clear " +
                    "holds progression until tracked objects release.");
                DrawFaqItem(
                    "What is enemy budget?",
                    "It is a conservative warning limit. The calculator " +
                    "assumes spawned enemies may remain active.");
                DrawFaqItem(
                    "How does runtime start?",
                    "Assign the layout, origin and spawner to WaveRunner, " +
                    "then call StartSequence from any room trigger.");
                DrawFaqItem(
                    "What stays external?",
                    "Doors, cameras, room locks, movement and enemy AI " +
                    "remain in their own game systems.");
            }
        }

        private void DrawFaqItem(string question, string answer)
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                GUILayout.Label(question, EditorStyles.boldLabel);
                GUILayout.Label(answer, _wrap);
            }
        }

        private void DrawStatusBar()
        {
            Rect rect = GUILayoutUtility.GetRect(
                0f,
                22f,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.09f));

            bool dirty = layout != null &&
                         EditorUtility.IsDirty(layout);

            if (layout != null &&
                layout.Catalog != null &&
                EditorUtility.IsDirty(layout.Catalog))
            {
                dirty = true;
            }

            if (selectedPaletteSpawnable != null &&
                EditorUtility.IsDirty(selectedPaletteSpawnable))
            {
                dirty = true;
            }

            if (selectedFormation != null &&
                EditorUtility.IsDirty(selectedFormation))
            {
                dirty = true;
            }

            string displayStatus = dirty
                ? "Unsaved changes."
                : _status;

            WaveLayoutValidationSeverity displaySeverity = dirty
                ? WaveLayoutValidationSeverity.Warning
                : _statusSeverity;

            Color colour = AccentColour;

            if (displaySeverity == WaveLayoutValidationSeverity.Error)
                colour = ErrorColour;
            else if (displaySeverity ==
                     WaveLayoutValidationSeverity.Warning)
                colour = new Color(1f, 0.68f, 0.18f);
            else
                colour = ValidColour;

            Color previous = GUI.contentColor;
            GUI.contentColor = colour;

            GUI.Label(
                new Rect(
                    rect.x + 8f,
                    rect.y + 2f,
                    rect.width - 16f,
                    18f),
                displayStatus,
                EditorStyles.miniLabel);

            GUI.contentColor = previous;
        }

        public void SelectPlacementFromScene(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            ClearSelection();
            _selectedIds.Add(id);
            _primarySelectedId = id;
            page = Page.Builder;
            Repaint();
        }

        private void SetLayout(WaveLayoutData nextLayout)
        {
            layout = nextLayout;
            currentWaveIndex = 0;
            ClearSelection();
            _validationIssues.Clear();

            if (runner != null)
                AssignRunnerBinding();

            SceneView.RepaintAll();
            Repaint();
        }

        private void SetRunner(WaveRunner nextRunner)
        {
            runner = nextRunner;

            if (runner != null)
            {
                if (runner.AuthoredLayout != null)
                    layout = runner.AuthoredLayout;

                if (runner.AuthoredLayoutOrigin != null)
                    previewOrigin = runner.AuthoredLayoutOrigin;

                previewSpawner = runner.SpawnerSource != null
                    ? runner.SpawnerSource
                    : runner.GetComponent<WaveSpawner>();

                if (layout != null)
                    AssignRunnerBinding();
            }

            currentWaveIndex = 0;
            ClearSelection();
            SceneView.RepaintAll();
        }

        private void SetOrigin(Transform nextOrigin)
        {
            previewOrigin = nextOrigin;

            if (runner != null)
                AssignRunnerBinding();

            SceneView.RepaintAll();
        }

        private void SetSpawner(MonoBehaviour nextSpawner)
        {
            if (nextSpawner != null &&
                !(nextSpawner is IWaveSpawner))
            {
                SetStatus(
                    "Spawner type is invalid.",
                    WaveLayoutValidationSeverity.Error);
                return;
            }

            previewSpawner = nextSpawner;

            if (runner != null)
                AssignRunnerBinding();
        }

        private void AssignRunnerBinding()
        {
            if (runner == null)
                return;

            Undo.RecordObject(runner, "Bind authored wave layout");
            SerializedObject serializedRunner =
                new SerializedObject(runner);

            serializedRunner.Update();
            serializedRunner.FindProperty("authoredLayout")
                .objectReferenceValue = layout;
            serializedRunner.FindProperty("authoredLayoutOrigin")
                .objectReferenceValue = previewOrigin;
            serializedRunner.FindProperty("spawnerSource")
                .objectReferenceValue = previewSpawner;
            serializedRunner.ApplyModifiedProperties();
            EditorUtility.SetDirty(runner);
        }

        private void CreateLayout()
        {
            WaveLayoutData created =
                WaveLayoutEditorUtility.CreateAsset<WaveLayoutData>(
                    "Create Wave Layout",
                    "WaveLayoutData");

            if (created != null)
            {
                SetLayout(created);
                SetStatus(
                    "Layout created.",
                    WaveLayoutValidationSeverity.Success);
            }
        }

        private void CreateCatalog()
        {
            string directory =
                WaveLayoutEditorUtility.AssetDirectory(layout);

            SpawnCatalog created =
                WaveLayoutEditorUtility.CreateAsset<SpawnCatalog>(
                    "Create Spawn Catalog",
                    "SpawnCatalog",
                    directory);

            if (created != null && layout != null)
            {
                RecordLayout("Assign spawn catalog");
                layout.Catalog = created;
                MarkLayoutDirty();
            }
        }

        private void CreateSpawnable()
        {
            if (!EnsureCatalog())
                return;

            string directory = WaveLayoutEditorUtility.AssetDirectory(
                layout.Catalog);

            SpawnableDefinition created =
                WaveLayoutEditorUtility
                    .CreateAsset<SpawnableDefinition>(
                        "Create Spawnable",
                        "SpawnableDefinition",
                        directory);

            if (created == null)
                return;

            Undo.RecordObject(layout.Catalog, "Add spawnable");
            layout.Catalog.MutableSpawnables.Add(created);
            EditorUtility.SetDirty(layout.Catalog);
            selectedPaletteSpawnable = created;
            palettePage = PalettePage.Spawnables;
            Selection.activeObject = created;
            page = Page.EditSpawnable;
            SetStatus(
                "Spawnable created.",
                WaveLayoutValidationSeverity.Success);
        }

        private void CreateFormation()
        {
            if (!EnsureCatalog())
                return;

            string directory = WaveLayoutEditorUtility.AssetDirectory(
                layout.Catalog);

            SpawnFormationDefinition created =
                WaveLayoutEditorUtility
                    .CreateAsset<SpawnFormationDefinition>(
                        "Create Formation",
                        "SpawnFormation",
                        directory);

            if (created == null)
                return;

            Undo.RecordObject(layout.Catalog, "Add formation");
            layout.Catalog.MutableFormations.Add(created);
            EditorUtility.SetDirty(layout.Catalog);
            selectedFormation = created;
            palettePage = PalettePage.Formations;
            SpawnFormationDesignerWindow.Open(created);
        }

        private bool EnsureCatalog()
        {
            if (layout == null)
            {
                SetStatus(
                    "Create a layout first.",
                    WaveLayoutValidationSeverity.Warning);
                return false;
            }

            if (layout.Catalog == null)
                CreateCatalog();

            return layout.Catalog != null;
        }

        private void AddWave()
        {
            if (layout == null)
                return;

            RecordLayout("Add wave");
            WaveLayoutWave wave = new WaveLayoutWave
            {
                WaveName = $"Wave {layout.Waves.Count + 1}"
            };

            layout.Waves.Add(wave);
            currentWaveIndex = layout.Waves.Count - 1;
            ClearSelection();
            MarkLayoutDirty();
        }

        private void DuplicateWave()
        {
            if (!HasCurrentWave())
                return;

            RecordLayout("Duplicate wave");
            WaveLayoutWave source = layout.Waves[currentWaveIndex];
            string sourceName =
                WaveLayoutEditorUtility.DisplayName(
                    source,
                    currentWaveIndex);

            WaveLayoutWave duplicate = source.Duplicate(
                $"{sourceName} Copy");

            layout.Waves.Insert(currentWaveIndex + 1, duplicate);
            currentWaveIndex++;
            ClearSelection();
            MarkLayoutDirty();
        }

        private void MoveWave(int direction)
        {
            if (!HasCurrentWave())
                return;

            int target = Mathf.Clamp(
                currentWaveIndex + direction,
                0,
                layout.Waves.Count - 1);

            if (target == currentWaveIndex)
                return;

            RecordLayout("Reorder wave");
            WaveLayoutWave wave = layout.Waves[currentWaveIndex];
            layout.Waves.RemoveAt(currentWaveIndex);
            layout.Waves.Insert(target, wave);
            currentWaveIndex = target;
            MarkLayoutDirty();
        }

        private void DeleteWave()
        {
            if (!HasCurrentWave())
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Wave",
                "Delete this wave layout?",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            RecordLayout("Delete wave");
            layout.Waves.RemoveAt(currentWaveIndex);
            currentWaveIndex = Mathf.Clamp(
                currentWaveIndex,
                0,
                Mathf.Max(0, layout.Waves.Count - 1));
            ClearSelection();
            MarkLayoutDirty();
        }

        private void ClearWave()
        {
            if (!HasCurrentWave())
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Wave",
                "Remove every placement here?",
                "Clear",
                "Cancel");

            if (!confirmed)
                return;

            RecordLayout("Clear wave placements");
            layout.Waves[currentWaveIndex].Placements.Clear();
            ClearSelection();
            MarkLayoutDirty();
        }

        private void AddPlacement(WaveLayoutPlacement placement)
        {
            RecordLayout("Add wave placement");
            layout.Waves[currentWaveIndex].Placements.Add(placement);
            ClearSelection();
            _selectedIds.Add(placement.Id);
            _primarySelectedId = placement.Id;
            MarkLayoutDirty();
            SetStatus(
                "Placement added.",
                WaveLayoutValidationSeverity.Success);
        }

        private void DuplicateSelected()
        {
            if (!HasCurrentWave() || _selectedIds.Count == 0)
                return;

            RecordLayout("Duplicate wave placements");
            List<WaveLayoutPlacement> copies =
                new List<WaveLayoutPlacement>();
            List<Vector2Int> sourceCells =
                new List<Vector2Int>();

            foreach (
                WaveLayoutPlacement placement
                in layout.Waves[currentWaveIndex].Placements)
            {
                if (placement == null ||
                    !_selectedIds.Contains(placement.Id))
                {
                    continue;
                }

                WaveLayoutPlacement copy = placement.Duplicate();
                copies.Add(copy);
                sourceCells.Add(placement.Cell);
            }

            layout.Waves[currentWaveIndex].Placements.AddRange(copies);

            bool foundSpace = false;

            for (int y = 0;
                 y < layout.Rows && !foundSpace;
                 y++)
            {
                for (int x = 0;
                     x < layout.Columns && !foundSpace;
                     x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector2Int offset = new Vector2Int(x, y);

                    for (int index = 0;
                         index < copies.Count;
                         index++)
                    {
                        copies[index].Cell =
                            sourceCells[index] + offset;
                    }

                    foundSpace =
                        WaveLayoutEditorUtility.IsWaveSpatiallyValid(
                            layout,
                            currentWaveIndex,
                            out _);
                }
            }

            if (!foundSpace)
            {
                foreach (WaveLayoutPlacement copy in copies)
                {
                    layout.Waves[currentWaveIndex]
                        .Placements.Remove(copy);
                }

                SetStatus(
                    "No duplicate space.",
                    WaveLayoutValidationSeverity.Warning);
                MarkLayoutDirty();
                return;
            }

            ClearSelection();

            foreach (WaveLayoutPlacement copy in copies)
            {
                _selectedIds.Add(copy.Id);
                _primarySelectedId = copy.Id;
            }

            MarkLayoutDirty();
        }

        private void DeleteSelected()
        {
            if (!HasCurrentWave() || _selectedIds.Count == 0)
                return;

            RecordLayout("Delete wave placements");
            layout.Waves[currentWaveIndex].Placements.RemoveAll(
                placement => placement == null ||
                             _selectedIds.Contains(placement.Id));
            ClearSelection();
            MarkLayoutDirty();
        }

        private void BeginMove(Vector2Int mouseCell)
        {
            _moving = _selectedIds.Count > 0;
            _moveStartCell = mouseCell;
            _moveStartCells.Clear();

            if (!_moving)
                return;

            foreach (
                WaveLayoutPlacement placement
                in layout.Waves[currentWaveIndex].Placements)
            {
                if (placement != null &&
                    _selectedIds.Contains(placement.Id))
                {
                    _moveStartCells[placement.Id] = placement.Cell;
                }
            }

            Undo.RecordObject(layout, "Move wave placements");
        }

        private void ApplyMovePreview(Vector2Int mouseCell)
        {
            Vector2Int delta = mouseCell - _moveStartCell;

            foreach (
                KeyValuePair<string, Vector2Int> pair
                in _moveStartCells)
            {
                WaveLayoutPlacement placement =
                    WaveLayoutEditorUtility.FindPlacement(
                        layout,
                        currentWaveIndex,
                        pair.Key);

                if (placement != null)
                    placement.Cell = pair.Value + delta;
            }
        }

        private void FinishMove()
        {
            _moving = false;

            if (!WaveLayoutEditorUtility.IsWaveSpatiallyValid(
                    layout,
                    currentWaveIndex,
                    out string message))
            {
                foreach (
                    KeyValuePair<string, Vector2Int> pair
                    in _moveStartCells)
                {
                    WaveLayoutPlacement placement =
                        WaveLayoutEditorUtility.FindPlacement(
                            layout,
                            currentWaveIndex,
                            pair.Key);

                    if (placement != null)
                        placement.Cell = pair.Value;
                }

                SetStatus(
                    message,
                    WaveLayoutValidationSeverity.Error);
            }
            else
            {
                SetStatus(
                    "Placement moved.",
                    WaveLayoutValidationSeverity.Success);
            }

            _moveStartCells.Clear();
            MarkLayoutDirty();
        }

        private bool IsCandidateValid(
            WaveLayoutPlacement candidate)
        {
            WaveLayoutWave wave = layout.Waves[currentWaveIndex];
            wave.Placements.Add(candidate);

            bool valid =
                WaveLayoutEditorUtility.IsWaveSpatiallyValid(
                    layout,
                    currentWaveIndex,
                    out _);

            wave.Placements.Remove(candidate);
            return valid;
        }

        private HashSet<string> GetInvalidPlacementIds()
        {
            HashSet<string> invalid = new HashSet<string>();

            if (!HasCurrentWave())
                return invalid;

            Dictionary<int, Dictionary<Vector2Int, string>> byTime =
                new Dictionary<int,
                    Dictionary<Vector2Int, string>>();

            foreach (
                WaveSpawnInstruction instruction
                in WaveLayoutCompiler.CompileWave(
                    layout,
                    currentWaveIndex))
            {
                string id = instruction.Placement.Id;
                int time = Mathf.RoundToInt(
                    instruction.SpawnTime / 0.001f);

                if (!byTime.TryGetValue(
                        time,
                        out Dictionary<Vector2Int, string> cells))
                {
                    cells = new Dictionary<Vector2Int, string>();
                    byTime.Add(time, cells);
                }

                foreach (
                    Vector2Int cell
                    in WaveLayoutGeometry.GetOccupiedCells(
                        instruction.Spawnable,
                        instruction.Cell,
                        instruction.Rotation,
                        instruction.FlipHorizontal,
                        instruction.FlipVertical))
                {
                    if (!WaveLayoutGeometry.IsInside(layout, cell))
                    {
                        invalid.Add(id);
                    }
                    else if (cells.TryGetValue(
                                 cell,
                                 out string otherId))
                    {
                        invalid.Add(id);
                        invalid.Add(otherId);
                    }
                    else
                    {
                        cells.Add(cell, id);
                    }
                }
            }

            return invalid;
        }

        private WaveLayoutPlacement FindPlacementAt(
            Vector2 mouse,
            Vector2 gridOrigin,
            float cellSize)
        {
            if (!HasCurrentWave())
                return null;

            List<WaveLayoutPlacement> placements =
                layout.Waves[currentWaveIndex].Placements;

            for (int index = placements.Count - 1;
                 index >= 0;
                 index--)
            {
                WaveLayoutPlacement placement = placements[index];

                if (placement == null || placement.Spawnable == null)
                    continue;

                foreach (
                    Vector2Int formationCell
                    in WaveLayoutGeometry.GetFormationCells(placement))
                {
                    foreach (
                        Vector2Int occupied
                        in WaveLayoutGeometry.GetOccupiedCells(
                            placement.Spawnable,
                            formationCell,
                            placement.Rotation,
                            placement.FlipHorizontal,
                            placement.FlipVertical))
                    {
                        if (CellRect(
                                occupied,
                                gridOrigin,
                                cellSize)
                            .Contains(mouse))
                        {
                            return placement;
                        }
                    }
                }
            }

            return null;
        }

        private void HandleGridShortcuts()
        {
            Event current = Event.current;

            if (!_gridFocused ||
                current.type != EventType.KeyDown ||
                GUIUtility.keyboardControl != 0)
            {
                return;
            }

            if (current.keyCode == KeyCode.Delete ||
                current.keyCode == KeyCode.Backspace)
            {
                DeleteSelected();
                current.Use();
            }
            else if (current.keyCode == KeyCode.D &&
                     HasActionModifier(current))
            {
                DuplicateSelected();
                current.Use();
            }
        }

        private void RunValidation()
        {
            _validationIssues.Clear();
            _validationIssues.AddRange(
                WaveLayoutValidator.Validate(
                    layout,
                    previewOrigin,
                    true));

            if (runner == null)
            {
                _validationIssues.Add(
                    new WaveLayoutValidationIssue(
                        WaveLayoutValidationSeverity.Warning,
                        "No scene runner selected."));
            }
            else if (previewSpawner == null)
            {
                _validationIssues.Add(
                    new WaveLayoutValidationIssue(
                        WaveLayoutValidationSeverity.Error,
                        "Runner spawner is missing.",
                        runner));
            }
            else if (!(previewSpawner is IWaveSpawner))
            {
                _validationIssues.Add(
                    new WaveLayoutValidationIssue(
                        WaveLayoutValidationSeverity.Error,
                        "Spawner type is invalid.",
                        previewSpawner));
            }

            int errors = _validationIssues.FindAll(
                issue => issue.Severity ==
                    WaveLayoutValidationSeverity.Error).Count;

            int warnings = _validationIssues.FindAll(
                issue => issue.Severity ==
                    WaveLayoutValidationSeverity.Warning).Count;

            if (errors > 0)
            {
                SetStatus(
                    $"{errors} errors, {warnings} warnings.",
                    WaveLayoutValidationSeverity.Error);
            }
            else if (warnings > 0)
            {
                SetStatus(
                    $"{warnings} warnings.",
                    WaveLayoutValidationSeverity.Warning);
            }
            else
            {
                SetStatus(
                    "Layout is ready.",
                    WaveLayoutValidationSeverity.Success);
            }
        }

        private bool HasCurrentWave()
        {
            return layout != null &&
                   layout.Waves != null &&
                   currentWaveIndex >= 0 &&
                   currentWaveIndex < layout.Waves.Count &&
                   layout.Waves[currentWaveIndex] != null;
        }

        private void ClampWaveIndex()
        {
            if (layout == null || layout.Waves.Count == 0)
            {
                currentWaveIndex = 0;
                return;
            }

            currentWaveIndex = Mathf.Clamp(
                currentWaveIndex,
                0,
                layout.Waves.Count - 1);
        }

        private WaveLayoutPlacement PrimaryPlacement()
        {
            return WaveLayoutEditorUtility.FindPlacement(
                layout,
                currentWaveIndex,
                _primarySelectedId);
        }

        private void ClearSelection()
        {
            _selectedIds.Clear();
            _primarySelectedId = null;
        }

        private static bool HasActionModifier(Event current)
        {
            EventModifiers modifiers = current.modifiers;

            return (modifiers & EventModifiers.Control) != 0 ||
                   (modifiers & EventModifiers.Command) != 0;
        }

        private static void RestorePlacement(
            WaveLayoutPlacement destination,
            WaveLayoutPlacement source)
        {
            destination.Enabled = source.Enabled;
            destination.Spawnable = source.Spawnable;
            destination.Formation = source.Formation;
            destination.Cell = source.Cell;
            destination.Rotation = source.Rotation;
            destination.FlipHorizontal = source.FlipHorizontal;
            destination.FlipVertical = source.FlipVertical;
            destination.SpawnDelay = source.SpawnDelay;
            destination.Sequential = source.Sequential;
            destination.SequenceInterval = source.SequenceInterval;
            destination.Repetitions = source.Repetitions;
            destination.RepeatInterval = source.RepeatInterval;
        }

        private bool Matches(string value)
        {
            return string.IsNullOrWhiteSpace(search) ||
                   (!string.IsNullOrWhiteSpace(value) &&
                    value.IndexOf(
                        search.Trim(),
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool MatchesKind(SpawnableKind kind)
        {
            return kindFilter == 0 ||
                   (int)kind == kindFilter - 1;
        }

        private Rect CellRect(
            Vector2Int cell,
            Vector2 gridOrigin,
            float cellSize)
        {
            return new Rect(
                gridOrigin.x + cell.x * cellSize,
                gridOrigin.y +
                (layout.Rows - cell.y - 1) * cellSize,
                cellSize,
                cellSize);
        }

        private Vector2Int MouseToCell(
            Vector2 mouse,
            Vector2 gridOrigin,
            float cellSize)
        {
            return new Vector2Int(
                Mathf.FloorToInt(
                    (mouse.x - gridOrigin.x) / cellSize),
                layout.Rows - 1 - Mathf.FloorToInt(
                    (mouse.y - gridOrigin.y) / cellSize));
        }

        private static Rect Shrink(Rect rect, float amount)
        {
            return new Rect(
                rect.x + amount,
                rect.y + amount,
                Mathf.Max(0f, rect.width - amount * 2f),
                Mathf.Max(0f, rect.height - amount * 2f));
        }

        private static void DrawDivider()
        {
            Rect divider = GUILayoutUtility.GetRect(
                1f,
                1f,
                GUILayout.Width(1f),
                GUILayout.ExpandHeight(true));

            EditorGUI.DrawRect(
                divider,
                new Color(0.08f, 0.08f, 0.09f));
        }

        private static void DrawStat(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label);
                GUILayout.FlexibleSpace();
                GUILayout.Label(value, EditorStyles.boldLabel);
            }
        }

        private void RecordLayout(string action)
        {
            if (layout != null)
                Undo.RecordObject(layout, action);
        }

        private void MarkLayoutDirty()
        {
            if (layout != null)
                EditorUtility.SetDirty(layout);

            SceneView.RepaintAll();
            Repaint();
        }

        private void SetStatus(
            string message,
            WaveLayoutValidationSeverity severity)
        {
            _status = message;
            _statusSeverity = severity;
            Repaint();
        }

        private void HandleUndoRedo()
        {
            ClearSelection();
            Repaint();
            SceneView.RepaintAll();
        }

        private void EnsureStyles()
        {
            if (_headerTitle != null)
                return;

            _headerTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                wordWrap = true
            };
            _headerTitle.normal.textColor = Color.white;

            _headerSubtitle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11
            };
            _headerSubtitle.normal.textColor =
                new Color(0.78f, 0.74f, 0.84f);

            _paletteItem = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 44f
            };

            _wrap = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            _centeredMini = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
        }
    }
}
