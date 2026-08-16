# Toolkit Debugger Specification

Package: `com.missnibiru.debugger`

Tool: **Tools > Miss Nibiru > Toolkit Debugger**

## Problem

Reusable Unity packages introduce recurring integration problems that are expensive to diagnose manually: invalid package metadata, broken assembly references, missing scripts, broken serialized references, duplicate IDs, and incorrectly configured toolkit assets.

The same checks were being repeated through the Project window, Inspector, Console, and source code. The Toolkit Debugger automates that recurring inspection workflow from one editor window.

## Goal

Provide a read-only Unity Editor diagnostic tool that can scan the project at different scopes, report actionable issues, and surface current editor logs without requiring a developer to read the debugger source.

## Users

- toolkit author maintaining reusable packages;
- developer integrating one or more Miss Nibiru packages;
- designer/technical user who needs to locate configuration problems without tracing package source.

## Entry point

Open from:

**Tools > Miss Nibiru > Toolkit Debugger**

## Scan modes

### Quick Scan

Checks the high-value project configuration needed for normal toolkit use, including packages, assemblies, open scenes, and Miss Nibiru ScriptableObjects.

Use this as the normal first diagnostic pass.

### Selection Scan

Checks the currently selected folders, assets, and/or scene objects.

Use this when the problem is known to be isolated to a specific area.

### Full Project Scan

Checks the wider project asset set, including ScriptableObjects and prefabs under `Assets`.

Use this when a problem cannot be localized or before a larger handoff/release validation pass.

## Diagnostic categories

Issues are classified into the following categories:

- Packages
- Assemblies
- Assets
- Scenes
- Toolkit

Each issue also has a severity:

- Info
- Warning
- Error

## Required issue data

A `ToolkitDebugIssue` should contain enough information for a user to act on it:

- severity;
- category;
- stable issue code;
- human-readable message;
- suggested action when available;
- asset path when available;
- Unity object context when available.

## Checks

The debugger is intended to detect recurring problems including:

- invalid package JSON;
- invalid or mismatched package versions/dependencies;
- missing or duplicate assembly definitions;
- broken assembly references;
- missing scripts;
- broken serialized references;
- duplicate or empty stable IDs;
- common configuration problems in Information, Waves, Enemies, Combat, and UI;
- current Unity warnings, errors, and logs from the editor session.

Checks should be deterministic and based on inspectable project state.

## Interface

The editor window contains four primary pages:

- **Dashboard** — scan entry points and summary information;
- **Issues** — scan findings with filtering/search/location actions;
- **Live Logs** — current editor-session Unity logs with filtering;
- **FAQ** — adoption/help information that reduces the need to inspect source.

## Report behaviour

A completed scan produces a `ToolkitDebugReport` containing:

- scan mode;
- completion time;
- scan duration;
- issue collection;
- error count;
- warning count;
- information count.

The report can also be converted to text for copy/paste debugging and handoff.

## Safety requirements

The debugger is **read-only**.

A scan must not:

- rewrite project assets;
- delete or create project content;
- modify package manifests automatically;
- change assembly definitions automatically;
- fix IDs silently;
- alter scene objects as part of diagnosis.

The tool may locate/select an issue or provide a suggested action, but the developer remains responsible for applying the fix intentionally.

## Scope boundary

Toolkit Debugger is not a replacement for:

- Rider/IDE breakpoints;
- runtime debugging of arbitrary game logic;
- the Unity Profiler;
- automated code generation;
- automatic repair of project files.

It requires the debugger Editor assembly to compile before the window can open.

## Extension point

New package/project checks should be added as scanner rules that emit `ToolkitDebugIssue` objects.

A new check should:

1. identify a repeatable integration/configuration failure;
2. classify it with an appropriate severity/category;
3. provide an actionable message;
4. include a path/context where practical;
5. remain read-only;
6. avoid reporting normal valid configurations as errors;
7. include/update EditMode tests when the rule can be tested deterministically.

## Adoption success criteria

The tool satisfies its purpose when another developer can:

1. open it from the Unity Tools menu;
2. choose an appropriate scan scope;
3. understand how many issues were found;
4. filter/search the findings;
5. locate the affected asset/object where possible;
6. understand the suggested corrective action;
7. copy a diagnostic report for handoff;

without opening `ToolkitProjectScanner.cs` or `ToolkitDebuggerWindow.cs`.
