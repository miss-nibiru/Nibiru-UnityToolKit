using MissNibiru.Core.Health;
using MissNibiru.Enemies.Targeting;
using UnityEngine;

namespace MissNibiru.Enemies.Actor
{
    public sealed class EnemyContext
    {
        public EnemyContext(
            EnemyActor actor,
            Transform transform,
            HealthComponent health,
            IEnemyTargetProvider targetProvider)
        {
            Actor = actor;
            Transform = transform;
            Health = health;
            TargetProvider = targetProvider;
        }

        public EnemyActor Actor { get; }
        public Transform Transform { get; }
        public HealthComponent Health { get; }
        public IEnemyTargetProvider TargetProvider { get; }

        public bool TryGetTarget(out Transform target)
        {
            target = null;

            return TargetProvider != null &&
                   TargetProvider.TryGetTarget(out target) &&
                   target != null;
        }
    }
}