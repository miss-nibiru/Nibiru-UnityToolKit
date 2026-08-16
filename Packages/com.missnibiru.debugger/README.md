# Nibiru Toolkit Debugger

Toolkit Debugger is a read-only Unity Editor diagnostic tool for Miss Nibiru
packages and the wider Unity project.

Open it from **Tools > Miss Nibiru > Toolkit Debugger**.

## Scans

- **Quick Scan** checks packages, assembly definitions, open scenes and Miss
  Nibiru ScriptableObjects.
- **Scan Selection** checks selected folders, assets and scene objects.
- **Full Project** checks every ScriptableObject and prefab under `Assets`.

## Detects

- Invalid package JSON and versions.
- Missing or mismatched package dependencies.
- Missing and duplicate assembly definitions.
- Broken assembly references.
- Missing scripts and broken serialized references.
- Duplicate or empty stable IDs.
- Common configuration problems in Information, Waves, Enemies, Combat and UI.
- Live Unity warnings, errors and logs from the current editor session.

## Safety

Scans never change project files. Every issue can be located or copied, and the
complete report can be copied for debugging or handoff.

Assembly validation covers project assets and embedded packages. Registry
package internals are used to resolve references but are not reported as
project errors.

Toolkit Debugger is not a replacement for Rider breakpoints. It requires its
Editor assembly to compile before its Unity window can open.
