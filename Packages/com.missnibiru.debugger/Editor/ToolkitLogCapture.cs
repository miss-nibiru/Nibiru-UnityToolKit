using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Debugger.Editor
{
    public sealed class ToolkitCapturedLog
    {
        public DateTime Time { get; }
        public string Message { get; }
        public string StackTrace { get; }
        public LogType Type { get; }

        public ToolkitCapturedLog(
            DateTime time,
            string message,
            string stackTrace,
            LogType type)
        {
            Time = time;
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            Type = type;
        }
    }

    [InitializeOnLoad]
    public static class ToolkitLogCapture
    {
        private const int MaximumEntries = 300;

        private static readonly List<ToolkitCapturedLog> EntriesInternal =
            new List<ToolkitCapturedLog>();

        public static event Action Changed;

        public static IReadOnlyList<ToolkitCapturedLog> Entries =>
            EntriesInternal;

        public static bool IsPaused { get; set; }

        static ToolkitLogCapture()
        {
            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;
        }

        public static void Clear()
        {
            EntriesInternal.Clear();
            Changed?.Invoke();
        }

        internal static void AddForTests(
            string message,
            LogType type = LogType.Log)
        {
            Add(message, string.Empty, type);
        }

        private static void HandleLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (IsPaused)
                return;

            Add(condition, stackTrace, type);
        }

        private static void Add(
            string message,
            string stackTrace,
            LogType type)
        {
            EntriesInternal.Add(
                new ToolkitCapturedLog(
                    DateTime.Now,
                    message,
                    stackTrace,
                    type));

            while (EntriesInternal.Count > MaximumEntries)
                EntriesInternal.RemoveAt(0);

            Changed?.Invoke();
        }
    }
}
