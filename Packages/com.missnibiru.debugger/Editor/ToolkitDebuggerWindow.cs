using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Debugger.Editor
{
    public sealed class ToolkitDebuggerWindow : EditorWindow
    {
        private enum Page
        {
            Dashboard,
            Issues,
            LiveLogs,
            FAQ
        }

        private const string BrandBannerPath =
            "Packages/com.missnibiru.debugger/Editor/Branding/" +
            "NibiruMainBanner.png";

        private static readonly Color HeaderColour =
            new Color(0.11f, 0.075f, 0.17f);

        private static readonly Color AccentColour =
            new Color(0.72f, 0.32f, 0.95f);

        private static readonly Color SuccessColour =
            new Color(0.26f, 0.76f, 0.43f);

        private static readonly Color WarningColour =
            new Color(1f, 0.68f, 0.25f);

        private static readonly Color ErrorColour =
            new Color(0.96f, 0.30f, 0.34f);

        [SerializeField]
        private Page page;

        [SerializeField]
        private string issueSearch = string.Empty;

        [SerializeField]
        private int severityFilter;

        [SerializeField]
        private int categoryFilter;

        [SerializeField]
        private string logSearch = string.Empty;

        [SerializeField]
        private int logTypeFilter;

        [SerializeField]
        private bool showStackTraces;

        private ToolkitDebugReport _report;
        private Texture2D _brandBanner;
        private Vector2 _dashboardScroll;
        private Vector2 _issuesScroll;
        private Vector2 _logsScroll;
        private Vector2 _faqScroll;
        private string _status = "Ready to scan.";
        private ToolkitDebugSeverity _statusSeverity =
            ToolkitDebugSeverity.Info;

        private GUIStyle _headerTitle;
        private GUIStyle _headerSubtitle;
        private GUIStyle _metricNumber;
        private GUIStyle _wrap;
        private GUIStyle _issueTitle;

        [MenuItem("Tools/Miss Nibiru/Toolkit Debugger")]
        public static void Open()
        {
            ToolkitDebuggerWindow window =
                GetWindow<ToolkitDebuggerWindow>();

            window.titleContent = new GUIContent("Toolkit Debugger");
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }

        private void OnEnable()
        {
            _brandBanner = AssetDatabase.LoadAssetAtPath<Texture2D>(
                BrandBannerPath);

            ToolkitLogCapture.Changed += HandleLogsChanged;
        }

        private void OnDisable()
        {
            ToolkitLogCapture.Changed -= HandleLogsChanged;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();

            switch (page)
            {
                case Page.Issues:
                    DrawIssuesPage();
                    break;
                case Page.LiveLogs:
                    DrawLogsPage();
                    break;
                case Page.FAQ:
                    DrawFaqPage();
                    break;
                default:
                    DrawDashboard();
                    break;
            }

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
                header.xMax - 370f,
                header.y + 38f,
                354f,
                28f);

            float titleWidth = Mathf.Max(
                100f,
                tabs.x - titleX - 10f);

            GUI.Label(
                new Rect(
                    titleX,
                    header.y + 23f,
                    titleWidth,
                    28f),
                "Toolkit Debugger",
                _headerTitle);

            GUI.Label(
                new Rect(
                    titleX + 1f,
                    header.y + 55f,
                    titleWidth,
                    20f),
                "Find setup problems quickly.",
                _headerSubtitle);

            page = (Page)GUI.Toolbar(
                tabs,
                (int)page,
                new[]
                {
                    "Dashboard",
                    IssueTabLabel(),
                    LogTabLabel(),
                    "FAQ"
                });
        }

        private void DrawDashboard()
        {
            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(
                       _dashboardScroll,
                       GUILayout.ExpandHeight(true)))
            {
                _dashboardScroll = scroll.scrollPosition;
                EditorGUILayout.Space(8f);

                DrawScanToolbar();
                EditorGUILayout.Space(8f);

                if (_report == null)
                {
                    EditorGUILayout.HelpBox(
                        "Run a scan to inspect the project.",
                        MessageType.Info);
                    DrawScopeGuide();
                    return;
                }

                DrawMetrics();
                EditorGUILayout.Space(8f);

                using (new EditorGUILayout.VerticalScope(
                           EditorStyles.helpBox))
                {
                    GUILayout.Label(
                        "Latest Scan",
                        EditorStyles.boldLabel);

                    GUILayout.Label(
                        $"{_report.Mode} completed at " +
                        $"{_report.CompletedAt:HH:mm:ss} in " +
                        $"{_report.Duration.TotalSeconds:0.00}s.");

                    if (_report.IsClean)
                    {
                        EditorGUILayout.HelpBox(
                            "No errors or warnings found.",
                            MessageType.Info);
                    }
                    else if (GUILayout.Button("Review Issues"))
                    {
                        page = Page.Issues;
                    }
                }

                EditorGUILayout.Space(6f);
                DrawCategorySummary();
                EditorGUILayout.Space(6f);
                DrawScopeGuide();
            }
        }

        private void DrawScanToolbar()
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                GUILayout.Label("Project Scan", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "Quick Scan",
                                "Checks common setup.")))
                    {
                        RunScan(ToolkitScanMode.Quick);
                    }

                    if (GUILayout.Button(
                            new GUIContent(
                                "Scan Selection",
                                "Checks selected assets.")))
                    {
                        RunScan(ToolkitScanMode.Selection);
                    }

                    if (GUILayout.Button(
                            new GUIContent(
                                "Full Project",
                                "Checks all project assets.")))
                    {
                        RunScan(ToolkitScanMode.FullProject);
                    }

                    GUI.enabled = _report != null;

                    if (GUILayout.Button(
                            new GUIContent(
                                "Copy Report",
                                "Copies the current report.")))
                    {
                        CopyReport();
                    }

                    GUI.enabled = true;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(
                            "Open Unity Test Runner",
                            GUILayout.Width(190f)))
                    {
                        EditorApplication.ExecuteMenuItem(
                            "Window/General/Test Runner");
                    }
                }
            }
        }

        private void DrawMetrics()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawMetric(
                    "Errors",
                    _report.ErrorCount,
                    ErrorColour);

                DrawMetric(
                    "Warnings",
                    _report.WarningCount,
                    WarningColour);

                DrawMetric(
                    "Information",
                    _report.InfoCount,
                    AccentColour);

                DrawMetric(
                    "Total",
                    _report.Issues.Count,
                    Color.white);
            }
        }

        private void DrawMetric(
            string label,
            int value,
            Color colour)
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox,
                       GUILayout.MinHeight(72f)))
            {
                Color previous = _metricNumber.normal.textColor;
                _metricNumber.normal.textColor = colour;

                GUILayout.Label(
                    value.ToString(),
                    _metricNumber,
                    GUILayout.ExpandWidth(true));

                _metricNumber.normal.textColor = previous;

                GUILayout.Label(
                    label,
                    _headerSubtitle,
                    GUILayout.ExpandWidth(true));
            }
        }

        private void DrawCategorySummary()
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                GUILayout.Label(
                    "Issues by Area",
                    EditorStyles.boldLabel);

                foreach (ToolkitDebugCategory category in
                         Enum.GetValues(typeof(ToolkitDebugCategory)))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(category.ToString());
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(
                            _report.Count(category).ToString(),
                            EditorStyles.boldLabel);
                    }
                }
            }
        }

        private void DrawScopeGuide()
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                GUILayout.Label("Scan Types", EditorStyles.boldLabel);
                GUILayout.Label(
                    "Quick Scan — packages, assemblies, open scenes " +
                    "and Miss Nibiru data.",
                    _wrap);
                GUILayout.Label(
                    "Scan Selection — selected folders, assets or " +
                    "scene objects.",
                    _wrap);
                GUILayout.Label(
                    "Full Project — all project ScriptableObjects and " +
                    "prefabs. This can take longer.",
                    _wrap);
            }
        }

        private void DrawIssuesPage()
        {
            DrawScanToolbar();

            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.toolbar))
            {
                issueSearch = GUILayout.TextField(
                    issueSearch,
                    GUI.skin.FindStyle("ToolbarSearchTextField") ??
                    EditorStyles.toolbarTextField,
                    GUILayout.MinWidth(180f));

                severityFilter = EditorGUILayout.Popup(
                    severityFilter,
                    new[] { "All", "Errors", "Warnings", "Info" },
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(100f));

                categoryFilter = EditorGUILayout.Popup(
                    categoryFilter,
                    CategoryFilterLabels(),
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(112f));
            }

            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(
                       _issuesScroll,
                       GUILayout.ExpandHeight(true)))
            {
                _issuesScroll = scroll.scrollPosition;

                if (_report == null)
                {
                    EditorGUILayout.HelpBox(
                        "Run a scan first.",
                        MessageType.Info);
                    return;
                }

                int visible = 0;

                foreach (ToolkitDebugIssue issue in _report.Issues)
                {
                    if (!MatchesIssue(issue))
                        continue;

                    DrawIssue(issue);
                    visible++;
                }

                if (visible == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No issues match these filters.",
                        MessageType.Info);
                }
            }
        }

        private void DrawIssue(ToolkitDebugIssue issue)
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color previous = _issueTitle.normal.textColor;
                    _issueTitle.normal.textColor =
                        SeverityColour(issue.Severity);

                    GUILayout.Label(
                        $"{issue.Severity} · {issue.Code}",
                        _issueTitle);

                    _issueTitle.normal.textColor = previous;
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(
                        issue.Category.ToString(),
                        EditorStyles.miniLabel);
                }

                GUILayout.Label(issue.Message, _wrap);

                if (!string.IsNullOrWhiteSpace(issue.AssetPath))
                {
                    GUILayout.Label(
                        issue.AssetPath,
                        EditorStyles.miniLabel);
                }

                if (!string.IsNullOrWhiteSpace(issue.SuggestedAction))
                {
                    GUILayout.Label(
                        "Fix: " + issue.SuggestedAction,
                        _wrap);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    bool canLocate = issue.Context != null ||
                                     !string.IsNullOrWhiteSpace(
                                         issue.AssetPath);

                    GUI.enabled = canLocate;

                    if (GUILayout.Button(
                            "Locate",
                            GUILayout.Width(72f)))
                    {
                        LocateIssue(issue);
                    }

                    GUI.enabled = true;

                    if (GUILayout.Button(
                            "Copy",
                            GUILayout.Width(64f)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            issue.ToString();
                        SetStatus(
                            "Issue copied.",
                            ToolkitDebugSeverity.Info);
                    }
                }
            }
        }

        private void DrawLogsPage()
        {
            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.toolbar))
            {
                bool paused = GUILayout.Toggle(
                    ToolkitLogCapture.IsPaused,
                    "Pause",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(58f));

                ToolkitLogCapture.IsPaused = paused;

                if (GUILayout.Button(
                        "Clear",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(52f)))
                {
                    ToolkitLogCapture.Clear();
                }

                if (GUILayout.Button(
                        "Copy",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(50f)))
                {
                    CopyLogs();
                }

                showStackTraces = GUILayout.Toggle(
                    showStackTraces,
                    "Stacks",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(60f));

                GUILayout.FlexibleSpace();

                logSearch = GUILayout.TextField(
                    logSearch,
                    GUI.skin.FindStyle("ToolbarSearchTextField") ??
                    EditorStyles.toolbarTextField,
                    GUILayout.Width(210f));

                logTypeFilter = EditorGUILayout.Popup(
                    logTypeFilter,
                    new[] { "All", "Errors", "Warnings", "Logs" },
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(92f));
            }

            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(
                       _logsScroll,
                       GUILayout.ExpandHeight(true)))
            {
                _logsScroll = scroll.scrollPosition;
                IReadOnlyList<ToolkitCapturedLog> entries =
                    ToolkitLogCapture.Entries;

                int visible = 0;

                for (int index = entries.Count - 1;
                     index >= 0;
                     index--)
                {
                    ToolkitCapturedLog entry = entries[index];

                    if (!MatchesLog(entry))
                        continue;

                    DrawLog(entry);
                    visible++;
                }

                if (visible == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No captured logs match.",
                        MessageType.Info);
                }
            }
        }

        private void DrawLog(ToolkitCapturedLog entry)
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color previous = _issueTitle.normal.textColor;
                    _issueTitle.normal.textColor = LogColour(entry.Type);

                    GUILayout.Label(
                        entry.Type.ToString(),
                        _issueTitle,
                        GUILayout.Width(92f));

                    _issueTitle.normal.textColor = previous;
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(
                        entry.Time.ToString("HH:mm:ss"),
                        EditorStyles.miniLabel);
                }

                GUILayout.Label(entry.Message, _wrap);

                if (showStackTraces &&
                    !string.IsNullOrWhiteSpace(entry.StackTrace))
                {
                    GUILayout.Label(
                        entry.StackTrace,
                        EditorStyles.miniLabel);
                }
            }
        }

        private void DrawFaqPage()
        {
            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(
                       _faqScroll,
                       GUILayout.ExpandHeight(true)))
            {
                _faqScroll = scroll.scrollPosition;
                EditorGUILayout.Space(8f);

                DrawFaq(
                    "What does it debug?",
                    "Packages, assemblies, assets, open scenes, " +
                    "toolkit setups and live Unity logs.");

                DrawFaq(
                    "Is this a code debugger?",
                    "No. Use Rider for breakpoints and stepping through code.");

                DrawFaq(
                    "Does it change assets?",
                    "No. Scans are read-only. Locate opens the affected asset.");

                DrawFaq(
                    "Which scan should I use?",
                    "Start with Quick Scan. Use Selection for focused work " +
                    "and Full Project before a build or submission.");

                DrawFaq(
                    "What are Live Logs?",
                    "Console messages captured while this editor session runs.");

                DrawFaq(
                    "Can it run during compile errors?",
                    "It requires its Editor assembly to compile. Package and " +
                    "assembly checks reduce future setup failures.");
            }
        }

        private void DrawFaq(string question, string answer)
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
            Color colour = SeverityColour(_statusSeverity);
            Rect rect = GUILayoutUtility.GetRect(
                0f,
                24f,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(
                rect,
                new Color(colour.r, colour.g, colour.b, 0.18f));

            GUI.Label(
                new Rect(
                    rect.x + 8f,
                    rect.y + 3f,
                    rect.width - 16f,
                    18f),
                _status,
                EditorStyles.miniLabel);
        }

        private void RunScan(ToolkitScanMode mode)
        {
            _status = "Scanning...";
            Repaint();

            _report = ToolkitProjectScanner.Scan(mode);

            if (_report.ErrorCount > 0)
            {
                SetStatus(
                    $"Found {_report.ErrorCount} errors and " +
                    $"{_report.WarningCount} warnings.",
                    ToolkitDebugSeverity.Error);
            }
            else if (_report.WarningCount > 0)
            {
                SetStatus(
                    $"Found {_report.WarningCount} warnings.",
                    ToolkitDebugSeverity.Warning);
            }
            else
            {
                SetStatus(
                    "Scan completed successfully.",
                    ToolkitDebugSeverity.Info);
            }
        }

        private bool MatchesIssue(ToolkitDebugIssue issue)
        {
            if (severityFilter == 1 &&
                issue.Severity != ToolkitDebugSeverity.Error)
                return false;

            if (severityFilter == 2 &&
                issue.Severity != ToolkitDebugSeverity.Warning)
                return false;

            if (severityFilter == 3 &&
                issue.Severity != ToolkitDebugSeverity.Info)
                return false;

            if (categoryFilter > 0 &&
                issue.Category !=
                (ToolkitDebugCategory)(categoryFilter - 1))
                return false;

            if (string.IsNullOrWhiteSpace(issueSearch))
                return true;

            string query = issueSearch.Trim();

            return Contains(issue.Message, query) ||
                   Contains(issue.Code, query) ||
                   Contains(issue.AssetPath, query) ||
                   Contains(issue.Category.ToString(), query);
        }

        private bool MatchesLog(ToolkitCapturedLog entry)
        {
            bool isError = entry.Type == LogType.Error ||
                           entry.Type == LogType.Exception ||
                           entry.Type == LogType.Assert;

            if (logTypeFilter == 1 && !isError)
                return false;

            if (logTypeFilter == 2 &&
                entry.Type != LogType.Warning)
                return false;

            if (logTypeFilter == 3 &&
                entry.Type != LogType.Log)
                return false;

            return string.IsNullOrWhiteSpace(logSearch) ||
                   Contains(entry.Message, logSearch) ||
                   Contains(entry.StackTrace, logSearch);
        }

        private void LocateIssue(ToolkitDebugIssue issue)
        {
            UnityEngine.Object target = issue.Context;

            if (target == null &&
                !string.IsNullOrWhiteSpace(issue.AssetPath))
            {
                target = AssetDatabase.LoadMainAssetAtPath(
                    issue.AssetPath);
            }

            if (target != null)
            {
                Selection.activeObject = target;
                EditorGUIUtility.PingObject(target);
            }
            else if (!string.IsNullOrWhiteSpace(issue.AssetPath))
            {
                EditorUtility.RevealInFinder(issue.AssetPath);
            }
        }

        private void CopyReport()
        {
            if (_report == null)
                return;

            EditorGUIUtility.systemCopyBuffer =
                _report.CreateTextReport();

            SetStatus(
                "Report copied.",
                ToolkitDebugSeverity.Info);
        }

        private void CopyLogs()
        {
            List<string> lines = new List<string>();

            foreach (ToolkitCapturedLog entry in
                     ToolkitLogCapture.Entries)
            {
                lines.Add(
                    $"[{entry.Time:HH:mm:ss}] [{entry.Type}] " +
                    entry.Message);

                if (showStackTraces &&
                    !string.IsNullOrWhiteSpace(entry.StackTrace))
                {
                    lines.Add(entry.StackTrace);
                }
            }

            EditorGUIUtility.systemCopyBuffer =
                string.Join("\n", lines);

            SetStatus(
                "Logs copied.",
                ToolkitDebugSeverity.Info);
        }

        private string IssueTabLabel()
        {
            return _report == null
                ? "Issues"
                : $"Issues ({_report.Issues.Count})";
        }

        private static string LogTabLabel()
        {
            return $"Live Logs ({ToolkitLogCapture.Entries.Count})";
        }

        private static string[] CategoryFilterLabels()
        {
            return new[]
            {
                "All Areas",
                "Packages",
                "Assemblies",
                "Assets",
                "Scenes",
                "Toolkit"
            };
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrEmpty(value) &&
                   !string.IsNullOrEmpty(query) &&
                   value.IndexOf(
                       query,
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Color SeverityColour(
            ToolkitDebugSeverity severity)
        {
            switch (severity)
            {
                case ToolkitDebugSeverity.Error:
                    return ErrorColour;
                case ToolkitDebugSeverity.Warning:
                    return WarningColour;
                default:
                    return AccentColour;
            }
        }

        private static Color LogColour(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return ErrorColour;
                case LogType.Warning:
                    return WarningColour;
                default:
                    return SuccessColour;
            }
        }

        private void SetStatus(
            string message,
            ToolkitDebugSeverity severity)
        {
            _status = message;
            _statusSeverity = severity;
            Repaint();
        }

        private void HandleLogsChanged()
        {
            if (page == Page.LiveLogs)
                Repaint();
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
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            _headerSubtitle.normal.textColor =
                new Color(0.78f, 0.74f, 0.84f);

            _metricNumber = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 25
            };

            _wrap = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            _issueTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
        }
    }
}
