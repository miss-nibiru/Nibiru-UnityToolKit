using System;
using MissNibiru.Combat.Projectiles;
using UnityEngine;

namespace MissNibiru.Combat.Attacks
{
    public sealed class ProjectileAttackExecutor
    {
        private const int MaximumShotsPerTick = 64;

        private ProjectileAttackConfiguration
            _configuration;

        private IProjectileEmitter _emitter;
        private Transform _owner;

        private int _shotsRemaining;
        private int _volleyNumber;

        private float _timeUntilNextShot;
        private float _cooldownRemaining;

        public event Action SequenceStarted;
        public event Action<int> VolleyFired;
        public event Action SequenceCompleted;

        public bool IsSequenceActive { get; private set; }

        public bool IsReady =>
            _configuration != null &&
            _emitter != null &&
            !IsSequenceActive &&
            _cooldownRemaining <= 0f;

        public float CooldownRemaining =>
            _cooldownRemaining;

        public ProjectileAttackExecutor(
            ProjectileAttackConfiguration configuration,
            IProjectileEmitter emitter,
            Transform owner)
        {
            Configure(
                configuration,
                emitter,
                owner);
        }

        public void Configure(
            ProjectileAttackConfiguration configuration,
            IProjectileEmitter emitter,
            Transform owner)
        {
            _configuration = configuration;
            _emitter = emitter;
            _owner = owner;

            Cancel();
            _cooldownRemaining = 0f;
        }

        public bool TryStartSequence(
            Vector3 origin,
            Vector3 direction)
        {
            if (!IsReady ||
                direction.sqrMagnitude <= 0f)
            {
                return false;
            }

            IsSequenceActive = true;

            _shotsRemaining =
                _configuration.ShotsPerSequence;

            _volleyNumber = 0;
            _timeUntilNextShot = 0f;

            SequenceStarted?.Invoke();

            bool emitted =
                EmitVolley(origin, direction);

            AdvanceSequence();

            return emitted;
        }

        public void Tick(
            float deltaTime,
            Vector3 origin,
            Vector3 direction)
        {
            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            if (!IsSequenceActive)
            {
                _cooldownRemaining = Mathf.Max(
                    0f,
                    _cooldownRemaining -
                    safeDeltaTime);

                return;
            }

            _timeUntilNextShot -=
                safeDeltaTime;

            int safetyCounter = 0;

            while (IsSequenceActive &&
                   _timeUntilNextShot <= 0f &&
                   safetyCounter <
                   MaximumShotsPerTick)
            {
                float carriedTime =
                    -_timeUntilNextShot;

                EmitVolley(origin, direction);
                AdvanceSequence();

                if (IsSequenceActive)
                {
                    _timeUntilNextShot -=
                        carriedTime;
                }

                safetyCounter++;
            }
        }

        public void Cancel()
        {
            IsSequenceActive = false;

            _shotsRemaining = 0;
            _volleyNumber = 0;
            _timeUntilNextShot = 0f;
        }

        private bool EmitVolley(
            Vector3 origin,
            Vector3 direction)
        {
            if (_configuration == null ||
                _emitter == null ||
                direction.sqrMagnitude <= 0f)
            {
                return false;
            }

            Vector3 centreDirection =
                direction.normalized;

            bool emittedAny = false;

            for (int i = 0;
                 i <
                 _configuration.ProjectilesPerVolley;
                 i++)
            {
                float angle =
                    _configuration.GetSpreadAngle(i);

                Vector3 projectileDirection =
                    Quaternion.AngleAxis(
                        angle,
                        _configuration.SpreadAxis) *
                    centreDirection;

                ProjectileSpawnRequest request =
                    new ProjectileSpawnRequest(
                        origin,
                        projectileDirection,
                        _configuration.ProjectileSpeed,
                        _configuration.ProjectileDamage,
                        _configuration.ProjectileLifetime,
                        _owner);

                if (_emitter.TryEmit(request))
                    emittedAny = true;
            }

            _volleyNumber++;
            VolleyFired?.Invoke(_volleyNumber);

            return emittedAny;
        }

        private void AdvanceSequence()
        {
            _shotsRemaining--;

            if (_shotsRemaining <= 0)
            {
                IsSequenceActive = false;

                _cooldownRemaining =
                    _configuration
                        .CooldownAfterSequence;

                SequenceCompleted?.Invoke();
                return;
            }

            _timeUntilNextShot =
                _configuration
                    .DelayBetweenSequenceShots;
        }
    }
}