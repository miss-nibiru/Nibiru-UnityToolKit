using System;
using System.Collections.Generic;
using MissNibiru.Waves.Layouts;
using UnityEngine;

namespace MissNibiru.Waves.Planning
{
    public sealed class WaveSpawnInstruction
    {
        public SpawnableDefinition Spawnable { get; }
        public WaveLayoutPlacement Placement { get; }
        public Vector2Int Cell { get; }
        public float SpawnTime { get; }
        public WaveGridRotation Rotation { get; }
        public bool FlipHorizontal { get; }
        public bool FlipVertical { get; }
        internal int Order { get; }

        internal WaveSpawnInstruction(
            SpawnableDefinition spawnable,
            WaveLayoutPlacement placement,
            Vector2Int cell,
            float spawnTime,
            int order)
        {
            Spawnable = spawnable;
            Placement = placement;
            Cell = cell;
            SpawnTime = spawnTime;
            Rotation = placement.Rotation;
            FlipHorizontal = placement.FlipHorizontal;
            FlipVertical = placement.FlipVertical;
            Order = order;
        }
    }

    public static class WaveLayoutCompiler
    {
        public static List<WaveSpawnInstruction> CompileWave(
            WaveLayoutData layout,
            int waveIndex)
        {
            List<WaveSpawnInstruction> instructions =
                new List<WaveSpawnInstruction>();

            if (layout == null ||
                layout.Waves == null ||
                waveIndex < 0 ||
                waveIndex >= layout.Waves.Count)
            {
                return instructions;
            }

            WaveLayoutWave wave = layout.Waves[waveIndex];

            if (wave == null || wave.Placements == null)
                return instructions;

            int order = 0;

            foreach (WaveLayoutPlacement placement in wave.Placements)
            {
                if (placement == null ||
                    !placement.Enabled ||
                    placement.Spawnable == null)
                {
                    continue;
                }

                IReadOnlyList<Vector2Int> cells =
                    WaveLayoutGeometry.GetFormationCells(placement);

                int repetitions = Mathf.Max(
                    1,
                    placement.Repetitions);

                for (int repeat = 0;
                     repeat < repetitions;
                     repeat++)
                {
                    float repeatTime =
                        Mathf.Max(0f, placement.SpawnDelay) +
                        repeat * Mathf.Max(
                            0f,
                            placement.RepeatInterval);

                    for (int index = 0;
                         index < cells.Count;
                         index++)
                    {
                        float spawnTime = repeatTime;

                        if (placement.Sequential)
                        {
                            spawnTime += index * Mathf.Max(
                                0f,
                                placement.SequenceInterval);
                        }

                        instructions.Add(
                            new WaveSpawnInstruction(
                                placement.Spawnable,
                                placement,
                                cells[index],
                                spawnTime,
                                order++));
                    }
                }
            }

            instructions.Sort(CompareInstructions);
            return instructions;
        }

        private static int CompareInstructions(
            WaveSpawnInstruction left,
            WaveSpawnInstruction right)
        {
            int timeComparison =
                left.SpawnTime.CompareTo(right.SpawnTime);

            return timeComparison != 0
                ? timeComparison
                : left.Order.CompareTo(right.Order);
        }
    }
}
