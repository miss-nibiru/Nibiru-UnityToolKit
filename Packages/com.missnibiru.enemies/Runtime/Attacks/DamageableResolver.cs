using MissNibiru.Core.Health;
using UnityEngine;

namespace MissNibiru.Enemies.Attacks
{
    public static class DamageableResolver
    {
        public static bool TryFind(
            Component source,
            out IDamageable damageable)
        {
            damageable = null;

            if (source == null)
                return false;

            MonoBehaviour[] behaviours =
                source.GetComponentsInParent<MonoBehaviour>(
                    true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageable candidate)
                {
                    damageable = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}