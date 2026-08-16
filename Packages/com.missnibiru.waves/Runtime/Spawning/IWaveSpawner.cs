using UnityEngine;

namespace MissNibiru.Waves.Spawning
{
    public interface IWaveSpawner
    {
        GameObject Spawn(
            GameObject prefab,
            Pose pose,
            Transform parent);

        void Despawn(GameObject instance);
    }
}