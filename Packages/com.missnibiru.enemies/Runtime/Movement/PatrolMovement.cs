using MissNibiru.Enemies.Actor;
using UnityEngine;

namespace MissNibiru.Enemies.Movement
{
    public sealed class PatrolMovement :
        MonoBehaviour,
        IEnemyMovementBehaviour
    {
        [SerializeField]
        private Transform[] waypoints;

        [SerializeField, Min(0f)]
        private float speed = 2f;

        [SerializeField, Min(0f)]
        private float arrivalDistance = 0.05f;

        [SerializeField]
        private PatrolLoopMode loopMode =
            PatrolLoopMode.PingPong;

        private EnemyContext _context;
        private Vector3[] _positions;
        private Vector3[] _configuredPositions;

        private int _currentIndex;
        private int _direction = 1;

        public bool IsActive { get; private set; }
        public bool IsComplete { get; private set; }

        public void Configure(
            Vector3[] patrolPositions,
            float movementSpeed,
            float waypointArrivalDistance,
            PatrolLoopMode mode)
        {
            _configuredPositions =
                patrolPositions != null
                    ? (Vector3[])patrolPositions.Clone()
                    : null;

            speed = Mathf.Max(0f, movementSpeed);

            arrivalDistance = Mathf.Max(
                0f,
                waypointArrivalDistance);

            loopMode = mode;
        }

        public void Initialize(EnemyContext context)
        {
            _context = context;
            CreatePositionSnapshot();

            _currentIndex = 0;
            _direction = 1;
            IsComplete = false;
        }

        public void Activate()
        {
            IsActive =
                _context != null &&
                _positions != null &&
                _positions.Length > 0 &&
                !IsComplete;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive ||
                _context == null ||
                _positions == null ||
                _positions.Length == 0)
            {
                return;
            }

            Vector3 targetPosition =
                _positions[_currentIndex];

            _context.Transform.position =
                Vector3.MoveTowards(
                    _context.Transform.position,
                    targetPosition,
                    speed * Mathf.Max(0f, deltaTime));

            if (Vector3.Distance(
                    _context.Transform.position,
                    targetPosition) <= arrivalDistance)
            {
                AdvanceWaypoint();
            }
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private void CreatePositionSnapshot()
        {
            if (_configuredPositions != null)
            {
                _positions =
                    (Vector3[])_configuredPositions.Clone();

                return;
            }

            if (waypoints == null ||
                waypoints.Length == 0)
            {
                _positions = new Vector3[0];
                return;
            }

            _positions =
                new Vector3[waypoints.Length];

            for (int i = 0;
                 i < waypoints.Length;
                 i++)
            {
                _positions[i] =
                    waypoints[i] != null
                        ? waypoints[i].position
                        : transform.position;
            }
        }

        private void AdvanceWaypoint()
        {
            if (_positions.Length <= 1)
            {
                if (loopMode == PatrolLoopMode.Once)
                {
                    IsComplete = true;
                    IsActive = false;
                }

                return;
            }

            switch (loopMode)
            {
                case PatrolLoopMode.Loop:

                    _currentIndex =
                        (_currentIndex + 1) %
                        _positions.Length;

                    break;

                case PatrolLoopMode.PingPong:

                    if (_currentIndex ==
                        _positions.Length - 1)
                    {
                        _direction = -1;
                    }
                    else if (_currentIndex == 0)
                    {
                        _direction = 1;
                    }

                    _currentIndex += _direction;
                    break;

                case PatrolLoopMode.Once:

                    if (_currentIndex >=
                        _positions.Length - 1)
                    {
                        IsComplete = true;
                        IsActive = false;
                    }
                    else
                    {
                        _currentIndex++;
                    }

                    break;
            }
        }
    }
}