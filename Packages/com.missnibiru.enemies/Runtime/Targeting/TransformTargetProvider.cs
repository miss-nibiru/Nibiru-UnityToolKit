using UnityEngine;

namespace MissNibiru.Enemies.Targeting
{
    public sealed class TransformTargetProvider :
        MonoBehaviour,
        IEnemyTargetProvider
    {
        [SerializeField]
        private Transform target;

        public Transform Target => target;

        public bool TryGetTarget(out Transform foundTarget)
        {
            foundTarget = target;
            return foundTarget != null;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void ClearTarget()
        {
            target = null;
        }
    }
}