using UnityEngine;

namespace MissNibiru.Waves.Data
{
    [CreateAssetMenu(
        fileName = "WaveData",
        menuName = "Nibiru/Waves/Wave Data")]
    public sealed class WaveData : ScriptableObject
    {
        [SerializeField]
        private string waveName;

        [SerializeField]
        private bool usesDuration;

        [SerializeField, Min(0f)]
        private float duration;

        [SerializeField, Min(0f)]
        private float initialDelay;

        [SerializeField, Min(0f)]
        private float spawnInterval;

        [SerializeField]
        private bool waitForActiveObjectsToClear;

        [SerializeField]
        private bool despawnActiveObjectsOnCompletion;

        [SerializeField]
        private WaveSpawnGroupData[] spawnGroups;

        public string WaveName => waveName;
        public bool UsesDuration => usesDuration;
        public float Duration => duration;
        public float InitialDelay => initialDelay;
        public float SpawnInterval => spawnInterval;

        public bool WaitForActiveObjectsToClear =>
            waitForActiveObjectsToClear;

        public bool DespawnActiveObjectsOnCompletion =>
            despawnActiveObjectsOnCompletion;

        public WaveSpawnGroupData[] SpawnGroups => spawnGroups;
    }
}