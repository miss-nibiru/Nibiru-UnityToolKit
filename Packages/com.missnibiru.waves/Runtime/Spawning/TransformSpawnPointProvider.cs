using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MissNibiru.Waves.Spawning
{
    public sealed class TransformSpawnPointProvider :
        MonoBehaviour,
        ISpawnPointProvider
    {
        [Serializable]
        private sealed class SpawnPointEntry
        {
            [SerializeField]
            private Transform point;

            [SerializeField]
            private string[] tags;

            public Transform Point => point;
            public string[] Tags => tags;
        }

        [SerializeField]
        private SpawnPointEntry[] spawnPoints;

        public bool TryGetSpawnPoint(
            string[] allowedTags,
            out Pose spawnPose)
        {
            spawnPose = default;

            if (spawnPoints == null || spawnPoints.Length == 0)
                return false;

            Transform selectedPoint = null;
            int matchingPointCount = 0;

            foreach (SpawnPointEntry entry in spawnPoints)
            {
                if (entry?.Point == null)
                    continue;

                if (!MatchesAllowedTags(entry.Tags, allowedTags))
                    continue;

                matchingPointCount++;

                if (Random.Range(0, matchingPointCount) == 0)
                {
                    selectedPoint = entry.Point;
                }
            }

            if (selectedPoint == null)
                return false;

            spawnPose = new Pose(
                selectedPoint.position,
                selectedPoint.rotation);

            return true;
        }

        private static bool MatchesAllowedTags(
            string[] pointTags,
            string[] allowedTags)
        {
            if (allowedTags == null || allowedTags.Length == 0)
                return true;

            if (pointTags == null || pointTags.Length == 0)
                return false;

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