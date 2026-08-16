using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Debugger.Editor
{
    public enum ToolkitDebugSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum ToolkitDebugCategory
    {
        Packages,
        Assemblies,
        Assets,
        Scenes,
        Toolkit
    }

    public enum ToolkitScanMode
    {
        Quick,
        Selection,
        FullProject
    }

    public sealed class ToolkitDebugIssue
    {
        public ToolkitDebugSeverity Severity { get; }
        public ToolkitDebugCategory Category { get; }
        public string Code { get; }
        public string Message { get; }
        public string SuggestedAction { get; }
        public string AssetPath { get; }
        public UnityEngine.Object Context { get; }

        public ToolkitDebugIssue(
            ToolkitDebugSeverity severity,
            ToolkitDebugCategory category,
            string code,
            string message,
            string suggestedAction = "",
            string assetPath = "",
            UnityEngine.Object context = null)
        {
            Severity = severity;
            Category = category;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            SuggestedAction = suggestedAction ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            Context = context;
        }

        public override string ToString()
        {
            string location = string.IsNullOrWhiteSpace(AssetPath)
                ? string.Empty
                : $" [{AssetPath}]";

            string action = string.IsNullOrWhiteSpace(SuggestedAction)
                ? string.Empty
                : $" Fix: {SuggestedAction}";

            return $"[{Severity}] [{Category}] {Code}: " +
                   Message + location + action;
        }
    }

    public sealed class ToolkitDebugReport
    {
        private readonly List<ToolkitDebugIssue> _issues =
            new List<ToolkitDebugIssue>();

        public ToolkitScanMode Mode { get; }
        public DateTime CompletedAt { get; private set; }
        public TimeSpan Duration { get; private set; }
        public IReadOnlyList<ToolkitDebugIssue> Issues => _issues;

        public int ErrorCount => Count(ToolkitDebugSeverity.Error);
        public int WarningCount => Count(ToolkitDebugSeverity.Warning);
        public int InfoCount => Count(ToolkitDebugSeverity.Info);
        public bool IsClean => ErrorCount == 0 && WarningCount == 0;

        public ToolkitDebugReport(ToolkitScanMode mode)
        {
            Mode = mode;
        }

        public void Add(ToolkitDebugIssue issue)
        {
            if (issue != null)
                _issues.Add(issue);
        }

        public void Complete(TimeSpan duration)
        {
            Duration = duration;
            CompletedAt = DateTime.Now;
        }

        public int Count(ToolkitDebugSeverity severity)
        {
            int count = 0;

            foreach (ToolkitDebugIssue issue in _issues)
            {
                if (issue.Severity == severity)
                    count++;
            }

            return count;
        }

        public int Count(ToolkitDebugCategory category)
        {
            int count = 0;

            foreach (ToolkitDebugIssue issue in _issues)
            {
                if (issue.Category == category)
                    count++;
            }

            return count;
        }

        public string CreateTextReport()
        {
            List<string> lines = new List<string>
            {
                "Miss Nibiru Toolkit Debugger",
                $"Scan: {Mode}",
                $"Completed: {CompletedAt:yyyy-MM-dd HH:mm:ss}",
                $"Duration: {Duration.TotalSeconds:0.00}s",
                $"Errors: {ErrorCount}",
                $"Warnings: {WarningCount}",
                $"Information: {InfoCount}",
                string.Empty
            };

            if (_issues.Count == 0)
            {
                lines.Add("No issues found.");
            }
            else
            {
                foreach (ToolkitDebugIssue issue in _issues)
                    lines.Add(issue.ToString());
            }

            return string.Join("\n", lines);
        }
    }
}
