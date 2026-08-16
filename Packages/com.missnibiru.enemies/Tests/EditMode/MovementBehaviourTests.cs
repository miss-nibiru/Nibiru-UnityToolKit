using MissNibiru.Core.Health;
using MissNibiru.Enemies.Actor;
using MissNibiru.Enemies.Movement;
using MissNibiru.Enemies.Targeting;
using NUnit.Framework;
using UnityEngine;

namespace MissNibiru.Enemies.Tests
{
    public sealed class MovementBehaviourTests
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

        private GameObject _enemyObject;
        private GameObject _targetObject;

        private EnemyActor _actor;
        private HealthComponent _health;
        private FixedTargetProvider _targetProvider;
        private EnemyContext _context;

        [SetUp]
        public void SetUp()
        {
            _enemyObject =
                new GameObject("Movement Test Enemy");

            _targetObject =
                new GameObject("Movement Test Target");

            _health =
                _enemyObject.AddComponent<
                    HealthComponent>();

            _actor =
                _enemyObject.AddComponent<
                    EnemyActor>();

            _targetProvider =
                new FixedTargetProvider
                {
                    Target = _targetObject.transform
                };

            _context = new EnemyContext(
                _actor,
                _enemyObject.transform,
                _health,
                _targetProvider);
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
        public void StationaryMovement_DoesNotMove()
        {
            StationaryMovement movement =
                _enemyObject.AddComponent<
                    StationaryMovement>();

            movement.Initialize(_context);
            movement.Activate();

            Vector3 startingPosition =
                _enemyObject.transform.position;

            movement.Tick(10f);

            Assert.AreEqual(
                startingPosition,
                _enemyObject.transform.position);
        }

        [Test]
        public void ChaseMovement_XYLocksZPosition()
        {
            _enemyObject.transform.position =
                Vector3.zero;

            _targetObject.transform.position =
                new Vector3(10f, 0f, 50f);

            ChaseMovement movement =
                _enemyObject.AddComponent<
                    ChaseMovement>();

            movement.Configure(
                2f,
                0f,
                MovementPlane.XY);

            movement.Initialize(_context);
            movement.Activate();
            movement.Tick(1f);

            Assert.That(
                _enemyObject.transform.position.x,
                Is.EqualTo(2f).Within(0.001f));

            Assert.That(
                _enemyObject.transform.position.z,
                Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ChaseMovement_RespectsStoppingDistance()
        {
            _enemyObject.transform.position =
                Vector3.zero;

            _targetObject.transform.position =
                new Vector3(1f, 0f, 0f);

            ChaseMovement movement =
                _enemyObject.AddComponent<
                    ChaseMovement>();

            movement.Configure(
                10f,
                0.25f,
                MovementPlane.XY);

            movement.Initialize(_context);
            movement.Activate();
            movement.Tick(1f);

            Assert.That(
                _enemyObject.transform.position.x,
                Is.EqualTo(0.75f).Within(0.001f));
        }

        [Test]
        public void ChaseMovement_WithoutTargetDoesNotMove()
        {
            _targetProvider.Target = null;

            ChaseMovement movement =
                _enemyObject.AddComponent<
                    ChaseMovement>();

            movement.Configure(
                10f,
                0f,
                MovementPlane.XY);

            movement.Initialize(_context);
            movement.Activate();
            movement.Tick(1f);

            Assert.AreEqual(
                Vector3.zero,
                _enemyObject.transform.position);
        }

        [Test]
        public void ChaseMovement_DeactivatedDoesNotMove()
        {
            _targetObject.transform.position =
                new Vector3(10f, 0f, 0f);

            ChaseMovement movement =
                _enemyObject.AddComponent<
                    ChaseMovement>();

            movement.Configure(
                5f,
                0f,
                MovementPlane.XY);

            movement.Initialize(_context);
            movement.Activate();
            movement.Deactivate();
            movement.Tick(1f);

            Assert.AreEqual(
                Vector3.zero,
                _enemyObject.transform.position);
        }

        [Test]
        public void PatrolMovement_PingPongs()
        {
            PatrolMovement movement =
                _enemyObject.AddComponent<
                    PatrolMovement>();

            movement.Configure(
                new[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(2f, 0f, 0f)
                },
                1f,
                0.001f,
                PatrolLoopMode.PingPong);

            movement.Initialize(_context);
            movement.Activate();

            movement.Tick(0f);
            movement.Tick(1f);

            Assert.That(
                _enemyObject.transform.position.x,
                Is.EqualTo(1f).Within(0.001f));

            movement.Tick(1f);

            Assert.That(
                _enemyObject.transform.position.x,
                Is.EqualTo(2f).Within(0.001f));

            movement.Tick(1f);

            Assert.That(
                _enemyObject.transform.position.x,
                Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void FormationMovement_UsesConfiguredDirections()
        {
            FormationMovement movement =
                _enemyObject.AddComponent<
                    FormationMovement>();

            movement.Configure(
                Vector3.right,
                1f,
                2f,
                Vector3.down,
                1f);

            movement.Initialize(_context);
            movement.Activate();

            movement.Tick(1f);

            Assert.That(
                _enemyObject.transform.position.x,
                Is.EqualTo(1f).Within(0.001f));

            Assert.That(
                _enemyObject.transform.position.y,
                Is.EqualTo(-1f).Within(0.001f));

            movement.Tick(1f);

            Assert.That(
                _enemyObject.transform.position.x,
                Is.EqualTo(-1f).Within(0.001f));

            Assert.That(
                _enemyObject.transform.position.y,
                Is.EqualTo(-2f).Within(0.001f));
        }
    }
}