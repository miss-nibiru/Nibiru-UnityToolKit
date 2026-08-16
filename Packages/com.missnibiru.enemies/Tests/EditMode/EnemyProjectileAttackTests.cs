using System.Collections.Generic;
using MissNibiru.Combat.Attacks;
using MissNibiru.Combat.Projectiles;
using MissNibiru.Core.Health;
using MissNibiru.Enemies.Actor;
using MissNibiru.Enemies.Attacks;
using MissNibiru.Enemies.Targeting;
using NUnit.Framework;
using UnityEngine;

namespace MissNibiru.Enemies.Tests
{
    public sealed class EnemyProjectileAttackTests
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

        private sealed class FixedTargetProvider :
            IEnemyTargetProvider
        {
            public Transform Target;

            public bool TryGetTarget(
                out Transform target)
            {
                target = Target;
                return target != null;
            }
        }

        private GameObject _enemyObject;
        private GameObject _targetObject;

        private EnemyProjectileAttack _attack;

        private ProjectileAttackConfiguration
            _configuration;

        private RecordingEmitter _emitter;
        private FixedTargetProvider _targetProvider;

        private EnemyContext _context;

        [SetUp]
        public void SetUp()
        {
            _enemyObject =
                new GameObject(
                    "Projectile Enemy");

            _targetObject =
                new GameObject(
                    "Projectile Target");

            HealthComponent health =
                _enemyObject.AddComponent<
                    HealthComponent>();

            EnemyActor actor =
                _enemyObject.AddComponent<
                    EnemyActor>();

            _attack =
                _enemyObject.AddComponent<
                    EnemyProjectileAttack>();

            _configuration =
                ScriptableObject.CreateInstance<
                    ProjectileAttackConfiguration>();

            _emitter =
                new RecordingEmitter();

            _targetProvider =
                new FixedTargetProvider
                {
                    Target =
                        _targetObject.transform
                };

            _context =
                new EnemyContext(
                    actor,
                    _enemyObject.transform,
                    health,
                    _targetProvider);
        }

        [TearDown]
        public void TearDown()
        {
            if (_configuration != null)
            {
                Object.DestroyImmediate(
                    _configuration);
            }

            if (_targetObject != null)
            {
                Object.DestroyImmediate(
                    _targetObject);
            }

            if (_enemyObject != null)
            {
                Object.DestroyImmediate(
                    _enemyObject);
            }
        }

        [Test]
        public void EnemyAttack_FiresTowardTarget()
        {
            ConfigureAttack(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 1f);

            _targetObject.transform.position =
                new Vector3(10f, 0f, 0f);

            InitializeAndActivate(
                spawnPoints: null,
                initialDelay: 0f);

            _attack.Tick(0f);

            Assert.AreEqual(
                1,
                _emitter.Requests.Count);

            Assert.That(
                Vector3.Distance(
                    Vector3.right,
                    _emitter.Requests[0]
                        .Direction),
                Is.LessThan(0.001f));
        }

        [Test]
        public void EnemyAttack_RespectsInitialDelay()
        {
            ConfigureAttack(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 1f);

            InitializeAndActivate(
                spawnPoints: null,
                initialDelay: 1f);

            _attack.Tick(0.5f);

            Assert.AreEqual(
                0,
                _emitter.Requests.Count);

            _attack.Tick(0.5f);

            Assert.AreEqual(
                1,
                _emitter.Requests.Count);
        }

        [Test]
        public void EnemyAttack_WithoutTargetDoesNotFire()
        {
            ConfigureAttack(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 1f);

            _targetProvider.Target = null;

            InitializeAndActivate(
                spawnPoints: null,
                initialDelay: 0f);

            _attack.Tick(1f);

            Assert.AreEqual(
                0,
                _emitter.Requests.Count);
        }

        [Test]
        public void EnemyAttack_FiresFromEverySpawnPoint()
        {
            ConfigureAttack(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 1f);

            GameObject firstMuzzle =
                new GameObject("First Muzzle");

            GameObject secondMuzzle =
                new GameObject("Second Muzzle");

            firstMuzzle.transform.SetParent(
                _enemyObject.transform);

            secondMuzzle.transform.SetParent(
                _enemyObject.transform);

            firstMuzzle.transform.position =
                new Vector3(-1f, 0f, 0f);

            secondMuzzle.transform.position =
                new Vector3(1f, 0f, 0f);

            InitializeAndActivate(
                new[]
                {
                    firstMuzzle.transform,
                    secondMuzzle.transform
                },
                0f);

            _attack.Tick(0f);

            Assert.AreEqual(
                2,
                _emitter.Requests.Count);

            Assert.That(
                _emitter.Requests[0].Position,
                Is.EqualTo(
                    firstMuzzle.transform.position));

            Assert.That(
                _emitter.Requests[1].Position,
                Is.EqualTo(
                    secondMuzzle.transform.position));
        }

        [Test]
        public void EnemyAttack_UsesTimedBurstSequence()
        {
            ConfigureAttack(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 3,
                sequenceDelay: 0.5f,
                cooldown: 1f);

            InitializeAndActivate(
                spawnPoints: null,
                initialDelay: 0f);

            _attack.Tick(0f);

            Assert.AreEqual(
                1,
                _emitter.Requests.Count);

            _attack.Tick(0.5f);

            Assert.AreEqual(
                2,
                _emitter.Requests.Count);

            _attack.Tick(0.5f);

            Assert.AreEqual(
                3,
                _emitter.Requests.Count);
        }

        [Test]
        public void EnemyAttack_DeactivatedDoesNotFire()
        {
            ConfigureAttack(
                projectilesPerVolley: 1,
                spreadAngle: 0f,
                spreadMode:
                    ProjectileSpreadMode.None,
                shotsPerSequence: 1,
                sequenceDelay: 0f,
                cooldown: 0f);

            InitializeAndActivate(
                spawnPoints: null,
                initialDelay: 0f);

            _attack.Deactivate();
            _attack.Tick(1f);

            Assert.AreEqual(
                0,
                _emitter.Requests.Count);
        }

        private void InitializeAndActivate(
            Transform[] spawnPoints,
            float initialDelay)
        {
            _attack.Configure(
                _configuration,
                _emitter,
                spawnPoints,
                initialDelay);

            _attack.Initialize(_context);
            _attack.Activate();
        }

        private void ConfigureAttack(
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