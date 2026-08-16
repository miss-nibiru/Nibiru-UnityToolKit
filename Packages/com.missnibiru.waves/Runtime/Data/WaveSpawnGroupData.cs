using UnityEngine;

namespace MissNibiru.Waves.Data
{
    [CreateAssetMenu(
        fileName = "WaveSpawnGroupData",
        menuName = "Nibiru/Waves/Spawn Group Data")]
    public sealed class WaveSpawnGroupData : ScriptableObject
    {
        public enum SpawnPattern
        {
            Single,
            Cluster,
            Line
        }

        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        private SpawnPattern[] allowedPatterns =
        {
            SpawnPattern.Single
        };

        [SerializeField]
        private string[] spawnPointTags;

        [SerializeField]
        private bool limitTotalSpawns;

        [SerializeField, Min(1)]
        private int totalSpawnLimit = 1;

        [SerializeField, Min(1)]
        private int instancesPerSpawn = 1;

        [SerializeField]
        private bool limitActiveInstances;

        [SerializeField, Min(1)]
        private int maximumActiveInstances = 1;

        [SerializeField, Min(0f)]
        private float delayBetweenSpawns;

        [SerializeField, Min(0f)]
        private float patternSpacing = 1f;

        [SerializeField]
        private Vector3 lineDirection = Vector3.right;

        public GameObject Prefab => prefab;
        public SpawnPattern[] AllowedPatterns => allowedPatterns;
        public string[] SpawnPointTags => spawnPointTags;

        public bool LimitTotalSpawns => limitTotalSpawns;
        public int TotalSpawnLimit => totalSpawnLimit;
        public int InstancesPerSpawn => instancesPerSpawn;

        public bool LimitActiveInstances => limitActiveInstances;

        public int MaximumActiveInstances =>
            maximumActiveInstances;

        public float DelayBetweenSpawns => delayBetweenSpawns;
        public float PatternSpacing => patternSpacing;
        public Vector3 LineDirection => lineDirection;
    }
}