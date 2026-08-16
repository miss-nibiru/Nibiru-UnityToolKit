using System;
using MissNibiru.Core.Health;
using MissNibiru.Enemies.Actor;
using UnityEngine;

namespace MissNibiru.Enemies.Attacks
{
    public sealed class ContactDamageAttack :
        MonoBehaviour,
        IEnemyAttackBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float damageCooldown = 0.5f;

        private EnemyContext _context;
        private float _remainingCooldown;

        public event Action<IDamageable, float>
            DamageApplied;

        public bool IsActive { get; private set; }

        public void Configure(
            float damageAmount,
            float cooldown)
        {
            damage = Mathf.Max(0f, damageAmount);

            damageCooldown =
                Mathf.Max(0f, cooldown);
        }

        public void Initialize(EnemyContext context)
        {
            _context = context;
            _remainingCooldown = 0f;
        }

        public void Activate()
        {
            IsActive = _context != null;
            _remainingCooldown = 0f;
        }

        public void Tick(float deltaTime)
        {
            _remainingCooldown = Mathf.Max(
                0f,
                _remainingCooldown -
                Mathf.Max(0f, deltaTime));
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public bool TryApplyDamage(Component target)
        {
            if (!IsActive ||
                damage <= 0f ||
                _remainingCooldown > 0f ||
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

            damageable.TakeDamage(damage);

            _remainingCooldown =
                damageCooldown;

            DamageApplied?.Invoke(
                damageable,
                damage);

            return true;
        }

        private bool IsOwner(Transform candidate)
        {
            if (_context == null ||
                _context.Transform == null ||
                candidate == null)
            {
                return false;
            }

            Transform owner = _context.Transform;

            return candidate == owner ||
                   candidate.IsChildOf(owner);
        }

        private void OnTriggerEnter2D(
            Collider2D other)
        {
            TryApplyDamage(other);
        }

        private void OnTriggerStay2D(
            Collider2D other)
        {
            TryApplyDamage(other);
        }

        private void OnCollisionEnter2D(
            Collision2D collision)
        {
            TryApplyDamage(collision.collider);
        }

        private void OnCollisionStay2D(
            Collision2D collision)
        {
            TryApplyDamage(collision.collider);
        }

        private void OnTriggerEnter(
            Collider other)
        {
            TryApplyDamage(other);
        }

        private void OnTriggerStay(
            Collider other)
        {
            TryApplyDamage(other);
        }

        private void OnCollisionEnter(
            Collision collision)
        {
            TryApplyDamage(collision.collider);
        }

        private void OnCollisionStay(
            Collision collision)
        {
            TryApplyDamage(collision.collider);
        }
    }
}