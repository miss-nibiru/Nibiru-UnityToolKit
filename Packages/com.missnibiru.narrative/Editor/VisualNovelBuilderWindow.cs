using System;
using System.Collections.Generic;
using System.IO;
using MissNibiru.Narrative;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MissNibiru.Narrative.Editor
{
    public sealed class VisualNovelBuilderWindow : EditorWindow
    {
        private enum BuilderTab
        {
            Flow,
            Presentation,
            Preview,
            Library,
            Validation,
            StartHere,
            FAQ
        }

        private const string BannerPath =
            "Packages/com.missnibiru.narrative/Editor/Branding/NibiruMainBanner.png";

        private readonly NarrativePresentationDesigner _designer =
            new NarrativePresentationDesigner();
        private readonly NarrativePreviewSession _preview =
            new NarrativePreviewSession();
        private readonly List<NarrativeValidationIssue> _issues =
            new List<NarrativeValidationIssue>();

        private NarrativeStory _story;
        private NarrativeNode _selectedNode;
        private BuilderTab _tab;
        private VisualElement _body;
        private VisualElement _tabsRoot;
        private NarrativeGraphView _graph;
        private VisualElement _inspectorRoot;
        private ObjectField _storyField;
        private Vector2 _libraryScroll;
        private Vector2 _validationScroll;
        private Vector2 _startHereScroll;
        private Vector2 _faqScroll;
        private bool _graphRefreshQueued;

        [MenuItem(
            "Tools/Miss Nibiru/Visual Novel Builder",
            false,
            120)]
        public static void Open()
        {
            VisualNovelBuilderWindow window =
                GetWindow<VisualNovelBuilderWindow>();
            window.titleContent = new GUIContent("Visual Novel Builder");
            window.minSize = new Vector2(1000f, 650f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_story == null)
                _story = Selection.activeObject as NarrativeStory;

            NarrativeEditorEvents.GraphRefreshRequested += QueueGraphRefresh;
            Undo.undoRedoPerformed += HandleUndoRedo;
            BuildWindow();
        }

        private void OnDisable()
        {
            NarrativeEditorEvents.GraphRefreshRequested -= QueueGraphRefresh;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            EditorApplication.delayCall -= RunQueuedGraphRefresh;
            _designer.Dispose();
        }

        private void BuildWindow()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor =
                new Color(0.055f, 0.04f, 0.075f);
            BuildHeader();
            BuildStoryBar();
            BuildTabs();
            _body = new VisualElement();
            _body.style.flexGrow = 1f;
            _body.style.minHeight = 300f;
            rootVisualElement.Add(_body);
            ShowTab(_tab);
        }

        private void BuildHeader()
        {
            VisualElement header = new VisualElement();
            header.style.height = 118f;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.backgroundColor =
                new Color(0.095f, 0.045f, 0.15f);
            header.style.borderBottomWidth = 2f;
            header.style.borderBottomColor =
                new Color(0.55f, 0.24f, 0.80f);

            Texture2D banner = AssetDatabase.LoadAssetAtPath<Texture2D>(
                BannerPath);

            if (banner != null)
            {
                Image image = new Image
                {
                    image = banner,
                    scaleMode = ScaleMode.ScaleToFit
                };
                image.style.width = 335f;
                image.style.height = 104f;
                image.style.marginLeft = 8f;
                header.Add(image);
            }

            VisualElement titleBlock = new VisualElement();
            titleBlock.style.marginLeft = 18f;
            titleBlock.style.flexGrow = 1f;
            Label title = new Label("Visual Novel Builder");
            title.style.fontSize = 24f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            Label subtitle = new Label(
                "Build branching dialogue visually.");
            subtitle.style.fontSize = 12f;
            subtitle.style.color = new Color(0.77f, 0.70f, 0.84f);
            subtitle.style.marginTop = 5f;
            titleBlock.Add(title);
            titleBlock.Add(subtitle);
            header.Add(titleBlock);
            rootVisualElement.Add(header);
        }

        private void BuildStoryBar()
        {
            VisualElement bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.paddingLeft = 7f;
            bar.style.paddingRight = 7f;
            bar.style.paddingTop = 5f;
            bar.style.paddingBottom = 5f;
            bar.style.backgroundColor = new Color(0.16f, 0.14f, 0.18f);

            Label label = new Label("Story");
            label.style.width = 48f;
            bar.Add(label);

            _storyField = new ObjectField
            {
                objectType = typeof(NarrativeStory),
                allowSceneObjects = false,
                value = _story,
                tooltip = "Dialogue asset"
            };
            _storyField.style.flexGrow = 1f;
            _storyField.RegisterValueChangedCallback(change =>
                SetStory(change.newValue as NarrativeStory));
            bar.Add(_storyField);

            bar.Add(CreateToolbarButton("New Story", CreateStory,
                "Create story"));
            bar.Add(CreateToolbarButton("Import Twee", ImportTwee,
                "Import Twee file"));
            bar.Add(CreateToolbarButton("Locate", LocateStory,
                "Find story asset"));
            bar.Add(CreateToolbarButton("Save", SaveAll,
                "Save all assets"));
            bar.Add(CreateToolbarButton("Validate", ValidateStory,
                "Check story"));
            rootVisualElement.Add(bar);
        }

        private void BuildTabs()
        {
            _tabsRoot = new VisualElement();
            _tabsRoot.style.flexDirection = FlexDirection.Row;
            _tabsRoot.style.justifyContent = Justify.Center;
            _tabsRoot.style.paddingTop = 5f;
            _tabsRoot.style.paddingBottom = 5f;
            _tabsRoot.style.backgroundColor =
                new Color(0.08f, 0.065f, 0.10f);
            RefreshTabButtons();
            rootVisualElement.Add(_tabsRoot);
        }

        private void ShowTab(BuilderTab tab)
        {
            _tab = tab;

            if (_body == null)
                return;

            _body.Clear();

            switch (tab)
            {
                case BuilderTab.Flow:
                    BuildFlowTab();
                    break;
                case BuilderTab.Presentation:
                    AddGuiTab(DrawPresentationTab);
                    break;
                case BuilderTab.Preview:
                    AddGuiTab(DrawPreviewTab);
                    break;
                case BuilderTab.Library:
                    AddGuiTab(DrawLibraryTab);
                    break;
                case BuilderTab.Validation:
                    AddGuiTab(DrawValidationTab);
                    break;
                case BuilderTab.StartHere:
                    AddGuiTab(DrawStartHereTab);
                    break;
                case BuilderTab.FAQ:
                    AddGuiTab(DrawFaqTab);
                    break;
            }

            RefreshTabButtons();
        }

        private void RefreshTabButtons()
        {
            if (_tabsRoot == null)
                return;

            _tabsRoot.Clear();

            foreach (BuilderTab tab in Enum.GetValues(typeof(BuilderTab)))
            {
                BuilderTab captured = tab;
                Button button = new Button(() => ShowTab(captured))
                {
                    text = GetTabLabel(tab)
                };
                button.style.width = 112f;
                button.style.height = 26f;
                button.style.marginLeft = 1f;
                button.style.marginRight = 1f;
                button.style.backgroundColor = tab == _tab
                    ? new Color(0.35f, 0.21f, 0.50f)
                    : new Color(0.25f, 0.24f, 0.27f);
                _tabsRoot.Add(button);
            }
        }

        private void AddGuiTab(Action handler)
        {
            IMGUIContainer container = new IMGUIContainer(handler);
            container.style.flexGrow = 1f;
            container.style.paddingLeft = 7f;
            container.style.paddingRight = 7f;
            container.style.paddingTop = 7f;
            container.style.paddingBottom = 7f;
            _body.Add(container);
        }

        private void BuildFlowTab()
        {
            VisualElement column = new VisualElement();
            column.style.flexGrow = 1f;
            column.style.flexDirection = FlexDirection.Column;
            VisualElement navigation = new VisualElement();
            navigation.style.height = 30f;
            navigation.style.flexDirection = FlexDirection.Row;
            navigation.style.alignItems = Align.Center;
            navigation.style.paddingLeft = 6f;
            navigation.style.paddingRight = 6f;
            navigation.style.backgroundColor =
                new Color(0.12f, 0.095f, 0.15f);
            Label findLabel = new Label("Find");
            findLabel.style.width = 35f;
            navigation.Add(findLabel);
            TextField search = new TextField
            {
                tooltip = "Find imported nodes"
            };
            search.style.flexGrow = 1f;
            search.style.maxWidth = 360f;
            navigation.Add(search);
            navigation.Add(CreateToolbarButton(
                "Previous",
                () => _graph?.FocusMatch(search.value, -1),
                "Previous match"));
            navigation.Add(CreateToolbarButton(
                "Next",
                () => _graph?.FocusMatch(search.value, 1),
                "Next match"));
            navigation.Add(CreateToolbarButton(
                "Frame All",
                () => _graph?.FrameAllNodes(),
                "Show every node"));
            column.Add(navigation);

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexGrow = 1f;

            VisualElement palette = BuildNodePalette();
            row.Add(palette);

            _graph = new NarrativeGraphView(SelectNode);
            _graph.style.flexGrow = 1f;
            _graph.LoadStory(_story);
            row.Add(_graph);

            _inspectorRoot = new ScrollView();
            _inspectorRoot.style.width = 325f;
            _inspectorRoot.style.flexShrink = 0f;
            _inspectorRoot.style.paddingLeft = 7f;
            _inspectorRoot.style.paddingRight = 7f;
            _inspectorRoot.style.backgroundColor =
                new Color(0.12f, 0.105f, 0.13f);
            row.Add(_inspectorRoot);
            RebuildInspector();
            column.Add(row);
            _body.Add(column);
        }

        private VisualElement BuildNodePalette()
        {
            VisualElement palette = new VisualElement();
            palette.style.width = 155f;
            palette.style.flexShrink = 0f;
            palette.style.paddingLeft = 6f;
            palette.style.paddingRight = 6f;
            palette.style.paddingTop = 7f;
            palette.style.backgroundColor = new Color(0.12f, 0.095f, 0.15f);

            Label heading = new Label("Nodes");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.fontSize = 14f;
            heading.style.marginBottom = 5f;
            palette.Add(heading);
            AddNodeButton<NarrativeLineNode>(palette, "Dialogue Line");
            AddNodeButton<NarrativeChoiceNode>(palette, "Player Choice");
            AddNodeButton<NarrativeConditionNode>(palette, "Condition");
            AddNodeButton<NarrativeSetValueNode>(palette, "Set Value / Flag");
            AddNodeButton<NarrativeRandomValueNode>(palette, "Random Value");
            AddNodeButton<NarrativeEventNode>(palette, "Gameplay Event");
            AddNodeButton<NarrativeWaitNode>(palette, "Wait");
            AddNodeButton<NarrativeEndNode>(palette, "End");

            palette.Add(CreatePaletteButton(
                "Duplicate Selected", DuplicateSelected,
                "Copy selected node"));
            palette.Add(CreatePaletteButton(
                "Delete Selected", DeleteSelected,
                "Safely delete node"));
            palette.Add(CreatePaletteButton(
                "Refresh Graph", RefreshGraph,
                "Refresh node ports"));
            return palette;
        }

        private void AddNodeButton<T>(VisualElement palette, string label)
            where T : NarrativeNode
        {
            palette.Add(CreatePaletteButton(
                "+ " + label,
                () => AddNode<T>(),
                "Create node"));
        }

        private static Button CreatePaletteButton(
            string text,
            Action clicked,
            string tooltip)
        {
            Button button = new Button(clicked)
            {
                text = text,
                tooltip = tooltip
            };
            button.style.height = 27f;
            button.style.marginBottom = 3f;
            return button;
        }

        private static Button CreateToolbarButton(
            string text,
            Action clicked,
            string tooltip)
        {
            Button button = new Button(clicked)
            {
                text = text,
                tooltip = tooltip
            };
            button.style.width = 88f;
            button.style.height = 23f;
            button.style.marginLeft = 4f;
            return button;
        }

        private void SelectNode(NarrativeNode node)
        {
            _selectedNode = node;
            RebuildInspector();
        }

        private void RebuildInspector()
        {
            if (_inspectorRoot == null)
                return;

            _inspectorRoot.Clear();
            Label heading = new Label(_selectedNode == null
                ? "Node Inspector"
                : _selectedNode.NodeTitle);
            heading.style.fontSize = 16f;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.color = new Color(0.78f, 0.48f, 0.98f);
            heading.style.marginTop = 8f;
            heading.style.marginBottom = 6f;
            _inspectorRoot.Add(heading);

            if (_selectedNode == null)
            {
                Label help = new Label("Select a node to edit it.");
                help.style.whiteSpace = WhiteSpace.Normal;
                _inspectorRoot.Add(help);
                return;
            }

            Label id = new Label("ID: " + _selectedNode.Id);
            id.style.fontSize = 10f;
            id.style.color = new Color(0.66f, 0.60f, 0.70f);
            id.style.marginBottom = 6f;
            _inspectorRoot.Add(id);
            InspectorElement inspector = new InspectorElement(_selectedNode);
            inspector.style.flexGrow = 1f;
            _inspectorRoot.Add(inspector);
        }

        private void CreateStory()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Narrative Story",
                "NarrativeStory",
                "asset",
                "Choose a story location.");

            if (!string.IsNullOrWhiteSpace(path))
                SetStory(NarrativeAssetFactory.CreateStory(path));
        }

        private void ImportTwee()
        {
            string sourcePath = EditorUtility.OpenFilePanel(
                "Import SugarCube Twee",
                string.Empty,
                "twee");

            if (string.IsNullOrWhiteSpace(sourcePath))
                return;

            string defaultName = Path.GetFileNameWithoutExtension(sourcePath);
            string storyPath = EditorUtility.SaveFilePanelInProject(
                "Save Imported Narrative Story",
                defaultName,
                "asset",
                "The importer creates a new Story and never overwrites an existing one.");

            if (string.IsNullOrWhiteSpace(storyPath))
                return;

            TweeImportReviewDecision decision =
                TweeImportReviewWindow.ShowReview(sourcePath);

            if (!decision.Accepted)
                return;

            TweeImportResult result = TweeImportService.ImportFile(
                sourcePath,
                storyPath,
                decision.Profile);

            if (result.Story != null)
            {
                _tab = BuilderTab.Flow;
                SetStory(result.Story);
                _graph?.FrameAllNodes();
            }

            string message = result.Story == null
                ? "Twee import could not create a story."
                : $"Imported {result.PassageCount} passages and " +
                  $"{result.DialogueLineCount} dialogue lines.\n" +
                  $"{result.CharacterCount} characters · " +
                  $"{result.AudioUsageCount} audio uses · " +
                  $"{result.NodeCount} nodes\n\n" +
                  $"{result.Count(TweeImportIssueSeverity.Error)} errors · " +
                  $"{result.Count(TweeImportIssueSeverity.Warning)} warnings";
            bool locateReport = EditorUtility.DisplayDialog(
                result.Story == null
                    ? "Twee Import Stopped"
                    : "Twee Import Complete",
                message,
                string.IsNullOrWhiteSpace(result.ReportPath)
                    ? "OK"
                    : "Locate Report",
                string.IsNullOrWhiteSpace(result.ReportPath)
                    ? string.Empty
                    : "Close");

            if (locateReport &&
                !string.IsNullOrWhiteSpace(result.ReportPath))
            {
                TextAsset report = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    result.ReportPath);
                Selection.activeObject = report;
                EditorGUIUtility.PingObject(report);
            }
        }

        private void SetStory(NarrativeStory value)
        {
            _story = value;
            _selectedNode = null;
            _issues.Clear();
            _preview.Start(null);

            if (_storyField != null && _storyField.value != value)
                _storyField.SetValueWithoutNotify(value);

            ShowTab(_tab);
        }

        private void LocateStory()
        {
            if (_story != null)
            {
                Selection.activeObject = _story;
                EditorGUIUtility.PingObject(_story);
            }
        }

        private void SaveAll()
        {
            if (_story != null)
                EditorUtility.SetDirty(_story);

            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Narrative saved"));
        }

        private void ValidateStory()
        {
            _issues.Clear();
            _issues.AddRange(NarrativeValidator.Validate(_story));
            ShowTab(BuilderTab.Validation);
        }

        private void AddNode<T>() where T : NarrativeNode
        {
            if (_story == null)
            {
                ShowNotification(new GUIContent("Create or assign a story"));
                return;
            }

            Vector2 position = _graph == null
                ? new Vector2(250f, 180f)
                : _graph.GetCreationPosition();
            NarrativeNode node = NarrativeAssetFactory.AddNode<T>(
                _story, position);
            _selectedNode = node;
            RefreshGraph();
        }

        private void DuplicateSelected()
        {
            NarrativeNode selected = _graph?.GetSelectedNode() ?? _selectedNode;
            NarrativeNode duplicate = NarrativeAssetFactory.DuplicateNode(
                _story, selected);

            if (duplicate != null)
            {
                _selectedNode = duplicate;
                RefreshGraph();
            }
        }

        private void DeleteSelected()
        {
            NarrativeNode selected = _graph?.GetSelectedNode() ?? _selectedNode;

            if (selected == null)
                return;

            if (selected is NarrativeStartNode)
            {
                EditorUtility.DisplayDialog(
                    "Start Node Required",
                    "The Start node cannot be deleted.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Narrative Node?",
                $"Delete {selected.NodeTitle}? Connected links will be cleared.",
                "Delete",
                "Cancel");

            if (confirmed && NarrativeAssetFactory.DeleteNode(_story, selected))
            {
                _selectedNode = null;
                RefreshGraph();
            }
        }

        private void RefreshGraph()
        {
            if (_tab != BuilderTab.Flow)
                return;

            _graph?.LoadStory(_story);
            RebuildInspector();
            Repaint();
        }

        private void QueueGraphRefresh()
        {
            if (_graphRefreshQueued)
                return;

            _graphRefreshQueued = true;
            EditorApplication.delayCall += RunQueuedGraphRefresh;
        }

        private void RunQueuedGraphRefresh()
        {
            EditorApplication.delayCall -= RunQueuedGraphRefresh;
            _graphRefreshQueued = false;

            if (this != null)
                RefreshGraph();
        }

        private void HandleUndoRedo()
        {
            QueueGraphRefresh();
            Repaint();
        }

        private NarrativeLineNode GetPreviewLine()
        {
            if (_selectedNode is NarrativeLineNode selectedLine)
                return selectedLine;

            if (_preview.CurrentLine != null)
                return _preview.CurrentLine;

            if (_story != null)
            {
                foreach (NarrativeNode node in _story.Nodes)
                {
                    if (node is NarrativeLineNode line)
                        return line;
                }
            }

            return null;
        }

        private void DrawPresentationTab()
        {
            DialoguePresentationProfile profile =
                _story == null ? null : _story.PresentationProfile;
            _designer.Draw(profile, GetPreviewLine());
        }

        private void DrawPreviewTab()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.enabled = _story != null;

            if (GUILayout.Button("Start Preview", EditorStyles.toolbarButton))
                _preview.Start(_story);

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                "Editor preview never fires gameplay events.",
                EditorStyles.miniLabel,
                GUILayout.Width(255f));
            EditorGUILayout.EndHorizontal();

            if (_story == null)
            {
                EditorGUILayout.HelpBox("Assign a story.", MessageType.Info);
                return;
            }

            if (!string.IsNullOrWhiteSpace(_preview.Error))
            {
                EditorGUILayout.HelpBox(_preview.Error, MessageType.Error);
                return;
            }

            if (_preview.IsComplete)
            {
                NarrativeEndNode end = _preview.CurrentNode as NarrativeEndNode;
                EditorGUILayout.HelpBox(
                    end == null
                        ? "Story complete."
                        : $"Ending: {end.EndingId}",
                    MessageType.Info);
                return;
            }

            if (_preview.CurrentNode == null)
            {
                EditorGUILayout.HelpBox(
                    "Press Start Preview.", MessageType.Info);
                return;
            }

            DrawPreviewScreen();

            if (_preview.CurrentLine != null)
            {
                if (GUILayout.Button("Next", GUILayout.Height(32f)))
                    _preview.Next();
            }
            else if (_preview.CurrentChoice != null)
            {
                for (int i = 0; i < _preview.Choices.Count; i++)
                {
                    int index = i;

                    if (GUILayout.Button(
                            _preview.Choices[i].Text,
                            GUILayout.Height(32f)))
                    {
                        _preview.Choose(index);
                    }
                }
            }
        }

        private void DrawPreviewScreen()
        {
            DialoguePresentationProfile profile = _story.PresentationProfile;
            Rect bounds = GUILayoutUtility.GetRect(
                500f,
                10000f,
                280f,
                520f,
                GUILayout.ExpandWidth(true));
            Rect screen = FitAspect(bounds, 16f / 9f);
            Color background = profile == null
                ? new Color(0.06f, 0.035f, 0.09f)
                : profile.PreviewBackground;
            EditorGUI.DrawRect(screen, background);

            NarrativeLineNode line = _preview.CurrentLine;

            NarrativeRect backgroundLayout = profile == null
                ? new NarrativeRect(0f, 0f, 1f, 1f)
                : profile.GetRect(NarrativeLayoutElement.Background);
            Rect backgroundRect = ToGuiRect(screen, backgroundLayout);

            if (line?.Background != null)
                GUI.DrawTexture(backgroundRect, line.Background.texture,
                    ScaleMode.ScaleAndCrop);

            DrawPreviewPortrait(screen, profile, line);

            NarrativeRect dialogueLayout = profile == null
                ? new NarrativeRect(0.05f, 0.05f, 0.90f, 0.25f)
                : profile.GetRect(NarrativeLayoutElement.DialogueBox);
            NarrativeRect speakerLayout = profile == null
                ? new NarrativeRect(0.07f, 0.27f, 0.30f, 0.06f)
                : profile.GetRect(NarrativeLayoutElement.SpeakerName);
            NarrativeRect bodyLayout = profile == null
                ? new NarrativeRect(0.07f, 0.08f, 0.86f, 0.17f)
                : profile.GetRect(NarrativeLayoutElement.BodyText);
            Rect box = ToGuiRect(screen, dialogueLayout);
            Rect speakerRect = ToGuiRect(screen, speakerLayout);
            Rect bodyRect = ToGuiRect(screen, bodyLayout);
            EditorGUI.DrawRect(
                box,
                profile == null
                    ? new Color(0.10f, 0.05f, 0.18f, 0.94f)
                    : profile.DialogueBoxColour);
            GUIStyle speakerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.82f, 0.50f, 1f) }
            };
            GUIStyle bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
            string speaker = line?.Character == null
                ? string.Empty
                : line.Character.DisplayName;
            string body = line != null
                ? _preview.CurrentText
                : _preview.CurrentChoice?.Prompt ?? string.Empty;
            GUI.Label(speakerRect, speaker, speakerStyle);
            GUI.Label(bodyRect, body, bodyStyle);

            if (_preview.CurrentChoice != null && profile != null)
            {
                Rect choicesRect = ToGuiRect(
                    screen,
                    profile.GetRect(NarrativeLayoutElement.Choices));
                EditorGUI.DrawRect(
                    choicesRect,
                    new Color(
                        profile.ChoiceColour.r,
                        profile.ChoiceColour.g,
                        profile.ChoiceColour.b,
                        0.70f));
                string previewText = string.Empty;

                for (int i = 0; i < _preview.Choices.Count; i++)
                    previewText += $"{i + 1}. {_preview.Choices[i].Text}\n";

                GUI.Label(
                    new Rect(
                        choicesRect.x + 9f,
                        choicesRect.y + 7f,
                        choicesRect.width - 18f,
                        choicesRect.height - 14f),
                    previewText,
                    bodyStyle);
            }
        }

        private static void DrawPreviewPortrait(
            Rect screen,
            DialoguePresentationProfile profile,
            NarrativeLineNode line)
        {
            if (line?.Character == null ||
                line.PortraitSide == NarrativePortraitSide.Hidden)
            {
                return;
            }

            Sprite portrait = line.Character.GetPortrait(line.Emotion);

            if (portrait == null)
                return;

            NarrativeRect layout;

            if (profile == null)
            {
                layout = line.PortraitSide == NarrativePortraitSide.Right
                    ? new NarrativeRect(0.62f, 0.18f, 0.36f, 0.78f)
                    : new NarrativeRect(0.02f, 0.18f, 0.36f, 0.78f);
            }
            else
            {
                layout = profile.GetRect(
                    line.PortraitSide == NarrativePortraitSide.Right
                        ? NarrativeLayoutElement.RightPortrait
                        : NarrativeLayoutElement.LeftPortrait);
            }

            if (line.PortraitSide == NarrativePortraitSide.Center)
            {
                layout.x = 0.5f - layout.width * 0.5f;
                layout.Clamp();
            }

            Rect target = ToGuiRect(screen, layout);
            Rect textureRect = portrait.textureRect;
            Rect uv = new Rect(
                textureRect.x / portrait.texture.width,
                textureRect.y / portrait.texture.height,
                textureRect.width / portrait.texture.width,
                textureRect.height / portrait.texture.height);
            GUI.DrawTextureWithTexCoords(target, portrait.texture, uv);
        }

        private void DrawLibraryTab()
        {
            _libraryScroll = EditorGUILayout.BeginScrollView(_libraryScroll);
            EditorGUILayout.LabelField("Narrative Library", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create reusable story assets, then assign them below.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            DrawCreateAssetButton<NarrativeCharacter>("Character");
            DrawCreateAssetButton<NarrativeEmotion>("Emotion");
            DrawCreateAssetButton<NarrativeAudioProfile>("Audio Profile");
            DrawCreateAssetButton<DialoguePresentationProfile>("Presentation");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            DrawCreateAssetButton<NarrativeVariable>("Variable");
            DrawCreateAssetButton<NarrativeFlag>("Story Flag");
            DrawCreateAssetButton<NarrativeEvent>("Gameplay Event");
            DrawCreateAssetButton<TweeImportProfile>("Twee Import Profile");
            EditorGUILayout.EndHorizontal();

            if (_story != null)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Story Assignments", EditorStyles.boldLabel);
                SerializedObject serialized = new SerializedObject(_story);
                serialized.Update();
                EditorGUILayout.PropertyField(
                    serialized.FindProperty("presentationProfile"));
                EditorGUILayout.PropertyField(
                    serialized.FindProperty("characters"), true);
                EditorGUILayout.PropertyField(
                    serialized.FindProperty("variables"), true);
                EditorGUILayout.PropertyField(
                    serialized.FindProperty("flags"), true);
                EditorGUILayout.PropertyField(
                    serialized.FindProperty("gameplayEvents"), true);

                if (serialized.ApplyModifiedProperties())
                    EditorUtility.SetDirty(_story);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCreateAssetButton<T>(string label)
            where T : ScriptableObject
        {
            if (GUILayout.Button(
                    "New " + label,
                    GUILayout.Height(32f),
                    GUILayout.ExpandWidth(true)))
            {
                CreateLibraryAsset<T>(label.Replace(" ", string.Empty));
            }
        }

        private void CreateLibraryAsset<T>(string defaultName)
            where T : ScriptableObject
        {
            string directory = GetStoryDirectory();
            string path = EditorUtility.SaveFilePanelInProject(
                "Create " + ObjectNames.NicifyVariableName(typeof(T).Name),
                defaultName,
                "asset",
                "Choose an asset location.",
                directory);

            if (string.IsNullOrWhiteSpace(path))
                return;

            T asset = NarrativeAssetFactory.CreateLibraryAsset<T>(path, _story);

            if (asset is DialoguePresentationProfile && _story != null &&
                _story.PresentationProfile == null)
            {
                SerializedObject serialized = new SerializedObject(_story);
                serialized.FindProperty("presentationProfile")
                    .objectReferenceValue = asset;
                serialized.ApplyModifiedProperties();
            }
        }

        private void DrawValidationTab()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(
                    "Run Validation", EditorStyles.toolbarButton))
            {
                _issues.Clear();
                _issues.AddRange(NarrativeValidator.Validate(_story));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                $"{CountIssues(NarrativeValidationSeverity.Error)} errors · " +
                $"{CountIssues(NarrativeValidationSeverity.Warning)} warnings",
                EditorStyles.miniLabel,
                GUILayout.Width(170f));
            EditorGUILayout.EndHorizontal();

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Run validation to check the story.",
                    MessageType.Info);
                return;
            }

            _validationScroll = EditorGUILayout.BeginScrollView(
                _validationScroll);

            foreach (NarrativeValidationIssue issue in _issues)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                Color previous = GUI.color;
                GUI.color = GetSeverityColour(issue.Severity);
                GUILayout.Label(
                    issue.Severity.ToString(),
                    EditorStyles.boldLabel,
                    GUILayout.Width(68f));
                GUI.color = previous;
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(
                    issue.Code + " · " + issue.Message,
                    EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();

                GUI.enabled = issue.Context != null;
                if (GUILayout.Button("Locate", GUILayout.Width(58f)))
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                }

                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private int CountIssues(NarrativeValidationSeverity severity)
        {
            int count = 0;

            foreach (NarrativeValidationIssue issue in _issues)
            {
                if (issue.Severity == severity)
                    count++;
            }

            return count;
        }

        private void DrawFaqTab()
        {
            _faqScroll = EditorGUILayout.BeginScrollView(_faqScroll);
            EditorGUILayout.LabelField("Visual Novel Builder FAQ",
                EditorStyles.boldLabel);
            DrawFaq("What is a Story?",
                "The full dialogue graph, library links and presentation profile.");
            DrawFaq("What is a Character?",
                "A reusable speaker with portraits, emotions and typing audio.");
            DrawFaq("What is an Emotion?",
                "A reusable emotion label mapped to a character portrait.");
            DrawFaq("Do lines need choices?",
                "No. Dialogue lines can connect directly to any next node.");
            DrawFaq("What are Flags and Variables?",
                "Saved story state used by conditions and Set Value nodes.");
            DrawFaq("Can variables power alchemy?",
                "Yes. Gameplay can read, change and observe numeric variables.");
            DrawFaq("What is a Gameplay Event?",
                "A ScriptableObject event channel that calls game logic.");
            DrawFaq("How do I connect nodes?",
                "Drag from an output port to another node's input port.");
            DrawFaq("How do I place the UI?",
                "Use Presentation, select an element, then drag or resize it.");
            DrawFaq("How many choices?",
                "Each Choice node supports up to five visible options.");
            DrawFaq("How does saving work?",
                "NarrativeRunner creates JSON and can store PlayerPrefs slots.");
            DrawFaq("Can I import Twine?",
                "Use Import Twee, review detection, then confirm the profile.");
            DrawFaq("What is an Import Profile?",
                "Reusable colour, speaker, emotion and audio mappings.");
            DrawFaq("Can two colours share a speaker?",
                "Yes. Add both colours to one speaker mapping.");
            DrawFaq("What are placeholder speakers?",
                "Characters created safely for unmapped colours.");
            DrawFaq("How do I use it in-game?",
                "Add NarrativeRunner and NarrativePresenter to one GameObject.");
            DrawFaq("Does preview fire events?",
                "No. Editor preview skips gameplay events safely.");
            EditorGUILayout.EndScrollView();
        }

        private void DrawStartHereTab()
        {
            _startHereScroll = EditorGUILayout.BeginScrollView(
                _startHereScroll);
            EditorGUILayout.LabelField(
                "Start Here · Creation Order",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Follow these steps from top to bottom.",
                MessageType.Info);

            DrawGuideStep(
                "1. Create the Story",
                "Click New Story, or Import Twee to convert a SugarCube story.");
            DrawGuideStep(
                "2. Build the Library",
                "Open Library. Create characters, emotions and optional audio, variables, flags or events.");
            DrawGuideStep(
                "3. Configure Characters",
                "Select each Character asset. Add portraits for its emotions and an optional audio profile.");
            DrawGuideStep(
                "4. Add Dialogue",
                "Open Flow. Add a Dialogue Line, select it and enter its speaker, emotion and text.");
            DrawGuideStep(
                "5. Connect the Path",
                "Drag Start's Next port into the first line, then connect every line until an End node.");
            DrawGuideStep(
                "6. Add Optional Logic",
                "Use Choice, Condition, Set Value, Gameplay Event or Wait nodes only where needed.");
            DrawGuideStep(
                "7. Design the Screen",
                "Open Presentation. Select, drag and resize the dialogue, portraits and choices.");
            DrawGuideStep(
                "8. Test the Story",
                "Open Preview, press Start Preview and follow every route and choice.");
            DrawGuideStep(
                "9. Validate and Save",
                "Run Validation, repair every error, then click Save.");
            DrawGuideStep(
                "10. Use It In-Game",
                "Add NarrativeRunner and NarrativePresenter to one GameObject, assign the Story and call StartStory().");

            EditorGUILayout.EndScrollView();
        }

        private static void DrawGuideStep(string title, string instruction)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                instruction,
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        private static string GetTabLabel(BuilderTab tab)
        {
            if (tab == BuilderTab.FAQ)
                return "FAQ";
            if (tab == BuilderTab.StartHere)
                return "Start Here";

            return ObjectNames.NicifyVariableName(tab.ToString());
        }

        private static void DrawFaq(string question, string answer)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(question, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(answer, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        private string GetStoryDirectory()
        {
            string path = _story == null
                ? "Assets"
                : AssetDatabase.GetAssetPath(_story);

            if (string.IsNullOrWhiteSpace(path))
                return "Assets";

            string directory = Path.GetDirectoryName(path)
                ?.Replace('\\', '/');
            return string.IsNullOrWhiteSpace(directory)
                ? "Assets"
                : directory;
        }

        private static Rect FitAspect(Rect bounds, float aspect)
        {
            float width = bounds.width;
            float height = width / aspect;

            if (height > bounds.height)
            {
                height = bounds.height;
                width = height * aspect;
            }

            return new Rect(
                bounds.x + (bounds.width - width) * 0.5f,
                bounds.y + (bounds.height - height) * 0.5f,
                width,
                height);
        }

        private static Rect ToGuiRect(
            Rect canvas,
            NarrativeRect value)
        {
            return new Rect(
                canvas.x + value.x * canvas.width,
                canvas.y + (1f - value.y - value.height) * canvas.height,
                value.width * canvas.width,
                value.height * canvas.height);
        }

        private static Color GetSeverityColour(
            NarrativeValidationSeverity severity)
        {
            switch (severity)
            {
                case NarrativeValidationSeverity.Error:
                    return new Color(1f, 0.35f, 0.40f);
                case NarrativeValidationSeverity.Warning:
                    return new Color(1f, 0.68f, 0.20f);
                default:
                    return new Color(0.75f, 0.42f, 1f);
            }
        }
    }
}
