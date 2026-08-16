using System;
using System.Collections.Generic;
using MissNibiru.Waves.Layouts;
using MissNibiru.Waves.Planning;
using UnityEngine;

namespace MissNibiru.Waves.Editor
{
    public enum WaveLayoutValidationSeverity
    {
        Success,
        Warning,
        Error
    }

    public sealed class WaveLayoutValidationIssue
    {
        public WaveLayoutValidationSeverity Severity { get; }
        public string Message { get; }
        public UnityEngine.Object Context { get; }

        public WaveLayoutValidationIssue(
            WaveLayoutValidationSeverity severity,
            string message,
            UnityEngine.Object context = null)
        {
            Severity = severity;
            Message = message;
            Context = context;
        }
    }

    public static class WaveLayoutValidator
    {
        public static List<WaveLayoutValidationIssue> Validate(
            WaveLayoutData layout,
            Transform worldOrigin,
            bool requireWorldOrigin = true)
        {
            List<WaveLayoutValidationIssue> issues =
                new List<WaveLayoutValidationIssue>();

            if (layout == null)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    "No layout selected.");
                return issues;
            }

            if (requireWorldOrigin && worldOrigin == null)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    "World origin is missing.",
                    layout);
            }

            if (layout.Catalog == null)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Warning,
                    "Spawn catalog is missing.",
                    layout);
            }
            else
            {
                ValidateCatalog(issues, layout.Catalog);
            }

            if (layout.Columns < 1 ||
                layout.Rows < 1 ||
                layout.Columns > WaveLayoutData.MaximumGridSize ||
                layout.Rows > WaveLayoutData.MaximumGridSize)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    "Grid size is invalid.",
                    layout);
            }

            if (layout.CellSize <= 0f)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    "Cell size is invalid.",
                    layout);
            }

            if (layout.Waves == null || layout.Waves.Count == 0)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    "No wave data exists.",
                    layout);
                return issues;
            }

            for (int waveIndex = 0;
                 waveIndex < layout.Waves.Count;
                 waveIndex++)
            {
                ValidateWave(issues, layout, waveIndex);
            }

            if (issues.Count == 0)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Success,
                    "Layout is ready.",
                    layout);
            }

            return issues;
        }

        private static void ValidateCatalog(
            ICollection<WaveLayoutValidationIssue> issues,
            SpawnCatalog catalog)
        {
            for (int index = 0;
                 index < catalog.Spawnables.Count;
                 index++)
            {
                SpawnableDefinition spawnable =
                    catalog.Spawnables[index];

                if (spawnable == null)
                {
                    Add(
                        issues,
                        WaveLayoutValidationSeverity.Error,
                        $"Catalog spawnable {index + 1} is broken.",
                        catalog);
                    continue;
                }

                if (spawnable.Prefab == null)
                {
                    Add(
                        issues,
                        WaveLayoutValidationSeverity.Error,
                        $"'{spawnable.DisplayName}' has no prefab.",
                        spawnable);
                }
            }

            for (int index = 0;
                 index < catalog.Formations.Count;
                 index++)
            {
                if (catalog.Formations[index] == null)
                {
                    Add(
                        issues,
                        WaveLayoutValidationSeverity.Error,
                        $"Catalog formation {index + 1} is broken.",
                        catalog);
                }
            }
        }

        private static void ValidateWave(
            List<WaveLayoutValidationIssue> issues,
            WaveLayoutData layout,
            int waveIndex)
        {
            WaveLayoutWave wave = layout.Waves[waveIndex];
            string waveLabel = $"Wave {waveIndex + 1}";

            if (wave == null)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"{waveLabel} is missing.",
                    layout);
                return;
            }

            if (wave.InitialDelay < 0f ||
                wave.Duration < 0f)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"{waveLabel} timing is invalid.",
                    layout);
            }

            if (wave.UsesDuration && wave.Duration <= 0f)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"{waveLabel} needs a duration.",
                    layout);
            }

            if (wave.Placements == null ||
                wave.Placements.Count == 0)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Warning,
                    $"{waveLabel} has no placements.",
                    layout);
                return;
            }

            HashSet<string> placementIds =
                new HashSet<string>(StringComparer.Ordinal);

            for (int placementIndex = 0;
                 placementIndex < wave.Placements.Count;
                 placementIndex++)
            {
                WaveLayoutPlacement placement =
                    wave.Placements[placementIndex];

                if (placement != null &&
                    !placementIds.Add(placement.Id))
                {
                    Add(
                        issues,
                        WaveLayoutValidationSeverity.Error,
                        $"{waveLabel} has duplicate placement IDs.",
                        layout);
                }

                ValidatePlacement(
                    issues,
                    layout,
                    wave,
                    waveIndex,
                    placementIndex);
            }

            List<WaveSpawnInstruction> instructions =
                WaveLayoutCompiler.CompileWave(
                    layout,
                    waveIndex);

            ValidateCompiledCells(
                issues,
                layout,
                waveLabel,
                instructions);

            WaveLayoutWaveStatistics statistics =
                WaveLayoutCalculator.CalculateWave(
                    layout,
                    waveIndex);

            if (statistics.ExceedsBudget)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Warning,
                    $"{waveLabel} exceeds enemy budget.",
                    layout);
            }
        }

        private static void ValidatePlacement(
            List<WaveLayoutValidationIssue> issues,
            WaveLayoutData layout,
            WaveLayoutWave wave,
            int waveIndex,
            int placementIndex)
        {
            WaveLayoutPlacement placement =
                wave.Placements[placementIndex];

            string label =
                $"Wave {waveIndex + 1}, placement " +
                $"{placementIndex + 1}";

            if (placement == null)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"{label} is missing.",
                    layout);
                return;
            }

            if (!placement.Enabled)
                return;

            if (placement.Spawnable == null)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"{label} has no spawnable.",
                    layout);
                return;
            }

            SpawnableDefinition spawnable = placement.Spawnable;

            if (spawnable.Prefab == null)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"'{spawnable.DisplayName}' has no prefab.",
                    spawnable);
            }

            Vector2Int footprint = spawnable.GridFootprint;
            Vector2Int pivot = spawnable.FootprintPivot;

            if (footprint.x < 1 || footprint.y < 1)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"'{spawnable.DisplayName}' footprint is invalid.",
                    spawnable);
            }

            if (pivot.x < 0 || pivot.y < 0 ||
                pivot.x >= footprint.x ||
                pivot.y >= footprint.y)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"'{spawnable.DisplayName}' pivot is invalid.",
                    spawnable);
            }

            if (placement.SpawnDelay < 0f ||
                placement.SequenceInterval < 0f ||
                placement.RepeatInterval < 0f ||
                placement.Repetitions < 1)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Error,
                    $"{label} timing is invalid.",
                    layout);
            }

            if (wave.UsesDuration &&
                placement.SpawnDelay > wave.Duration)
            {
                Add(
                    issues,
                    WaveLayoutValidationSeverity.Warning,
                    $"{label} starts after duration.",
                    layout);
            }
        }

        private static void ValidateCompiledCells(
            List<WaveLayoutValidationIssue> issues,
            WaveLayoutData layout,
            string waveLabel,
            List<WaveSpawnInstruction> instructions)
        {
            Dictionary<int, Dictionary<Vector2Int,
                WaveSpawnInstruction>> occupiedByTime =
                    new Dictionary<int,
                        Dictionary<Vector2Int,
                            WaveSpawnInstruction>>();

            bool reportedBoundary = false;
            bool reportedOverlap = false;

            foreach (WaveSpawnInstruction instruction in instructions)
            {
                int timeKey = Mathf.RoundToInt(
                    instruction.SpawnTime / 0.001f);

                if (!occupiedByTime.TryGetValue(
                        timeKey,
                        out Dictionary<Vector2Int,
                            WaveSpawnInstruction> occupied))
                {
                    occupied = new Dictionary<Vector2Int,
                        WaveSpawnInstruction>();
                    occupiedByTime.Add(timeKey, occupied);
                }

                IReadOnlyList<Vector2Int> cells =
                    WaveLayoutGeometry.GetOccupiedCells(
                        instruction.Spawnable,
                        instruction.Cell,
                        instruction.Rotation,
                        instruction.FlipHorizontal,
                        instruction.FlipVertical);

                foreach (Vector2Int cell in cells)
                {
                    if (!WaveLayoutGeometry.IsInside(layout, cell))
                    {
                        if (!reportedBoundary)
                        {
                            Add(
                                issues,
                                WaveLayoutValidationSeverity.Error,
                                $"{waveLabel} leaves grid bounds.",
                                layout);
                            reportedBoundary = true;
                        }

                        continue;
                    }

                    if (occupied.ContainsKey(cell))
                    {
                        if (!reportedOverlap)
                        {
                            Add(
                                issues,
                                WaveLayoutValidationSeverity.Error,
                                $"{waveLabel} has simultaneous overlap.",
                                layout);
                            reportedOverlap = true;
                        }
                    }
                    else
                    {
                        occupied.Add(cell, instruction);
                    }
                }
            }
        }

        private static void Add(
            ICollection<WaveLayoutValidationIssue> issues,
            WaveLayoutValidationSeverity severity,
            string message,
            UnityEngine.Object context = null)
        {
            issues.Add(
                new WaveLayoutValidationIssue(
                    severity,
                    message,
                    context));
        }
    }
}
