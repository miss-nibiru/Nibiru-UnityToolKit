using System;
using System.Collections.Generic;
using System.IO;
using MissNibiru.Waves.Layouts;
using MissNibiru.Waves.Planning;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Waves.Editor
{
    public static class WaveLayoutEditorUtility
    {
        private const float TimeBucket = 0.001f;

        public static T CreateAsset<T>(
            string title,
            string defaultName,
            string directory = "Assets")
            where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject(
                title,
                defaultName,
                "asset",
                "Choose an asset location.",
                directory);

            if (string.IsNullOrWhiteSpace(path))
                return null;

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        public static string AssetDirectory(
            UnityEngine.Object context)
        {
            if (context == null)
                return "Assets";

            string path = AssetDatabase.GetAssetPath(context);

            if (string.IsNullOrWhiteSpace(path))
                return "Assets";

            string directory = Path.GetDirectoryName(path);
            return string.IsNullOrWhiteSpace(directory)
                ? "Assets"
                : directory.Replace('\\', '/');
        }

        public static string DisplayName(
            WaveLayoutWave wave,
            int index)
        {
            return wave == null ||
                   string.IsNullOrWhiteSpace(wave.WaveName)
                ? $"Wave {index + 1}"
                : wave.WaveName;
        }

        public static bool IsWaveSpatiallyValid(
            WaveLayoutData layout,
            int waveIndex,
            out string message)
        {
            message = string.Empty;

            if (layout == null ||
                waveIndex < 0 ||
                waveIndex >= layout.Waves.Count)
            {
                message = "Wave is missing.";
                return false;
            }

            Dictionary<int, Dictionary<Vector2Int,
                WaveSpawnInstruction>> occupiedByTime =
                    new Dictionary<int,
                        Dictionary<Vector2Int,
                            WaveSpawnInstruction>>();

            List<WaveSpawnInstruction> instructions =
                WaveLayoutCompiler.CompileWave(
                    layout,
                    waveIndex);

            foreach (WaveSpawnInstruction instruction in instructions)
            {
                IReadOnlyList<Vector2Int> occupied =
                    WaveLayoutGeometry.GetOccupiedCells(
                        instruction.Spawnable,
                        instruction.Cell,
                        instruction.Rotation,
                        instruction.FlipHorizontal,
                        instruction.FlipVertical);

                int timeKey = Mathf.RoundToInt(
                    instruction.SpawnTime / TimeBucket);

                if (!occupiedByTime.TryGetValue(
                        timeKey,
                        out Dictionary<Vector2Int,
                            WaveSpawnInstruction> timeCells))
                {
                    timeCells = new Dictionary<Vector2Int,
                        WaveSpawnInstruction>();
                    occupiedByTime.Add(timeKey, timeCells);
                }

                foreach (Vector2Int cell in occupied)
                {
                    if (!WaveLayoutGeometry.IsInside(layout, cell))
                    {
                        message = "Placement leaves grid.";
                        return false;
                    }

                    if (timeCells.ContainsKey(cell))
                    {
                        message = "Spawn footprints overlap.";
                        return false;
                    }

                    timeCells.Add(cell, instruction);
                }
            }

            return true;
        }

        public static WaveLayoutPlacement FindPlacement(
            WaveLayoutData layout,
            int waveIndex,
            string id)
        {
            if (layout == null ||
                string.IsNullOrWhiteSpace(id) ||
                waveIndex < 0 ||
                waveIndex >= layout.Waves.Count)
            {
                return null;
            }

            foreach (
                WaveLayoutPlacement placement
                in layout.Waves[waveIndex].Placements)
            {
                if (placement != null &&
                    string.Equals(
                        placement.Id,
                        id,
                        StringComparison.Ordinal))
                {
                    return placement;
                }
            }

            return null;
        }

        public static void Save(UnityEngine.Object asset)
        {
            if (asset != null)
                EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();
        }
    }
}
