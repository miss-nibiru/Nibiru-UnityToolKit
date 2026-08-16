using MissNibiru.Enemies.Actor;
using UnityEngine;

namespace MissNibiru.Enemies.Movement
{
    public sealed class StationaryMovement :
        MonoBehaviour,
        IEnemyMovementBehaviour
    {
        private EnemyContext _context;

        public bool IsActive { get; private set; }

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
            // Explicitly stationary.
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}