using System;
using System.Collections.Generic;
using MissNibiru.Waves.Layouts;
using UnityEngine;

namespace MissNibiru.Waves.Planning
{
    public sealed class WaveLayoutWaveStatistics
    {
        private readonly Dictionary<SpawnableDefinition, int>
            _spawnCounts =
                new Dictionary<SpawnableDefinition, int>();

        public int TotalSpawns { get; internal set; }
        public int EnemySpawns { get; internal set; }
        public int HazardSpawns { get; internal set; }
        public int PickupSpawns { get; internal set; }
        public int OtherSpawns { get; internal set; }
        public int MaximumSimultaneous { get; internal set; }
        public int MaximumSimultaneousEnemies { get; internal set; }
        public int MaximumActiveEnemies { get; internal set; }
        public float EstimatedDuration { get; internal set; }
        public float SpawnRatePerSecond { get; internal set; }
        public float OccupiedCellPercentage { get; internal set; }
        public bool ExceedsBudget { get; internal set; }

        public IReadOnlyDictionary<SpawnableDefinition, int>
            SpawnCounts => _spawnCounts;

        internal void AddSpawnable(
            SpawnableDefinition spawnable)
        {
            if (spawnable == null)
                return;

            _spawnCounts.TryGetValue(spawnable, out int count);
            _spawnCounts[spawnable] = count + 1;
        }
    }

    public sealed class WaveLayoutSequenceStatistics
    {
        public int TotalSpawns { get; internal set; }
        public int EnemySpawns { get; internal set; }
        public int HazardSpawns { get; internal set; }
        public int PickupSpawns { get; internal set; }
        public int OtherSpawns { get; internal set; }
        public int MaximumSimultaneous { get; internal set; }
        public int MaximumSimultaneousEnemies { get; internal set; }
        public int MaximumActiveEnemies { get; internal set; }
        public float EstimatedDuration { get; internal set; }
    }

    public static class WaveLayoutCalculator
    {
        private const float TimeBucket = 0.001f;

        public static WaveLayoutWaveStatistics CalculateWave(
            WaveLayoutData layout,
            int waveIndex)
        {
            WaveLayoutWaveStatistics statistics =
                new WaveLayoutWaveStatistics();

            if (layout == null ||
                waveIndex < 0 ||
                waveIndex >= layout.Waves.Count ||
                layout.Waves[waveIndex] == null)
            {
                return statistics;
            }

            WaveLayoutWave wave = layout.Waves[waveIndex];
            List<WaveSpawnInstruction> instructions =
                WaveLayoutCompiler.CompileWave(layout, waveIndex);

            Dictionary<int, int> simultaneous =
                new Dictionary<int, int>();

            Dictionary<int, int> simultaneousEnemies =
                new Dictionary<int, int>();

            HashSet<Vector2Int> occupiedCells =
                new HashSet<Vector2Int>();

            float finalSpawnTime = 0f;

            foreach (WaveSpawnInstruction instruction in instructions)
            {
                statistics.TotalSpawns++;
                finalSpawnTime = Mathf.Max(
                    finalSpawnTime,
                    instruction.SpawnTime);

                int timeKey = Mathf.RoundToInt(
                    instruction.SpawnTime / TimeBucket);

                simultaneous.TryGetValue(
                    timeKey,
                    out int simultaneousCount);

                simultaneousCount++;
                simultaneous[timeKey] = simultaneousCount;
                statistics.MaximumSimultaneous = Mathf.Max(
                    statistics.MaximumSimultaneous,
                    simultaneousCount);

                CountKind(statistics, instruction.Spawnable.Kind);
                statistics.AddSpawnable(instruction.Spawnable);

                if (instruction.Spawnable.Kind ==
                    SpawnableKind.Enemy)
                {
                    simultaneousEnemies.TryGetValue(
                        timeKey,
                        out int enemyCount);
                    enemyCount++;
                    simultaneousEnemies[timeKey] = enemyCount;
                    statistics.MaximumSimultaneousEnemies =
                        Mathf.Max(
                            statistics.MaximumSimultaneousEnemies,
                            enemyCount);
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
                    if (WaveLayoutGeometry.IsInside(layout, cell))
                        occupiedCells.Add(cell);
                }
            }

            statistics.MaximumActiveEnemies =
                statistics.EnemySpawns;

            float activeDuration = wave.UsesDuration
                ? Mathf.Max(0f, wave.Duration)
                : finalSpawnTime;

            statistics.EstimatedDuration =
                Mathf.Max(0f, wave.InitialDelay) + activeDuration;

            statistics.SpawnRatePerSecond =
                statistics.TotalSpawns == 0
                    ? 0f
                    : statistics.TotalSpawns /
                      Mathf.Max(1f, activeDuration);

            int totalCells = Mathf.Max(
                1,
                layout.Columns * layout.Rows);

            statistics.OccupiedCellPercentage =
                occupiedCells.Count * 100f / totalCells;

            statistics.ExceedsBudget =
                statistics.MaximumActiveEnemies >
                Mathf.Max(1, layout.ActiveEnemyBudget);

            return statistics;
        }

        public static WaveLayoutSequenceStatistics
            CalculateSequence(WaveLayoutData layout)
        {
            WaveLayoutSequenceStatistics sequence =
                new WaveLayoutSequenceStatistics();

            if (layout == null || layout.Waves == null)
                return sequence;

            for (int index = 0;
                 index < layout.Waves.Count;
                 index++)
            {
                WaveLayoutWaveStatistics wave =
                    CalculateWave(layout, index);

                sequence.TotalSpawns += wave.TotalSpawns;
                sequence.EnemySpawns += wave.EnemySpawns;
                sequence.HazardSpawns += wave.HazardSpawns;
                sequence.PickupSpawns += wave.PickupSpawns;
                sequence.OtherSpawns += wave.OtherSpawns;
                sequence.EstimatedDuration += wave.EstimatedDuration;
                sequence.MaximumSimultaneous = Mathf.Max(
                    sequence.MaximumSimultaneous,
                    wave.MaximumSimultaneous);
                sequence.MaximumSimultaneousEnemies = Mathf.Max(
                    sequence.MaximumSimultaneousEnemies,
                    wave.MaximumSimultaneousEnemies);
                sequence.MaximumActiveEnemies = Mathf.Max(
                    sequence.MaximumActiveEnemies,
                    wave.MaximumActiveEnemies);
            }

            return sequence;
        }

        private static void CountKind(
            WaveLayoutWaveStatistics statistics,
            SpawnableKind kind)
        {
            switch (kind)
            {
                case SpawnableKind.Enemy:
                    statistics.EnemySpawns++;
                    break;

                case SpawnableKind.Hazard:
                    statistics.HazardSpawns++;
                    break;

                case SpawnableKind.Pickup:
                    statistics.PickupSpawns++;
                    break;

                default:
                    statistics.OtherSpawns++;
                    break;
            }
        }
    }
}
