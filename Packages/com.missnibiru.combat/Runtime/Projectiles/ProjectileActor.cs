using System;
using MissNibiru.Core.Health;
using MissNibiru.Combat.Damage;
using UnityEngine;

namespace MissNibiru.Combat.Projectiles

{
    public sealed class ProjectileActor :
        MonoBehaviour
    {
        private Vector3 _direction;

        private float _speed;
        private float _damage;
        private float _remainingLifetime;

        private Transform _owner;

        public event Action<ProjectileActor> Completed;

        public bool IsFlying { get; private set; }

        public Vector3 Direction => _direction;
        public float Damage => _damage;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool Launch(
            ProjectileSpawnRequest request)
        {
            transform.position = request.Position;

            _direction = request.Direction;
            _speed = request.Speed;
            _damage = request.Damage;

            _remainingLifetime =
                request.Lifetime;

            _owner = request.Owner;

            IsFlying =
                _direction.sqrMagnitude > 0f &&
                _remainingLifetime > 0f;

            return IsFlying;
        }

        public void Tick(float deltaTime)
        {
            if (!IsFlying)
                return;

            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            float travelTime = Mathf.Min(
                safeDeltaTime,
                _remainingLifetime);

            transform.position +=
                _direction *
                (_speed * travelTime);

            _remainingLifetime -=
                safeDeltaTime;

            if (_remainingLifetime <= 0f)
                Complete();
        }

        public bool TryApplyHit(Component target)
        {
            if (!IsFlying ||
                target == null ||
                IsOwner(target.transform))
            {
                return false;
            }

            if (!DamageableResolver.TryFind(
                    target,
                    out IDamageable damageable))
            {
                return false;
            }

            if (_damage <= 0f)
                return false;

            damageable.TakeDamage(_damage);

            Complete();
            return true;
        }

        public void Cancel()
        {
            if (IsFlying)
                Complete();
        }

        private bool IsOwner(Transform candidate)
        {
            if (_owner == null ||
                candidate == null)
            {
                return false;
            }

            return candidate == _owner ||
                   candidate.IsChildOf(_owner);
        }

        private void Complete()
        {
            if (!IsFlying)
                return;

            IsFlying = false;
            Completed?.Invoke(this);
        }

        private void OnTriggerEnter2D(
            Collider2D other)
        {
            TryApplyHit(other);
        }

        private void OnCollisionEnter2D(
            Collision2D collision)
        {
            TryApplyHit(collision.collider);
        }

        private void OnTriggerEnter(
            Collider other)
        {
            TryApplyHit(other);
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            TryApplyHit(collision.collider);
        }
    }
}