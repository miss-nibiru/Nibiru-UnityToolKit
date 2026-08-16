using UnityEngine;

namespace MissNibiru.Enemies.Targeting
{
    public interface IEnemyTargetProvider
    {
        bool TryGetTarget(out Transform target);
    }
}