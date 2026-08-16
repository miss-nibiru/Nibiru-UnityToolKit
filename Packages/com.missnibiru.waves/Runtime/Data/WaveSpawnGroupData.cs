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
        private SpawnPattern[] allowedPatterns;

        [SerializeField]
        private string[] spawnPointTags;

        [SerializeField]
        private bool limitTotalSpawns;

        [SerializeField, Min(0)]
        private int totalSpawnLimit;

        [SerializeField, Min(0)]
        private int instancesPerSpawn;

        [SerializeField]
        private bool limitActiveInstances;

        [SerializeField, Min(0)]
        private int maximumActiveInstances;

        [SerializeField, Min(0f)]
        private float delayBetweenSpawns;

        [SerializeField, Min(0f)]
        private float patternSpacing;

        [SerializeField]
        private Vector3 lineDirection;

        [SerializeField]
        private GameObject formationPrefab;

        public GameObject Prefab => prefab;
        public SpawnPattern[] AllowedPatterns => allowedPatterns;
        public string[] SpawnPointTags => spawnPointTags;

        public bool LimitTotalSpawns => limitTotalSpawns;
        public int TotalSpawnLimit => totalSpawnLimit;
        public int InstancesPerSpawn => instancesPerSpawn;

        public bool LimitActiveInstances => limitActiveInstances;
        public int MaximumActiveInstances => maximumActiveInstances;

        public float DelayBetweenSpawns => delayBetweenSpawns;
        public float PatternSpacing => patternSpacing;
        public Vector3 LineDirection => lineDirection;
        public GameObject FormationPrefab => formationPrefab;
    }
}