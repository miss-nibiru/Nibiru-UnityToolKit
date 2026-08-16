using UnityEngine;

namespace MissNibiru.Waves.Spawning
{
    public interface ISpawnPointProvider
    {
        bool TryGetSpawnPoint(
            string[] allowedTags,
            out Pose spawnPose);
    }
}