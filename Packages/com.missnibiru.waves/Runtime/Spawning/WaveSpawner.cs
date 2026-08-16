using UnityEngine;

namespace MissNibiru.Waves.Spawning
{
    public sealed class WaveSpawner :
        MonoBehaviour,
        IWaveSpawner
    {
        public GameObject Spawn(
            GameObject prefab,
            Pose pose,
            Transform parent)
        {
            if (prefab == null)
                return null;

            GameObject instance = Instantiate(
                prefab,
                pose.position,
                pose.rotation,
                parent);

            instance.SetActive(true);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null)
                return;

            if (Application.isPlaying)
                Destroy(instance);
            else
                DestroyImmediate(instance);
        }
    }
}