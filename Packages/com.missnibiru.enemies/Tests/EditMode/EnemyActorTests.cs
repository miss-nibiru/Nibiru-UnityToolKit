using MissNibiru.Core.Health;
using MissNibiru.Enemies.Actor;
using MissNibiru.Enemies.Attacks;
using MissNibiru.Enemies.Movement;
using MissNibiru.Enemies.Targeting;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Enemies.Tests
{
    public sealed class EnemyActorTests
    {
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

        private sealed class RecordingMovement :
            IEnemyMovementBehaviour
        {
            public EnemyContext Context;
            public int InitializeCount;
            public int ActivateCount;
            public int TickCount;
            public int DeactivateCount;

            public void Initialize(
                EnemyContext context)
            {
                Context = context;
                InitializeCount++;
            }

            public void Activate()
            {
                ActivateCount++;
            }

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public void Deactivate()
            {
                DeactivateCount++;
            }
        }

        private sealed class RecordingAttack :
            IEnemyAttackBehaviour
        {
            public EnemyContext Context;
            public int InitializeCount;
            public int ActivateCount;
            public int TickCount;
            public int DeactivateCount;

            public void Initialize(
                EnemyContext context)
            {
                Context = context;
                InitializeCount++;
            }

            public void Activate()
            {
                ActivateCount++;
            }

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public void Deactivate()
            {
                DeactivateCount++;
            }
        }

        private GameObject _enemyObject;
        private GameObject _targetObject;

        private HealthComponent _health;
        private EnemyActor _actor;

        private FixedTargetProvider _targetProvider;
        private RecordingMovement _movement;
        private RecordingAttack _firstAttack;
        private RecordingAttack _secondAttack;

        [SetUp]
        public void SetUp()
        {
            _enemyObject =
                new GameObject("Enemy Actor Test");

            _targetObject =
                new GameObject("Target Test");

            _health =
                _enemyObject.AddComponent<
                    HealthComponent>();

            SerializedObject serializedHealth =
                new SerializedObject(_health);

            serializedHealth
                .FindProperty("maxHealth")
                .floatValue = 100f;

            serializedHealth
                .ApplyModifiedPropertiesWithoutUndo();

            _health.ResetHealth();

            _actor =
                _enemyObject.AddComponent<
                    EnemyActor>();

            _targetProvider =
                new FixedTargetProvider
                {
                    Target = _targetObject.transform
                };

            _movement =
                new RecordingMovement();

            _firstAttack =
                new RecordingAttack();

            _secondAttack =
                new RecordingAttack();

            _actor.Configure(
                _health,
                _targetProvider,
                _movement,
                new IEnemyAttackBehaviour[]
                {
                    _firstAttack,
                    _secondAttack
                });
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
        public void Initialize_InitializesEveryBehaviour()
        {
            int initializedEvents = 0;

            _actor.Initialized +=
                _ => initializedEvents++;

            Assert.IsTrue(_actor.Initialize());

            Assert.AreEqual(
                1,
                _movement.InitializeCount);

            Assert.AreEqual(
                1,
                _firstAttack.InitializeCount);

            Assert.AreEqual(
                1,
                _secondAttack.InitializeCount);

            Assert.AreEqual(1, initializedEvents);

            Assert.AreSame(
                _actor.Context,
                _movement.Context);

            Assert.IsTrue(
                _actor.Context.TryGetTarget(
                    out Transform target));

            Assert.AreSame(
                _targetObject.transform,
                target);
        }

        [Test]
        public void ActivateAndTick_DrivesAllBehaviours()
        {
            Assert.IsTrue(_actor.Activate());

            _actor.Tick(0.25f);

            Assert.AreEqual(
                1,
                _movement.ActivateCount);

            Assert.AreEqual(
                1,
                _movement.TickCount);

            Assert.AreEqual(
                1,
                _firstAttack.ActivateCount);

            Assert.AreEqual(
                1,
                _firstAttack.TickCount);

            Assert.AreEqual(
                1,
                _secondAttack.ActivateCount);

            Assert.AreEqual(
                1,
                _secondAttack.TickCount);
        }

        [Test]
        public void Deactivate_StopsEveryBehaviour()
        {
            _actor.Activate();
            _actor.Deactivate();

            Assert.IsFalse(_actor.IsActive);

            Assert.AreEqual(
                1,
                _movement.DeactivateCount);

            Assert.AreEqual(
                1,
                _firstAttack.DeactivateCount);

            Assert.AreEqual(
                1,
                _secondAttack.DeactivateCount);
        }

        [Test]
        public void HealthDeath_DeactivatesAndRaisesEventOnce()
        {
            int diedEvents = 0;

            _actor.Died +=
                _ => diedEvents++;

            _actor.Activate();

            _health.TakeDamage(100f);
            _health.TakeDamage(100f);

            Assert.IsTrue(_actor.IsDead);
            Assert.IsFalse(_actor.IsActive);
            Assert.AreEqual(1, diedEvents);

            Assert.AreEqual(
                1,
                _movement.DeactivateCount);

            Assert.AreEqual(
                1,
                _firstAttack.DeactivateCount);

            Assert.AreEqual(
                1,
                _secondAttack.DeactivateCount);
        }

        [Test]
        public void ResetActor_AllowsReactivation()
        {
            _actor.Activate();
            _health.TakeDamage(100f);

            Assert.IsTrue(_actor.IsDead);
            Assert.IsTrue(_actor.ResetActor());
            Assert.IsFalse(_actor.IsDead);

            Assert.IsTrue(_actor.Activate());

            Assert.AreEqual(
                2,
                _movement.ActivateCount);

            Assert.AreEqual(
                2,
                _firstAttack.ActivateCount);
        }

        [Test]
        public void RepeatedActivate_DoesNotDoubleActivate()
        {
            _actor.Activate();
            _actor.Activate();

            Assert.AreEqual(
                1,
                _movement.ActivateCount);

            Assert.AreEqual(
                1,
                _firstAttack.ActivateCount);

            Assert.AreEqual(
                1,
                _secondAttack.ActivateCount);
        }
    }
}