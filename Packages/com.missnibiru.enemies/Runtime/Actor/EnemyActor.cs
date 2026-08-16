using System;
using System.Collections.Generic;
using MissNibiru.Core.Health;
using MissNibiru.Enemies.Attacks;
using MissNibiru.Enemies.Movement;
using MissNibiru.Enemies.Targeting;
using UnityEngine;

namespace MissNibiru.Enemies.Actor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyActor : MonoBehaviour
    {
        [Header("Lifecycle")]

        [SerializeField]
        private bool activateOnStart = true;

        [Header("Composable Behaviours")]

        [SerializeField]
        private MonoBehaviour targetProviderSource;

        [SerializeField]
        private MonoBehaviour movementBehaviourSource;

        [SerializeField]
        private MonoBehaviour[] attackBehaviourSources;

        private HealthComponent _health;
        private HealthComponent _subscribedHealth;

        private IEnemyTargetProvider _targetProvider;
        private IEnemyMovementBehaviour _movementBehaviour;

        private IEnemyAttackBehaviour[] _attackBehaviours =
            Array.Empty<IEnemyAttackBehaviour>();

        private EnemyContext _context;
        private bool _deathHandled;

        public event Action<EnemyActor> Initialized;
        public event Action<EnemyActor> Activated;
        public event Action<EnemyActor> Deactivated;
        public event Action<EnemyActor> Died;

        public bool IsInitialized { get; private set; }
        public bool IsActive { get; private set; }

        public bool IsDead =>
            _deathHandled ||
            (_health != null && _health.IsDead);

        public HealthComponent Health => _health;
        public EnemyContext Context => _context;

        private void Awake()
        {
            SetHealth(GetComponent<HealthComponent>());
            ResolveBehaviourSources();
        }

        private void Start()
        {
            if (activateOnStart)
                Activate();
        }

        private void Update()
        {
            if (IsActive)
                Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            Deactivate();
        }

        private void OnDestroy()
        {
            SetHealth(null);
        }

        public void Configure(
            HealthComponent health,
            IEnemyTargetProvider targetProvider,
            IEnemyMovementBehaviour movementBehaviour,
            IEnemyAttackBehaviour[] attackBehaviours)
        {
            if (IsActive)
                Deactivate();

            SetHealth(
                health != null
                    ? health
                    : GetComponent<HealthComponent>());

            _targetProvider = targetProvider;
            _movementBehaviour = movementBehaviour;

            _attackBehaviours =
                RemoveNullAttackBehaviours(
                    attackBehaviours);

            _context = null;
            IsInitialized = false;
            _deathHandled =
                _health != null && _health.IsDead;
        }

        public bool Initialize()
        {
            if (IsInitialized)
                return true;

            ResolveBehaviourSources();

            if (_health == null)
            {
                Debug.LogError(
                    "EnemyActor requires a HealthComponent.",
                    this);

                return false;
            }

            _context = new EnemyContext(
                this,
                transform,
                _health,
                _targetProvider);

            _movementBehaviour?.Initialize(_context);

            foreach (
                IEnemyAttackBehaviour attack
                in _attackBehaviours)
            {
                attack.Initialize(_context);
            }

            IsInitialized = true;
            Initialized?.Invoke(this);

            return true;
        }

        public bool Activate()
        {
            if (IsActive)
                return true;

            if (!Initialize() || IsDead)
                return false;

            IsActive = true;

            _movementBehaviour?.Activate();

            foreach (
                IEnemyAttackBehaviour attack
                in _attackBehaviours)
            {
                attack.Activate();
            }

            Activated?.Invoke(this);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive || IsDead)
                return;

            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            _movementBehaviour?.Tick(safeDeltaTime);

            foreach (
                IEnemyAttackBehaviour attack
                in _attackBehaviours)
            {
                attack.Tick(safeDeltaTime);
            }
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;

            foreach (
                IEnemyAttackBehaviour attack
                in _attackBehaviours)
            {
                attack.Deactivate();
            }

            _movementBehaviour?.Deactivate();
            Deactivated?.Invoke(this);
        }

        public bool ResetActor()
        {
            if (_health == null)
                return false;

            Deactivate();

            _health.ResetHealth();
            _deathHandled = false;

            return true;
        }

        private void ResolveBehaviourSources()
        {
            if (_health == null)
                SetHealth(GetComponent<HealthComponent>());

            if (_targetProvider == null)
            {
                _targetProvider =
                    targetProviderSource
                        as IEnemyTargetProvider;
            }

            if (_movementBehaviour == null)
            {
                _movementBehaviour =
                    movementBehaviourSource
                        as IEnemyMovementBehaviour;
            }

            if ((_attackBehaviours == null ||
                 _attackBehaviours.Length == 0) &&
                attackBehaviourSources != null)
            {
                List<IEnemyAttackBehaviour>
                    resolvedAttacks =
                        new List<IEnemyAttackBehaviour>();

                foreach (
                    MonoBehaviour source
                    in attackBehaviourSources)
                {
                    if (source is
                        IEnemyAttackBehaviour attack)
                    {
                        resolvedAttacks.Add(attack);
                    }
                }

                _attackBehaviours =
                    resolvedAttacks.ToArray();
            }
        }

        private static IEnemyAttackBehaviour[]
            RemoveNullAttackBehaviours(
                IEnemyAttackBehaviour[] attacks)
        {
            if (attacks == null || attacks.Length == 0)
                return Array.Empty<IEnemyAttackBehaviour>();

            List<IEnemyAttackBehaviour> validAttacks =
                new List<IEnemyAttackBehaviour>();

            foreach (IEnemyAttackBehaviour attack in attacks)
            {
                if (attack != null)
                    validAttacks.Add(attack);
            }

            return validAttacks.ToArray();
        }

        private void SetHealth(
            HealthComponent newHealth)
        {
            if (_subscribedHealth != null)
            {
                _subscribedHealth.Died -=
                    HandleHealthDied;
            }

            _health = newHealth;
            _subscribedHealth = newHealth;

            if (_subscribedHealth != null)
            {
                _subscribedHealth.Died +=
                    HandleHealthDied;
            }
        }

        private void HandleHealthDied()
        {
            if (_deathHandled)
                return;

            _deathHandled = true;

            Deactivate();
            Died?.Invoke(this);
        }
    }
}