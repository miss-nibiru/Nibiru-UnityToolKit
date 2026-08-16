using NUnit.Framework;
using MissNibiru.Debugger.Editor;
using UnityEngine;

namespace MissNibiru.Debugger.Tests
{
    public sealed class ToolkitDebuggerTests
    {
        [TestCase("0.1.0")]
        [TestCase("1.0.0")]
        [TestCase("2.4.1-preview.3")]
        public void SemanticVersions_AcceptValidValues(string version)
        {
            Assert.That(
                ToolkitProjectScanner
                    .IsValidSemanticVersion(version),
                Is.True);
        }

        [TestCase("")]
        [TestCase("1")]
        [TestCase("1.0")]
        [TestCase("version-one")]
        public void SemanticVersions_RejectInvalidValues(string version)
        {
            Assert.That(
                ToolkitProjectScanner
                    .IsValidSemanticVersion(version),
                Is.False);
        }

        [Test]
        public void Report_CountsIssueSeverities()
        {
            ToolkitDebugReport report =
                new ToolkitDebugReport(
                    ToolkitScanMode.Quick);

            report.Add(
                new ToolkitDebugIssue(
                    ToolkitDebugSeverity.Error,
                    ToolkitDebugCategory.Assets,
                    "ERR",
                    "Error"));

            report.Add(
                new ToolkitDebugIssue(
                    ToolkitDebugSeverity.Warning,
                    ToolkitDebugCategory.Assets,
                    "WRN",
                    "Warning"));

            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.InfoCount, Is.Zero);
            Assert.That(report.IsClean, Is.False);
        }

        [Test]
        public void LiveLogs_KeepLatestThreeHundredEntries()
        {
            ToolkitLogCapture.Clear();

            for (int index = 0; index < 305; index++)
            {
                ToolkitLogCapture.AddForTests(
                    "Log " + index,
                    LogType.Log);
            }

            Assert.That(
                ToolkitLogCapture.Entries.Count,
                Is.EqualTo(300));

            Assert.That(
                ToolkitLogCapture.Entries[0].Message,
                Is.EqualTo("Log 5"));

            ToolkitLogCapture.Clear();
        }

        [Test]
        public void AssemblyScope_IncludesProjectAndEmbeddedPackages()
        {
            Assert.That(
                ToolkitProjectScanner.IsProjectOwnedAssemblyPath(
                    "Assets/Project.asmdef"),
                Is.True);

            Assert.That(
                ToolkitProjectScanner.IsProjectOwnedAssemblyPath(
                    "Packages/com.missnibiru.debugger/Editor/" +
                    "MissNibiru.Debugger.Editor.asmdef"),
                Is.True);
        }

        [Test]
        public void AssemblyScope_ExcludesRegistryPackageInternals()
        {
            Assert.That(
                ToolkitProjectScanner.IsProjectOwnedAssemblyPath(
                    "Packages/com.example.registry/DoesNotExist.asmdef"),
                Is.False);
        }
    }
}
