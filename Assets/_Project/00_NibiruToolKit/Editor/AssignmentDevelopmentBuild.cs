using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Reproducible assignment Development Build for the toolkit demo stress scenario.
/// </summary>
public static class AssignmentDevelopmentBuild
{
    private const string DemoScene =
        "Assets/_Project/02_Scenes/ToolkitDEMO_Scene.unity";

    private const string StressDefine =
        "NIBIRU_ASSIGNMENT_STRESS";

    [MenuItem("Tools/Miss Nibiru/Assignment/Build + Run Stress Development Build")]
    public static void BuildAndRunStressScenario()
    {
        BuildStressScenario(autoRun: true, connectProfiler: false);
    }

    [MenuItem("Tools/Miss Nibiru/Assignment/Build + Run Stress With Profiler")]
    public static void BuildAndRunStressScenarioWithProfiler()
    {
        BuildStressScenario(autoRun: true, connectProfiler: true);
    }

    /// <summary>
    /// Command-line entry point for a build machine that has Unity installed/licensed.
    /// Example:
    /// Unity -batchmode -quit -projectPath . \
    ///   -executeMethod AssignmentDevelopmentBuild.BuildStressScenarioFromCommandLine
    /// </summary>
    public static void BuildStressScenarioFromCommandLine()
    {
        BuildStressScenario(autoRun: false, connectProfiler: false);
    }

    private static void BuildStressScenario(bool autoRun, bool connectProfiler)
    {
        if (!File.Exists(DemoScene))
        {
            throw new FileNotFoundException(
                "Assignment demo scene was not found.",
                DemoScene);
        }

        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string locationPathName = GetBuildLocation(target);

        string directory = Path.GetDirectoryName(locationPathName);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        BuildOptions options = BuildOptions.Development;

        if (autoRun)
            options |= BuildOptions.AutoRunPlayer;

        if (connectProfiler)
            options |= BuildOptions.ConnectWithProfiler;

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { DemoScene },
            locationPathName = locationPathName,
            target = target,
            options = options,
            extraScriptingDefines = new[] { StressDefine }
        };

        Debug.Log(
            "NIBIRU_DEVELOPMENT_BUILD_START " +
            $"target={target} " +
            $"scene={DemoScene} " +
            $"output={locationPathName} " +
            $"stressDefine={StressDefine} " +
            $"connectProfiler={connectProfiler}");

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        Debug.Log(
            "NIBIRU_DEVELOPMENT_BUILD_RESULT " +
            $"result={summary.result} " +
            $"duration={summary.totalTime} " +
            $"sizeBytes={summary.totalSize} " +
            $"errors={summary.totalErrors} " +
            $"warnings={summary.totalWarnings} " +
            $"output={locationPathName}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Assignment Development Build failed: {summary.result}");
        }
    }

    private static string GetBuildLocation(BuildTarget target)
    {
        const string root = "Builds/AssignmentStress";

        switch (target)
        {
            case BuildTarget.StandaloneOSX:
                return $"{root}/NibiruToolkitStress.app";

            case BuildTarget.StandaloneWindows64:
                return $"{root}/NibiruToolkitStress.exe";

            case BuildTarget.StandaloneLinux64:
                return $"{root}/NibiruToolkitStress";

            default:
                throw new NotSupportedException(
                    "Assignment stress build currently supports desktop standalone " +
                    "targets only. Switch the active Build Target to macOS, " +
                    "Windows 64-bit, or Linux 64-bit and run the command again.");
        }
    }
}
