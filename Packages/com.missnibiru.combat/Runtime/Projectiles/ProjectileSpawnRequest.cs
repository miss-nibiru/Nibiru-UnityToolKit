using UnityEngine;

namespace MissNibiru.Combat.Projectiles

{
    public readonly struct ProjectileSpawnRequest
    {
        public ProjectileSpawnRequest(
            Vector3 position,
            Vector3 direction,
            float speed,
            float damage,
            float lifetime,
            Transform owner)
        {
            Position = position;

            Direction =
                direction.sqrMagnitude > 0f
                    ? direction.normalized
                    : Vector3.zero;

            Speed = Mathf.Max(0f, speed);
            Damage = Mathf.Max(0f, damage);
            Lifetime = Mathf.Max(0f, lifetime);
            Owner = owner;
        }

        public Vector3 Position { get; }
        public Vector3 Direction { get; }

        public float Speed { get; }
        public float Damage { get; }
        public float Lifetime { get; }

        public Transform Owner { get; }
    }
}