using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using PackageManagerDependencyInfo =
    UnityEditor.PackageManager.DependencyInfo;
using PackageManagerPackageInfo =
    UnityEditor.PackageManager.PackageInfo;
using PackageManagerPackageSource =
    UnityEditor.PackageManager.PackageSource;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace MissNibiru.Debugger.Editor
{
    public static class ToolkitProjectScanner
    {
        [Serializable]
        private sealed class PackageManifestData
        {
            public string name;
            public string version;
        }

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
        }

        private sealed class IdAssetRecord
        {
            public string Path;
            public UnityEngine.Object Asset;
        }

        private static readonly Regex SemanticVersionPattern =
            new Regex(
                @"^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$",
                RegexOptions.Compiled);

        private static readonly HashSet<string> DuplicateIdKeys =
            new HashSet<string>(StringComparer.Ordinal);

        private static readonly HashSet<string> RuntimeToolkitPackages =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "com.missnibiru.core",
                "com.missnibiru.combat",
                "com.missnibiru.enemies",
                "com.missnibiru.information",
                "com.missnibiru.ui",
                "com.missnibiru.waves"
            };

        private static bool ScanCancelled;

        public static ToolkitDebugReport Scan(ToolkitScanMode mode)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ToolkitDebugReport report = new ToolkitDebugReport(mode);
            Dictionary<string, IdAssetRecord> ids =
                new Dictionary<string, IdAssetRecord>(
                    StringComparer.Ordinal);

            DuplicateIdKeys.Clear();
            ScanCancelled = false;

            try
            {
                if (mode == ToolkitScanMode.Selection)
                {
                    ScanSelection(report, ids);
                }
                else
                {
                    ScanPackages(report);
                    ScanAssemblyDefinitions(report);
                    ScanOpenScenes(report);
                    ScanScriptableAssets(
                        report,
                        ids,
                        mode == ToolkitScanMode.FullProject);

                    if (mode == ToolkitScanMode.FullProject &&
                        !ScanCancelled)
                        ScanProjectPrefabs(report);
                }
            }
            catch (Exception exception)
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Error,
                        ToolkitDebugCategory.Toolkit,
                        "DBG000",
                        "The scan stopped unexpectedly.",
                        exception.Message));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                stopwatch.Stop();
                report.Complete(stopwatch.Elapsed);
            }

            return report;
        }

        public static bool IsValidSemanticVersion(string version)
        {
            return !string.IsNullOrWhiteSpace(version) &&
                   SemanticVersionPattern.IsMatch(version.Trim());
        }

        private static void ScanPackages(ToolkitDebugReport report)
        {
            if (!Directory.Exists("Packages"))
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Error,
                        ToolkitDebugCategory.Packages,
                        "PKG001",
                        "The Packages folder is missing.",
                        "Restore the project Packages folder."));
                return;
            }

            ValidateProjectJson(
                report,
                "Packages/manifest.json",
                "PKG011");

            ValidateProjectJson(
                report,
                "Packages/packages-lock.json",
                "PKG012");

            Dictionary<string, string> localNames =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            foreach (string directory in
                     Directory.GetDirectories("Packages"))
            {
                string manifestPath = Path.Combine(
                    directory,
                    "package.json");

                if (!File.Exists(manifestPath))
                    continue;

                string assetPath = NormalizePath(manifestPath);
                PackageManifestData manifest;

                try
                {
                    manifest = JsonUtility.FromJson<PackageManifestData>(
                        File.ReadAllText(manifestPath));
                }
                catch (Exception)
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Error,
                            ToolkitDebugCategory.Packages,
                            "PKG002",
                            "Package JSON is invalid.",
                            "Correct the JSON syntax.",
                            assetPath));
                    continue;
                }

                if (manifest == null ||
                    string.IsNullOrWhiteSpace(manifest.name))
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Error,
                            ToolkitDebugCategory.Packages,
                            "PKG003",
                            "Package name is missing.",
                            "Add the package name.",
                            assetPath));
                    continue;
                }

                if (localNames.ContainsKey(manifest.name))
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Error,
                            ToolkitDebugCategory.Packages,
                            "PKG004",
                            $"Duplicate package name: {manifest.name}.",
                            "Give each package a unique name.",
                            assetPath));
                }
                else
                {
                    localNames.Add(manifest.name, manifest.version);
                }

                if (!IsValidSemanticVersion(manifest.version))
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Warning,
                            ToolkitDebugCategory.Packages,
                            "PKG005",
                            $"{manifest.name} has an invalid version.",
                            "Use a version like 1.0.0.",
                            assetPath));
                }

                CheckPackageAssemblyFolder(
                    report,
                    directory,
                    "Runtime",
                    manifest.name);

                CheckPackageAssemblyFolder(
                    report,
                    directory,
                    "Editor",
                    manifest.name);

                string runtimeFolder = Path.Combine(
                    directory,
                    "Runtime");

                if (RuntimeToolkitPackages.Contains(manifest.name) &&
                    (!Directory.Exists(runtimeFolder) ||
                     Directory.GetFiles(
                             runtimeFolder,
                             "*.cs",
                             SearchOption.AllDirectories).Length == 0))
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Error,
                            ToolkitDebugCategory.Packages,
                            "PKG008",
                            $"{manifest.name} has no runtime scripts.",
                            "Restore the complete package folder.",
                            assetPath));
                }

                if (!File.Exists(Path.Combine(directory, "README.md")) &&
                    !Directory.Exists(Path.Combine(directory, "Documentation~")))
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Info,
                            ToolkitDebugCategory.Packages,
                            "PKG010",
                            $"{manifest.name} has no package documentation.",
                            "Add setup and extension notes.",
                            assetPath));
                }
            }

            PackageManagerPackageInfo[] registered =
                PackageManagerPackageInfo.GetAllRegisteredPackages();

            Dictionary<string, PackageManagerPackageInfo> installed =
                new Dictionary<string, PackageManagerPackageInfo>(
                    StringComparer.Ordinal);

            foreach (PackageManagerPackageInfo package in registered)
            {
                if (package != null &&
                    !string.IsNullOrWhiteSpace(package.name))
                {
                    installed[package.name] = package;
                }
            }

            foreach (PackageManagerPackageInfo package in registered)
            {
                if (package == null ||
                    package.dependencies == null ||
                    !package.name.StartsWith(
                        "com.missnibiru.",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (PackageManagerDependencyInfo dependency
                         in package.dependencies)
                {
                    if (string.IsNullOrWhiteSpace(dependency.name) ||
                        !dependency.name.StartsWith(
                            "com.missnibiru.",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!installed.TryGetValue(
                            dependency.name,
                            out PackageManagerPackageInfo found))
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Error,
                                ToolkitDebugCategory.Packages,
                                "PKG006",
                                $"{package.name} requires " +
                                $"missing package {dependency.name}.",
                                "Install the required package."));
                    }
                    else if (!string.Equals(
                                 dependency.version,
                                 found.version,
                                 StringComparison.Ordinal))
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Warning,
                                ToolkitDebugCategory.Packages,
                                "PKG007",
                                $"{package.name} requests " +
                                $"{dependency.name} {dependency.version}, " +
                                $"but {found.version} is installed.",
                                "Synchronize package versions."));
                    }
                }
            }
        }

        private static void CheckPackageAssemblyFolder(
            ToolkitDebugReport report,
            string packageDirectory,
            string folderName,
            string packageName)
        {
            string folder = Path.Combine(packageDirectory, folderName);

            if (!Directory.Exists(folder))
                return;

            string[] scripts = Directory.GetFiles(
                folder,
                "*.cs",
                SearchOption.AllDirectories);

            if (scripts.Length == 0)
                return;

            string[] definitions = Directory.GetFiles(
                folder,
                "*.asmdef",
                SearchOption.AllDirectories);

            if (definitions.Length == 0)
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Warning,
                        ToolkitDebugCategory.Assemblies,
                        "ASM001",
                        $"{packageName}/{folderName} has scripts " +
                        "but no assembly definition.",
                        "Add an assembly definition.",
                        NormalizePath(folder)));
            }
        }

        private static void ValidateProjectJson(
            ToolkitDebugReport report,
            string path,
            string code)
        {
            if (!File.Exists(path))
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Error,
                        ToolkitDebugCategory.Packages,
                        code,
                        $"{path} is missing.",
                        "Restore the project package file.",
                        path));
                return;
            }

            try
            {
                JsonUtility.FromJson<PackageManifestData>(
                    File.ReadAllText(path));
            }
            catch (Exception)
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Error,
                        ToolkitDebugCategory.Packages,
                        code,
                        $"{path} contains invalid JSON.",
                        "Correct the JSON syntax.",
                        path));
            }
        }

        private static void ScanAssemblyDefinitions(
            ToolkitDebugReport report)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:AssemblyDefinitionAsset");

            Dictionary<string, string> names =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            List<Tuple<string, AssemblyDefinitionData>> definitions =
                new List<Tuple<string, AssemblyDefinitionData>>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string text = ReadAssetText(path);
                bool shouldValidate =
                    IsProjectOwnedAssemblyPath(path);

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                AssemblyDefinitionData data;

                try
                {
                    data = JsonUtility.FromJson<AssemblyDefinitionData>(text);
                }
                catch (Exception)
                {
                    if (shouldValidate)
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Error,
                                ToolkitDebugCategory.Assemblies,
                                "ASM002",
                                "Assembly definition JSON is invalid.",
                                "Correct the JSON syntax.",
                                path));
                    }

                    continue;
                }

                if (data == null || string.IsNullOrWhiteSpace(data.name))
                {
                    if (shouldValidate)
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Error,
                                ToolkitDebugCategory.Assemblies,
                                "ASM003",
                                "Assembly name is missing.",
                                "Add a unique assembly name.",
                                path));
                    }

                    continue;
                }

                if (names.TryGetValue(data.name, out string previousPath))
                {
                    if (shouldValidate ||
                        IsProjectOwnedAssemblyPath(previousPath))
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Error,
                                ToolkitDebugCategory.Assemblies,
                                "ASM004",
                                $"Duplicate assembly name: {data.name}.",
                                $"Also used by {previousPath}.",
                                path));
                    }
                }
                else
                {
                    names.Add(data.name, path);
                }

                if (shouldValidate)
                {
                    definitions.Add(
                        new Tuple<string, AssemblyDefinitionData>(
                            path,
                            data));
                }
            }

            foreach (Tuple<string, AssemblyDefinitionData> definition
                     in definitions)
            {
                string path = definition.Item1;
                AssemblyDefinitionData data = definition.Item2;

                if (data.references == null)
                    continue;

                foreach (string reference in data.references)
                {
                    if (string.IsNullOrWhiteSpace(reference))
                        continue;

                    if (reference.StartsWith(
                            "GUID:",
                            StringComparison.Ordinal))
                    {
                        string referenceGuid = reference.Substring(5);
                        string referencePath =
                            AssetDatabase.GUIDToAssetPath(referenceGuid);

                        if (string.IsNullOrWhiteSpace(referencePath))
                        {
                            report.Add(
                                new ToolkitDebugIssue(
                                    ToolkitDebugSeverity.Error,
                                    ToolkitDebugCategory.Assemblies,
                                    "ASM005",
                                    $"{data.name} has a broken GUID reference.",
                                    "Replace the missing assembly reference.",
                                    path));
                        }
                    }
                    else if (!names.ContainsKey(reference))
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Error,
                                ToolkitDebugCategory.Assemblies,
                                "ASM006",
                                $"{data.name} references missing " +
                                $"assembly {reference}.",
                                "Install or repair the assembly.",
                                path));
                    }
                }
            }
        }

        private static void ScanSelection(
            ToolkitDebugReport report,
            Dictionary<string, IdAssetRecord> ids)
        {
            UnityEngine.Object[] selection = Selection.objects;

            if (selection == null || selection.Length == 0)
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Info,
                        ToolkitDebugCategory.Assets,
                        "AST001",
                        "Nothing is selected.",
                        "Select assets or scene objects."));
                return;
            }

            HashSet<string> scannedPaths =
                new HashSet<string>(StringComparer.Ordinal);

            foreach (UnityEngine.Object selected in selection)
            {
                if (selected == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(selected);

                if (!string.IsNullOrWhiteSpace(path) &&
                    AssetDatabase.IsValidFolder(path))
                {
                    ScanFolder(report, ids, path, scannedPaths);
                    continue;
                }

                ScanSelectedObject(report, ids, selected, path);
            }
        }

        private static void ScanFolder(
            ToolkitDebugReport report,
            Dictionary<string, IdAssetRecord> ids,
            string folder,
            HashSet<string> scannedPaths)
        {
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:ScriptableObject",
                         new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!scannedPaths.Add(path))
                    continue;

                ScriptableObject asset =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                ScanScriptableObject(report, ids, asset, path);
            }

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Prefab",
                         new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!scannedPaths.Add(path))
                    continue;

                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);

                ScanGameObject(report, prefab, path);
            }
        }

        private static void ScanSelectedObject(
            ToolkitDebugReport report,
            Dictionary<string, IdAssetRecord> ids,
            UnityEngine.Object selected,
            string path)
        {
            if (selected is ScriptableObject scriptableObject)
            {
                ScanScriptableObject(report, ids, scriptableObject, path);
            }
            else if (selected is GameObject gameObject)
            {
                ScanGameObject(report, gameObject, path);
            }
            else if (selected is Component component)
            {
                ScanGameObject(report, component.gameObject, path);
            }
            else
            {
                ScanSerializedReferences(report, selected, path);
            }
        }

        private static void ScanScriptableAssets(
            ToolkitDebugReport report,
            Dictionary<string, IdAssetRecord> ids,
            bool includeAllProjectAssets)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:ScriptableObject",
                new[] { "Assets" });

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ScriptableObject asset =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset == null)
                    continue;

                Type type = asset.GetType();
                bool isToolkitAsset = type.Namespace != null &&
                                      type.Namespace.StartsWith(
                                          "MissNibiru.",
                                          StringComparison.Ordinal);

                if (!includeAllProjectAssets && !isToolkitAsset)
                    continue;

                if (includeAllProjectAssets &&
                    EditorUtility.DisplayCancelableProgressBar(
                        "Toolkit Debugger",
                        $"Scanning {path}",
                        guids.Length == 0
                            ? 1f
                            : (float)index / guids.Length))
                {
                    ScanCancelled = true;
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Info,
                            ToolkitDebugCategory.Assets,
                            "AST099",
                            "The full scan was cancelled."));
                    break;
                }

                ScanScriptableObject(report, ids, asset, path);
            }
        }

        private static void ScanProjectPrefabs(
            ToolkitDebugReport report)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets" });

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Toolkit Debugger",
                        $"Scanning {path}",
                        guids.Length == 0
                            ? 1f
                            : (float)index / guids.Length))
                {
                    ScanCancelled = true;
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Info,
                            ToolkitDebugCategory.Assets,
                            "AST099",
                            "The full scan was cancelled."));
                    break;
                }

                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);

                ScanGameObject(report, prefab, path);
            }
        }

        private static void ScanOpenScenes(ToolkitDebugReport report)
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);

                if (!scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    ScanGameObject(report, root, scene.path);
            }
        }

        private static void ScanScriptableObject(
            ToolkitDebugReport report,
            Dictionary<string, IdAssetRecord> ids,
            ScriptableObject asset,
            string path)
        {
            if (asset == null)
                return;

            ScanSerializedReferences(report, asset, path);

            SerializedObject serialized = new SerializedObject(asset);
            Type type = asset.GetType();
            string fullName = type.FullName ?? type.Name;

            if (type.Namespace != null &&
                type.Namespace.StartsWith(
                    "MissNibiru.",
                    StringComparison.Ordinal))
            {
                ScanStableId(report, ids, serialized, asset, path, fullName);
            }

            switch (fullName)
            {
                case "MissNibiru.Waves.Layouts.SpawnableDefinition":
                    RequireObjectReference(
                        report,
                        serialized,
                        "prefab",
                        "DAT101",
                        "Spawnable has no prefab.",
                        "Assign the runtime prefab.",
                        path,
                        asset);

                    SerializedProperty footprint =
                        serialized.FindProperty("gridFootprint");

                    if (footprint != null &&
                        (footprint.vector2IntValue.x < 1 ||
                         footprint.vector2IntValue.y < 1))
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Error,
                                ToolkitDebugCategory.Toolkit,
                                "DAT102",
                                "Spawnable footprint is invalid.",
                                "Use at least 1 x 1.",
                                path,
                                asset));
                    }
                    break;

                case "MissNibiru.Core.Patterns.PatternDefinition":
                    SerializedProperty tokens =
                        serialized.FindProperty("tokens");

                    if (tokens == null || tokens.arraySize == 0)
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Warning,
                                ToolkitDebugCategory.Toolkit,
                                "DAT201",
                                "Pattern has no tokens.",
                                "Add at least one token.",
                                path,
                                asset));
                    }
                    else
                    {
                        CheckNullArrayEntries(
                            report,
                            tokens,
                            "DAT202",
                            "Pattern contains an empty token.",
                            path,
                            asset);
                    }
                    break;

                case "MissNibiru.Information.Data.InformationEntry":
                    RequireString(
                        report,
                        serialized,
                        "displayName",
                        "DAT301",
                        "Information entry has no display name.",
                        path,
                        asset);
                    break;

                case "MissNibiru.Waves.Layouts.WaveLayoutData":
                    RequireObjectReference(
                        report,
                        serialized,
                        "catalog",
                        "DAT401",
                        "Wave layout has no catalog.",
                        "Assign a spawn catalog.",
                        path,
                        asset);
                    break;
            }
        }

        private static void ScanStableId(
            ToolkitDebugReport report,
            Dictionary<string, IdAssetRecord> ids,
            SerializedObject serialized,
            UnityEngine.Object asset,
            string path,
            string typeName)
        {
            SerializedProperty id = serialized.FindProperty("id");

            if (id == null ||
                id.propertyType != SerializedPropertyType.String)
            {
                return;
            }

            string value = id.stringValue == null
                ? string.Empty
                : id.stringValue.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Warning,
                        ToolkitDebugCategory.Toolkit,
                        "DAT001",
                        $"{asset.name} has no stable ID.",
                        "Generate a unique ID.",
                        path,
                        asset));
                return;
            }

            string key = typeName + "|" + value;

            if (!ids.TryGetValue(key, out IdAssetRecord previous))
            {
                ids.Add(
                    key,
                    new IdAssetRecord
                    {
                        Path = path,
                        Asset = asset
                    });
                return;
            }

            report.Add(
                new ToolkitDebugIssue(
                    ToolkitDebugSeverity.Error,
                    ToolkitDebugCategory.Toolkit,
                    "DAT002",
                    $"Duplicate {typeName} ID: {value}.",
                    $"Also used by {previous.Path}.",
                    path,
                    asset));

            if (DuplicateIdKeys.Add(key))
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Error,
                        ToolkitDebugCategory.Toolkit,
                        "DAT002",
                        $"Duplicate {typeName} ID: {value}.",
                        $"Also used by {path}.",
                        previous.Path,
                        previous.Asset));
            }
        }

        private static void ScanGameObject(
            ToolkitDebugReport report,
            GameObject root,
            string path)
        {
            if (root == null)
                return;

            foreach (Transform transform in
                     root.GetComponentsInChildren<Transform>(true))
            {
                GameObject gameObject = transform.gameObject;
                int missing =
                    GameObjectUtility
                        .GetMonoBehavioursWithMissingScriptCount(gameObject);

                if (missing > 0)
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Error,
                            string.IsNullOrWhiteSpace(path)
                                ? ToolkitDebugCategory.Scenes
                                : Path.GetExtension(path) == ".unity"
                                    ? ToolkitDebugCategory.Scenes
                                    : ToolkitDebugCategory.Assets,
                            "SCR001",
                            $"{gameObject.name} has {missing} missing " +
                            "script reference(s).",
                            "Remove or restore the scripts.",
                            path,
                            gameObject));
                }

                Component[] components =
                    gameObject.GetComponents<Component>();

                foreach (Component component in components)
                {
                    if (component == null)
                        continue;

                    ScanSerializedReferences(report, component, path);
                    ScanToolkitComponent(report, component, path);
                }
            }
        }

        private static void ScanToolkitComponent(
            ToolkitDebugReport report,
            Component component,
            string path)
        {
            string fullName = component.GetType().FullName;

            if (string.IsNullOrWhiteSpace(fullName))
                return;

            SerializedObject serialized = new SerializedObject(component);

            switch (fullName)
            {
                case "MissNibiru.Enemies.Actor.EnemyActor":
                    if (!HasComponent(
                            component.gameObject,
                            "MissNibiru.Core.Health.HealthComponent"))
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Error,
                                ToolkitDebugCategory.Toolkit,
                                "ENM001",
                                "EnemyActor has no HealthComponent.",
                                "Add HealthComponent.",
                                path,
                                component));
                    }

                    RequireObjectReference(
                        report,
                        serialized,
                        "movementBehaviourSource",
                        "ENM002",
                        "EnemyActor has no movement behaviour.",
                        "Assign a movement component.",
                        path,
                        component);
                    break;

                case "MissNibiru.Combat.Projectiles.PrefabProjectileEmitter":
                    RequireObjectReference(
                        report,
                        serialized,
                        "projectilePrefab",
                        "CBT001",
                        "Projectile emitter has no projectile prefab.",
                        "Assign a projectile prefab.",
                        path,
                        component);
                    break;

                case "MissNibiru.Enemies.Attacks.EnemyProjectileAttack":
                    RequireObjectReference(
                        report,
                        serialized,
                        "configuration",
                        "CBT002",
                        "Enemy projectile attack has no configuration.",
                        "Assign an attack configuration.",
                        path,
                        component);

                    RequireObjectReference(
                        report,
                        serialized,
                        "emitterSource",
                        "CBT003",
                        "Enemy projectile attack has no emitter.",
                        "Assign a projectile emitter.",
                        path,
                        component);
                    break;

                case "MissNibiru.UI.Health.HealthBarUI":
                    RequireObjectReference(
                        report,
                        serialized,
                        "fillImage",
                        "UI001",
                        "Health bar has no fill image.",
                        "Assign the fill image.",
                        path,
                        component);
                    break;

                case "MissNibiru.Information.Unity.InformationSource":
                    RequireObjectReference(
                        report,
                        serialized,
                        "entry",
                        "INF001",
                        "Information source has no entry.",
                        "Assign an information entry.",
                        path,
                        component);

                    RequireObjectReference(
                        report,
                        serialized,
                        "collection",
                        "INF002",
                        "Information source has no collection.",
                        "Assign a collection component.",
                        path,
                        component);
                    break;

                case "MissNibiru.Information.Unity.InformationCollectionComponent":
                    RequireObjectReference(
                        report,
                        serialized,
                        "database",
                        "INF003",
                        "Information collection has no database.",
                        "Assign an information database.",
                        path,
                        component);
                    break;

                case "MissNibiru.Waves.Execution.WaveRunner":
                    RequireObjectReference(
                        report,
                        serialized,
                        "spawnerSource",
                        "WAV001",
                        "WaveRunner has no spawner.",
                        "Assign an IWaveSpawner component.",
                        path,
                        component);

                    SerializedProperty authored =
                        serialized.FindProperty("authoredLayout");
                    SerializedProperty origin =
                        serialized.FindProperty("authoredLayoutOrigin");

                    if (authored != null &&
                        authored.objectReferenceValue != null &&
                        origin != null &&
                        origin.objectReferenceValue == null)
                    {
                        report.Add(
                            new ToolkitDebugIssue(
                                ToolkitDebugSeverity.Error,
                                ToolkitDebugCategory.Toolkit,
                                "WAV002",
                                "Authored wave layout has no origin.",
                                "Assign the grid origin.",
                                path,
                                component));
                    }
                    break;
            }
        }

        private static void ScanSerializedReferences(
            ToolkitDebugReport report,
            UnityEngine.Object target,
            string path)
        {
            if (target == null)
                return;

            SerializedObject serialized;

            try
            {
                serialized = new SerializedObject(target);
            }
            catch (Exception)
            {
                return;
            }

            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = true;

                if (property.propertyPath == "m_Script" ||
                    property.propertyType !=
                    SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (property.objectReferenceValue == null &&
                    property.objectReferenceInstanceIDValue != 0)
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Error,
                            ToolkitDebugCategory.Assets,
                            "REF001",
                            $"{target.name} has a broken reference: " +
                            property.displayName + ".",
                            "Restore or clear the reference.",
                            path,
                            target));
                }
            }
        }

        private static void RequireObjectReference(
            ToolkitDebugReport report,
            SerializedObject serialized,
            string propertyName,
            string code,
            string message,
            string action,
            string path,
            UnityEngine.Object context)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property != null &&
                property.objectReferenceValue == null)
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Warning,
                        ToolkitDebugCategory.Toolkit,
                        code,
                        message,
                        action,
                        path,
                        context));
            }
        }

        private static void RequireString(
            ToolkitDebugReport report,
            SerializedObject serialized,
            string propertyName,
            string code,
            string message,
            string path,
            UnityEngine.Object context)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property != null &&
                string.IsNullOrWhiteSpace(property.stringValue))
            {
                report.Add(
                    new ToolkitDebugIssue(
                        ToolkitDebugSeverity.Warning,
                        ToolkitDebugCategory.Toolkit,
                        code,
                        message,
                        "Enter a display name.",
                        path,
                        context));
            }
        }

        private static void CheckNullArrayEntries(
            ToolkitDebugReport report,
            SerializedProperty array,
            string code,
            string message,
            string path,
            UnityEngine.Object context)
        {
            for (int index = 0; index < array.arraySize; index++)
            {
                SerializedProperty element =
                    array.GetArrayElementAtIndex(index);

                if (element.propertyType ==
                        SerializedPropertyType.ObjectReference &&
                    element.objectReferenceValue == null)
                {
                    report.Add(
                        new ToolkitDebugIssue(
                            ToolkitDebugSeverity.Error,
                            ToolkitDebugCategory.Toolkit,
                            code,
                            message,
                            "Assign or remove the empty slot.",
                            path,
                            context));
                    return;
                }
            }
        }

        private static bool HasComponent(
            GameObject gameObject,
            string fullTypeName)
        {
            foreach (Component component in
                     gameObject.GetComponents<Component>())
            {
                if (component != null &&
                    component.GetType().FullName == fullTypeName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReadAssetText(string path)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);

            TextAsset asset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            if (asset != null)
                return asset.text;

            UnityEngine.Object mainAsset =
                AssetDatabase.LoadMainAssetAtPath(path);

            if (mainAsset == null)
                return string.Empty;

            System.Reflection.PropertyInfo textProperty =
                mainAsset.GetType().GetProperty("text");

            return textProperty == null
                ? string.Empty
                : textProperty.GetValue(mainAsset) as string ??
                  string.Empty;
        }

        internal static bool IsProjectOwnedAssemblyPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            if (path.StartsWith("Assets/", StringComparison.Ordinal))
                return true;

            if (!path.StartsWith("Packages/", StringComparison.Ordinal))
                return false;

            PackageManagerPackageInfo package;

            try
            {
                package =
                    PackageManagerPackageInfo.FindForAssetPath(path);
            }
            catch (Exception)
            {
                return false;
            }

            if (package == null)
                return false;

            return package.source == PackageManagerPackageSource.Embedded ||
                   package.source == PackageManagerPackageSource.Local;
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/');
        }
    }
}
