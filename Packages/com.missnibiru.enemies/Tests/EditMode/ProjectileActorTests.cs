using MissNibiru.Enemies.Projectiles;
using NUnit.Framework;
using UnityEngine;

namespace MissNibiru.Enemies.Tests
{
    public sealed class ProjectileActorTests
    {
        private GameObject _ownerObject;
        private GameObject _projectileObject;
        private GameObject _targetObject;

        private ProjectileActor _projectile;
        private DamageableSpy _damageable;

        [SetUp]
        public void SetUp()
        {
            _ownerObject =
                new GameObject("Projectile Owner");

            _projectileObject =
                new GameObject("Projectile");

            _targetObject =
                new GameObject("Projectile Target");

            _projectile =
                _projectileObject.AddComponent<
                    ProjectileActor>();

            _damageable =
                _targetObject.AddComponent<
                    DamageableSpy>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_targetObject != null)
                Object.DestroyImmediate(_targetObject);

            if (_projectileObject != null)
                Object.DestroyImmediate(
                    _projectileObject);

            if (_ownerObject != null)
                Object.DestroyImmediate(_ownerObject);
        }

        [Test]
        public void Projectile_MovesInConfiguredDirection()
        {
            ProjectileSpawnRequest request =
                new ProjectileSpawnRequest(
                    Vector3.zero,
                    new Vector3(1f, 2f, 2f),
                    3f,
                    10f,
                    5f,
                    _ownerObject.transform);

            Assert.IsTrue(
                _projectile.Launch(request));

            _projectile.Tick(1f);

            Vector3 expected =
                new Vector3(1f, 2f, 2f);

            Assert.That(
                Vector3.Distance(
                    expected,
                    _projectile.transform.position),
                Is.LessThan(0.001f));
        }

        [Test]
        public void Projectile_ExpiresOnlyOnce()
        {
            int completionCount = 0;

            _projectile.Completed +=
                _ => completionCount++;

            ProjectileSpawnRequest request =
                new ProjectileSpawnRequest(
                    Vector3.zero,
                    Vector3.forward,
                    1f,
                    10f,
                    0.5f,
                    _ownerObject.transform);

            _projectile.Launch(request);

            _projectile.Tick(1f);
            _projectile.Tick(1f);

            Assert.IsFalse(_projectile.IsFlying);
            Assert.AreEqual(1, completionCount);
        }

        [Test]
        public void Projectile_DamagesTargetAndCompletes()
        {
            ProjectileSpawnRequest request =
                new ProjectileSpawnRequest(
                    Vector3.zero,
                    Vector3.forward,
                    1f,
                    15f,
                    5f,
                    _ownerObject.transform);

            _projectile.Launch(request);

            bool applied =
                _projectile.TryApplyHit(_damageable);

            Assert.IsTrue(applied);

            Assert.That(
                _damageable.TotalDamage,
                Is.EqualTo(15f));

            Assert.IsFalse(_projectile.IsFlying);
        }

        [Test]
        public void Projectile_IgnoresOwner()
        {
            ProjectileSpawnRequest request =
                new ProjectileSpawnRequest(
                    Vector3.zero,
                    Vector3.forward,
                    1f,
                    10f,
                    5f,
                    _ownerObject.transform);

            _projectile.Launch(request);

            bool applied =
                _projectile.TryApplyHit(
                    _ownerObject.transform);

            Assert.IsFalse(applied);
            Assert.IsTrue(_projectile.IsFlying);
        }

        [Test]
        public void Projectile_RejectsZeroDirection()
        {
            ProjectileSpawnRequest request =
                new ProjectileSpawnRequest(
                    Vector3.zero,
                    Vector3.zero,
                    1f,
                    10f,
                    5f,
                    _ownerObject.transform);

            bool launched =
                _projectile.Launch(request);

            Assert.IsFalse(launched);
            Assert.IsFalse(_projectile.IsFlying);
        }
    }
}