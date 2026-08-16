using System;
using UnityEngine;

namespace MissNibiru.Enemies.Projectiles
{
    public sealed class PrefabProjectileEmitter :
        MonoBehaviour,
        IProjectileEmitter
    {
        [SerializeField]
        private ProjectileActor projectilePrefab;

        [SerializeField]
        private Transform projectileContainer;

        public event Action<ProjectileActor> Emitted;

        public void Configure(
            ProjectileActor prefab,
            Transform container = null)
        {
            projectilePrefab = prefab;
            projectileContainer = container;
        }

        public bool TryEmit(
            ProjectileSpawnRequest request)
        {
            if (projectilePrefab == null ||
                request.Direction.sqrMagnitude <= 0f ||
                request.Lifetime <= 0f)
            {
                return false;
            }

            ProjectileActor projectile =
                Instantiate(
                    projectilePrefab,
                    request.Position,
                    projectilePrefab.transform.rotation,
                    projectileContainer);

            projectile.Completed +=
                HandleProjectileCompleted;

            if (!projectile.Launch(request))
            {
                projectile.Completed -=
                    HandleProjectileCompleted;

                DisposeProjectile(projectile);
                return false;
            }

            Emitted?.Invoke(projectile);
            return true;
        }

        private void HandleProjectileCompleted(
            ProjectileActor projectile)
        {
            projectile.Completed -=
                HandleProjectileCompleted;

            DisposeProjectile(projectile);
        }

        private static void DisposeProjectile(
            ProjectileActor projectile)
        {
            if (projectile == null)
                return;

            if (Application.isPlaying)
            {
                Destroy(projectile.gameObject);
            }
            else
            {
                DestroyImmediate(
                    projectile.gameObject);
            }
        }
    }
}