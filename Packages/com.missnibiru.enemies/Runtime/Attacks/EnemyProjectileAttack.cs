using System;
using System.Collections.Generic;
using MissNibiru.Combat.Attacks;
using MissNibiru.Combat.Projectiles;
using MissNibiru.Enemies.Actor;
using UnityEngine;

namespace MissNibiru.Enemies.Attacks
{
    public sealed class EnemyProjectileAttack :
        MonoBehaviour,
        IEnemyAttackBehaviour
    {
        [Header("Attack")]

        [SerializeField]
        private ProjectileAttackConfiguration
            configuration;

        [SerializeField]
        private MonoBehaviour emitterSource;

        [Header("Firing")]

        [SerializeField]
        private Transform[] spawnPoints;

        [SerializeField, Min(0f)]
        private float initialDelay;

        private EnemyContext _context;
        private IProjectileEmitter _emitter;

        private Transform[] _resolvedSpawnPoints =
            Array.Empty<Transform>();

        private ProjectileAttackExecutor[] _executors =
            Array.Empty<ProjectileAttackExecutor>();

        private float _initialDelayRemaining;

        public bool IsActive { get; private set; }

        public ProjectileAttackConfiguration
            Configuration => configuration;

        public void Configure(
            ProjectileAttackConfiguration
                attackConfiguration,
            IProjectileEmitter projectileEmitter,
            Transform[] projectileSpawnPoints,
            float firstAttackDelay = 0f)
        {
            if (IsActive)
                Deactivate();

            configuration =
                attackConfiguration;

            _emitter =
                projectileEmitter;

            spawnPoints =
                projectileSpawnPoints != null
                    ? (Transform[])
                        projectileSpawnPoints.Clone()
                    : null;

            initialDelay =
                Mathf.Max(0f, firstAttackDelay);
        }

        public void Initialize(EnemyContext context)
        {
            _context = context;

            if (_emitter == null)
            {
                _emitter =
                    emitterSource as
                        IProjectileEmitter;
            }

            BuildExecutors();
        }

        public void Activate()
        {
            if (_context == null ||
                configuration == null)
            {
                IsActive = false;
                return;
            }

            if (_emitter == null)
            {
                _emitter =
                    emitterSource as
                        IProjectileEmitter;
            }

            if (_emitter == null)
            {
                IsActive = false;
                return;
            }

            BuildExecutors();

            _initialDelayRemaining =
                Mathf.Max(0f, initialDelay);

            IsActive =
                _executors.Length > 0;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive ||
                _context == null)
            {
                return;
            }

            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            if (_initialDelayRemaining > 0f)
            {
                _initialDelayRemaining =
                    Mathf.Max(
                        0f,
                        _initialDelayRemaining -
                        safeDeltaTime);

                if (_initialDelayRemaining > 0f)
                    return;
            }

            if (!_context.TryGetTarget(
                    out Transform target))
            {
                TickWithoutTarget(safeDeltaTime);
                return;
            }

            for (int i = 0;
                 i < _executors.Length;
                 i++)
            {
                Transform spawnPoint =
                    _resolvedSpawnPoints[i];

                Vector3 origin =
                    spawnPoint.position;

                Vector3 direction =
                    target.position - origin;

                if (direction.sqrMagnitude <= 0f)
                {
                    direction =
                        spawnPoint.up.sqrMagnitude > 0f
                            ? spawnPoint.up
                            : Vector3.forward;
                }

                ProjectileAttackExecutor executor =
                    _executors[i];

                executor.Tick(
                    safeDeltaTime,
                    origin,
                    direction);

                if (executor.IsReady)
                {
                    executor.TryStartSequence(
                        origin,
                        direction);
                }
            }
        }

        public void Deactivate()
        {
            IsActive = false;

            foreach (
                ProjectileAttackExecutor executor
                in _executors)
            {
                executor?.Cancel();
            }
        }

        private void TickWithoutTarget(
            float deltaTime)
        {
            foreach (
                ProjectileAttackExecutor executor
                in _executors)
            {
                if (executor == null)
                    continue;

                executor.Cancel();

                executor.Tick(
                    deltaTime,
                    Vector3.zero,
                    Vector3.zero);
            }
        }

        private void BuildExecutors()
        {
            List<Transform> validSpawnPoints =
                new List<Transform>();

            if (spawnPoints != null)
            {
                foreach (
                    Transform spawnPoint
                    in spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        validSpawnPoints.Add(
                            spawnPoint);
                    }
                }
            }

            if (validSpawnPoints.Count == 0 &&
                _context != null &&
                _context.Transform != null)
            {
                validSpawnPoints.Add(
                    _context.Transform);
            }

            _resolvedSpawnPoints =
                validSpawnPoints.ToArray();

            _executors =
                new ProjectileAttackExecutor[
                    _resolvedSpawnPoints.Length];

            for (int i = 0;
                 i < _executors.Length;
                 i++)
            {
                _executors[i] =
                    new ProjectileAttackExecutor(
                        configuration,
                        _emitter,
                        _context.Transform);
            }
        }
    }
}