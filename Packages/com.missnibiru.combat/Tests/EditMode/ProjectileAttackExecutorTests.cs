using System.Collections.Generic;
using MissNibiru.Combat.Attacks;
using MissNibiru.Combat.Projectiles;
using NUnit.Framework;
using UnityEngine;

namespace MissNibiru.Combat.Tests
{
    public sealed class ProjectileAttackExecutorTests
    {
        private sealed class RecordingEmitter :
            IProjectileEmitter
        {
            public readonly
                List<ProjectileSpawnRequest> Requests =
                    new List<ProjectileSpawnRequest>();

            public bool TryEmit(
                ProjectileSpawnRequest request)
            {
                Requests.Add(request);
                return true;
            }
        }

        private GameObject _ownerObject;

        private ProjectileAttackConfiguration
            _configuration;

        private RecordingEmitter _emitter;

        [SetUp]
        public void SetUp()
        {
            _ownerObject =
                new GameObject("Attack Owner");

            _configuration =
                ScriptableObject.CreateInstance<
                    ProjectileAttackConfiguration>();

            _emitter = new RecordingEmitter();
        }

        [TearDown]
        public void TearDown()
        {
            if (_configuration != null)
            {
                Object.DestroyImmediate(
                    _configuration);
            }

            if (_ownerObject != null)
            {
                Object.DestroyImmediate(
                    _ownerObject);
            }
        }

        [Test]
        public void SingleVolley_EmitsOneProjectile()
        {
            Configure(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 0f);

            ProjectileAttackExecutor executor =
                CreateExecutor();

            bool fired =
                executor.TryStartSequence(
                    Vector3.zero,
                    Vector3.forward);

            Assert.IsTrue(fired);
            Assert.AreEqual(
                1,
                _emitter.Requests.Count);

            Assert.That(
                _emitter.Requests[0]
                    .Direction,
                Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void EvenSpread_CreatesSymmetricDirections()
        {
            Configure(
                projectilesPerVolley: 3,
                spreadAngle: 60f,
                spreadMode:
                    ProjectileSpreadMode.Even,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 0f);

            ProjectileAttackExecutor executor =
                CreateExecutor();

            executor.TryStartSequence(
                Vector3.zero,
                Vector3.right);

            Assert.AreEqual(
                3,
                _emitter.Requests.Count);

            float firstAngle =
                Vector3.SignedAngle(
                    Vector3.right,
                    _emitter.Requests[0]
                        .Direction,
                    Vector3.forward);

            float centreAngle =
                Vector3.SignedAngle(
                    Vector3.right,
                    _emitter.Requests[1]
                        .Direction,
                    Vector3.forward);

            float finalAngle =
                Vector3.SignedAngle(
                    Vector3.right,
                    _emitter.Requests[2]
                        .Direction,
                    Vector3.forward);

            Assert.That(
                firstAngle,
                Is.EqualTo(-30f).Within(0.01f));

            Assert.That(
                centreAngle,
                Is.EqualTo(0f).Within(0.01f));

            Assert.That(
                finalAngle,
                Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void Sequence_WaitsBetweenShots()
        {
            Configure(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 3,
                sequenceDelay: 0.5f,
                cooldown: 0f);

            ProjectileAttackExecutor executor =
                CreateExecutor();

            executor.TryStartSequence(
                Vector3.zero,
                Vector3.forward);

            Assert.AreEqual(
                1,
                _emitter.Requests.Count);

            executor.Tick(
                0.49f,
                Vector3.zero,
                Vector3.forward);

            Assert.AreEqual(
                1,
                _emitter.Requests.Count);

            executor.Tick(
                0.01f,
                Vector3.zero,
                Vector3.forward);

            Assert.AreEqual(
                2,
                _emitter.Requests.Count);

            executor.Tick(
                0.5f,
                Vector3.zero,
                Vector3.forward);

            Assert.AreEqual(
                3,
                _emitter.Requests.Count);

            Assert.IsFalse(
                executor.IsSequenceActive);
        }

        [Test]
        public void Cooldown_BlocksNewSequence()
        {
            Configure(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 1f);

            ProjectileAttackExecutor executor =
                CreateExecutor();

            Assert.IsTrue(
                executor.TryStartSequence(
                    Vector3.zero,
                    Vector3.forward));

            Assert.IsFalse(
                executor.TryStartSequence(
                    Vector3.zero,
                    Vector3.forward));

            executor.Tick(
                1f,
                Vector3.zero,
                Vector3.forward);

            Assert.IsTrue(executor.IsReady);
        }

        [Test]
        public void RandomSpread_RemainsInsideConfiguredAngle()
        {
            Configure(
                projectilesPerVolley: 1,
                spreadAngle: 20f,
                spreadMode:
                    ProjectileSpreadMode.Random,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 0f);

            ProjectileAttackExecutor executor =
                CreateExecutor();

            for (int i = 0; i < 25; i++)
            {
                executor.TryStartSequence(
                    Vector3.zero,
                    Vector3.right);
            }

            Assert.AreEqual(
                25,
                _emitter.Requests.Count);

            foreach (
                ProjectileSpawnRequest request
                in _emitter.Requests)
            {
                float angle =
                    Vector3.SignedAngle(
                        Vector3.right,
                        request.Direction,
                        Vector3.forward);

                Assert.That(
                    angle,
                    Is.InRange(-10f, 10f));
            }
        }

        [Test]
        public void ZeroDirection_DoesNotFire()
        {
            Configure(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 0f);

            ProjectileAttackExecutor executor =
                CreateExecutor();

            bool fired =
                executor.TryStartSequence(
                    Vector3.zero,
                    Vector3.zero);

            Assert.IsFalse(fired);

            Assert.AreEqual(
                0,
                _emitter.Requests.Count);
        }

        private ProjectileAttackExecutor
            CreateExecutor()
        {
            return new ProjectileAttackExecutor(
                _configuration,
                _emitter,
                _ownerObject.transform);
        }

        private void Configure(
            int projectilesPerVolley,
            float spreadAngle,
            ProjectileSpreadMode spreadMode,
            int shotsPerSequence,
            float sequenceDelay,
            float cooldown)
        {
            _configuration.Configure(
                speed: 10f,
                damage: 5f,
                lifetime: 3f,
                volleyProjectileCount:
                    projectilesPerVolley,
                spreadAngle: spreadAngle,
                projectileSpreadMode:
                    spreadMode,
                projectileSpreadAxis:
                    Vector3.forward,
                sequenceShotCount:
                    shotsPerSequence,
                sequenceShotDelay:
                    sequenceDelay,
                sequenceCooldown:
                    cooldown);
        }
    }
}