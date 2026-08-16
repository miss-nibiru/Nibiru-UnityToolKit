using MissNibiru.Core.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Core.Tests.Health
{
    public sealed class HealthComponentTests
    {
        private GameObject _gameObject;
        private HealthComponent _health;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Health Test Object");
            _health = _gameObject.AddComponent<HealthComponent>();

            SerializedObject serializedHealth =
                new SerializedObject(_health);

            SerializedProperty maxHealthProperty =
                serializedHealth.FindProperty("maxHealth");

            maxHealthProperty.floatValue = 100f;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            _health.ResetHealth();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void ResetHealth_StartsAtMaximumHealth()
        {
            Assert.AreEqual(100f, _health.CurrentHealth);
            Assert.AreEqual(100f, _health.MaxHealth);
            Assert.IsFalse(_health.IsDead);
        }

        [Test]
        public void TakeDamage_ReducesHealthAndClampsAtZero()
        {
            _health.TakeDamage(25f);

            Assert.AreEqual(75f, _health.CurrentHealth);

            _health.TakeDamage(200f);

            Assert.AreEqual(0f, _health.CurrentHealth);
            Assert.IsTrue(_health.IsDead);
        }

        [Test]
        public void Heal_IncreasesHealthAndClampsAtMaximum()
        {
            _health.TakeDamage(60f);
            _health.Heal(25f);

            Assert.AreEqual(65f, _health.CurrentHealth);

            _health.Heal(500f);

            Assert.AreEqual(100f, _health.CurrentHealth);
        }

        [Test]
        public void DeathEvent_FiresOnlyOnce()
        {
            int deathEventCount = 0;

            _health.Died += () => deathEventCount++;

            _health.TakeDamage(100f);
            _health.TakeDamage(10f);

            Assert.AreEqual(1, deathEventCount);
        }

        [Test]
        public void DamageCalculator_SupportsFlatAndFractionalDamage()
        {
            float flatDamage = DamageCalculator.Calculate(
                25f,
                DamageCalculationMode.Flat);

            float fractionalDamage = DamageCalculator.Calculate(
                0.25f,
                DamageCalculationMode.FractionOfMaximumHealth,
                _health);

            Assert.AreEqual(25f, flatDamage);
            Assert.AreEqual(25f, fractionalDamage);
        }
    }
}