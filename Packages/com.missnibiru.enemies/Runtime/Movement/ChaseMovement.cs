using MissNibiru.Enemies.Actor;
using UnityEngine;

namespace MissNibiru.Enemies.Movement
{
    public sealed class ChaseMovement :
        MonoBehaviour,
        IEnemyMovementBehaviour
    {
        [SerializeField, Min(0f)]
        private float speed = 3f;

        [SerializeField, Min(0f)]
        private float stoppingDistance = 0.05f;

        [SerializeField]
        private MovementPlane movementPlane =
            MovementPlane.XY;

        private EnemyContext _context;

        public bool IsActive { get; private set; }

        public void Configure(
            float movementSpeed,
            float stopDistance,
            MovementPlane plane)
        {
            speed = Mathf.Max(0f, movementSpeed);
            stoppingDistance =
                Mathf.Max(0f, stopDistance);

            movementPlane = plane;
        }

        public void Initialize(EnemyContext context)
        {
            _context = context;
        }

        public void Activate()
        {
            IsActive = _context != null;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive ||
                _context == null ||
                !_context.TryGetTarget(
                    out Transform target))
            {
                return;
            }

            Vector3 currentPosition =
                _context.Transform.position;

            Vector3 targetPosition =
                ConstrainTargetPosition(
                    currentPosition,
                    target.position);

            Vector3 difference =
                targetPosition - currentPosition;

            float distance = difference.magnitude;

            if (distance <= stoppingDistance ||
                distance <= Mathf.Epsilon)
            {
                return;
            }

            float permittedDistance =
                distance - stoppingDistance;

            float movementDistance = Mathf.Min(
                speed * Mathf.Max(0f, deltaTime),
                permittedDistance);

            _context.Transform.position =
                currentPosition +
                difference.normalized *
                movementDistance;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private Vector3 ConstrainTargetPosition(
            Vector3 currentPosition,
            Vector3 targetPosition)
        {
            switch (movementPlane)
            {
                case MovementPlane.XY:
                    targetPosition.z =
                        currentPosition.z;
                    break;

                case MovementPlane.XZ:
                    targetPosition.y =
                        currentPosition.y;
                    break;
            }

            return targetPosition;
        }
    }
}