using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MissNibiru.Waves.Spawning
{
    public sealed class GridSpawnPointProvider :
        MonoBehaviour,
        ISpawnPointProvider
    {
        [Serializable]
        private sealed class GridSpawnCell
        {
            [SerializeField]
            private Vector3Int cell;

            [SerializeField]
            private string[] tags;

            public Vector3Int Cell => cell;
            public string[] Tags => tags;
        }

        [SerializeField]
        private Grid grid;

        [SerializeField]
        private GridSpawnCell[] spawnCells;

        public bool TryGetSpawnPoint(
            string[] allowedTags,
            out Pose spawnPose)
        {
            spawnPose = default;

            if (grid == null ||
                spawnCells == null ||
                spawnCells.Length == 0)
            {
                return false;
            }

            GridSpawnCell selectedCell = null;
            int matchingCellCount = 0;

            foreach (GridSpawnCell cell in spawnCells)
            {
                if (cell == null)
                    continue;

                if (!MatchesAllowedTags(
                        cell.Tags,
                        allowedTags))
                {
                    continue;
                }

                matchingCellCount++;

                if (Random.Range(0, matchingCellCount) == 0)
                    selectedCell = cell;
            }

            if (selectedCell == null)
                return false;

            Vector3 position =
                grid.GetCellCenterWorld(selectedCell.Cell);

            spawnPose = new Pose(
                position,
                grid.transform.rotation);

            return true;
        }

        private static bool MatchesAllowedTags(
            string[] pointTags,
            string[] allowedTags)
        {
            if (allowedTags == null ||
                allowedTags.Length == 0)
            {
                return true;
            }

            if (pointTags == null ||
                pointTags.Length == 0)
            {
                return false;
            }

            foreach (string allowedTag in allowedTags)
            {
                foreach (string pointTag in pointTags)
                {
                    if (string.Equals(
                            allowedTag,
                            pointTag,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}