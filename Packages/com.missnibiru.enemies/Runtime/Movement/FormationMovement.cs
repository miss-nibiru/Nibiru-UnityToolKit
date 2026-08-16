using MissNibiru.Enemies.Actor;
using UnityEngine;

namespace MissNibiru.Enemies.Movement
{
    public sealed class FormationMovement :
        MonoBehaviour,
        IEnemyMovementBehaviour
    {
        [Header("Oscillation")]

        [SerializeField]
        private Vector3 oscillationAxis =
            Vector3.right;

        [SerializeField, Min(0f)]
        private float oscillationDistance = 4f;

        [SerializeField, Min(0f)]
        private float oscillationSpeed = 2f;

        [Header("Advancement")]

        [SerializeField]
        private Vector3 advanceDirection =
            Vector3.down;

        [SerializeField, Min(0f)]
        private float advanceSpeed = 0.5f;

        private EnemyContext _context;

        private float _signedOscillationOffset;
        private int _oscillationDirection = 1;

        public bool IsActive { get; private set; }

        public void Configure(
            Vector3 sideToSideAxis,
            float sideToSideDistance,
            float sideToSideSpeed,
            Vector3 forwardDirection,
            float forwardSpeed)
        {
            oscillationAxis = sideToSideAxis;

            oscillationDistance =
                Mathf.Max(0f, sideToSideDistance);

            oscillationSpeed =
                Mathf.Max(0f, sideToSideSpeed);

            advanceDirection = forwardDirection;

            advanceSpeed =
                Mathf.Max(0f, forwardSpeed);
        }

        public void Initialize(EnemyContext context)
        {
            _context = context;
        }

        public void Activate()
        {
            if (_context == null)
                return;

            _signedOscillationOffset = 0f;
            _oscillationDirection = 1;
            IsActive = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive || _context == null)
                return;

            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);

            Vector3 normalizedOscillation =
                oscillationAxis.sqrMagnitude > 0f
                    ? oscillationAxis.normalized
                    : Vector3.zero;

            Vector3 normalizedAdvance =
                advanceDirection.sqrMagnitude > 0f
                    ? advanceDirection.normalized
                    : Vector3.zero;

            float desiredOffset =
                _signedOscillationOffset +
                _oscillationDirection *
                oscillationSpeed *
                safeDeltaTime;

            if (oscillationDistance <= 0f)
            {
                desiredOffset = 0f;
            }
            else if (desiredOffset >=
                     oscillationDistance)
            {
                desiredOffset =
                    oscillationDistance;

                _oscillationDirection = -1;
            }
            else if (desiredOffset <=
                     -oscillationDistance)
            {
                desiredOffset =
                    -oscillationDistance;

                _oscillationDirection = 1;
            }

            float oscillationChange =
                desiredOffset -
                _signedOscillationOffset;

            Vector3 movement =
                normalizedOscillation *
                oscillationChange;

            movement +=
                normalizedAdvance * (advanceSpeed * safeDeltaTime);

            _context.Transform.position += movement;

            _signedOscillationOffset =
                desiredOffset;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}