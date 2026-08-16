using MissNibiru.Core.Health;
using MissNibiru.Enemies.Actor;
using MissNibiru.Enemies.Attacks;
using NUnit.Framework;
using UnityEngine;

namespace MissNibiru.Enemies.Tests
{
    public sealed class DamageableSpy : MonoBehaviour, IDamageable
    {
        public float TotalDamage { get; private set; }

        public void TakeDamage(float amount)
        {
            TotalDamage += amount;
        }
    }

    public sealed class ContactDamageAttackTests
    {
        private GameObject _enemyObject;
        private GameObject _targetObject;

        private ContactDamageAttack _attack;
        private DamageableSpy _damageable;

        [SetUp]
        public void SetUp()
        {
            _enemyObject =
                new GameObject("Contact Enemy");

            _targetObject =
                new GameObject("Damage Target");

            HealthComponent health =
                _enemyObject.AddComponent<
                    HealthComponent>();

            EnemyActor actor =
                _enemyObject.AddComponent<
                    EnemyActor>();

            _attack =
                _enemyObject.AddComponent<
                    ContactDamageAttack>();

            _damageable =
                _targetObject.AddComponent<
                    DamageableSpy>();

            EnemyContext context =
                new EnemyContext(
                    actor,
                    _enemyObject.transform,
                    health,
                    null);

            _attack.Initialize(context);
        }

        [TearDown]
        public void TearDown()
        {
            if (_targetObject != null)
                Object.DestroyImmediate(_targetObject);

            if (_enemyObject != null)
                Object.DestroyImmediate(_enemyObject);
        }

        [Test]
        public void ContactDamage_AppliesConfiguredDamage()
        {
            _attack.Configure(12f, 0.5f);
            _attack.Activate();

            bool applied =
                _attack.TryApplyDamage(_damageable);

            Assert.IsTrue(applied);

            Assert.That(
                _damageable.TotalDamage,
                Is.EqualTo(12f));
        }

        [Test]
        public void ContactDamage_CooldownBlocksRepeatedDamage()
        {
            _attack.Configure(10f, 0.5f);
            _attack.Activate();

            Assert.IsTrue(
                _attack.TryApplyDamage(_damageable));

            Assert.IsFalse(
                _attack.TryApplyDamage(_damageable));

            Assert.That(
                _damageable.TotalDamage,
                Is.EqualTo(10f));

            _attack.Tick(0.5f);

            Assert.IsTrue(
                _attack.TryApplyDamage(_damageable));

            Assert.That(
                _damageable.TotalDamage,
                Is.EqualTo(20f));
        }

        [Test]
        public void ContactDamage_DeactivatedDoesNotDamage()
        {
            _attack.Configure(10f, 0f);
            _attack.Activate();
            _attack.Deactivate();

            bool applied =
                _attack.TryApplyDamage(_damageable);

            Assert.IsFalse(applied);

            Assert.That(
                _damageable.TotalDamage,
                Is.EqualTo(0f));
        }

        [Test]
        public void ContactDamage_IgnoresOwner()
        {
            _attack.Configure(10f, 0f);
            _attack.Activate();

            bool applied =
                _attack.TryApplyDamage(
                    _enemyObject.transform);

            Assert.IsFalse(applied);
        }
    }
}