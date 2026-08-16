using UnityEngine;

namespace MissNibiru.Combat.Attacks
{
    [CreateAssetMenu(
        fileName = "ProjectileAttackConfiguration",
        menuName =
            "Miss Nibiru/Combat/Projectile Attack Configuration")]
    public sealed class ProjectileAttackConfiguration :
        ScriptableObject
    {
        [Header("Projectile")]

        [SerializeField, Min(0f)]
        private float projectileSpeed = 10f;

        [SerializeField, Min(0f)]
        private float projectileDamage = 1f;

        [SerializeField, Min(0.01f)]
        private float projectileLifetime = 5f;

        [Header("Volley")]

        [SerializeField, Min(1)]
        private int projectilesPerVolley = 1;

        [SerializeField, Min(0f)]
        private float totalSpreadAngle;

        [SerializeField]
        private ProjectileSpreadMode spreadMode =
            ProjectileSpreadMode.None;

        [SerializeField]
        private Vector3 spreadAxis =
            Vector3.forward;

        [Header("Sequence")]

        [SerializeField, Min(1)]
        private int shotsPerSequence = 1;

        [SerializeField, Min(0f)]
        private float delayBetweenSequenceShots =
            0.1f;

        [SerializeField, Min(0f)]
        private float cooldownAfterSequence =
            0.5f;

        public float ProjectileSpeed =>
            projectileSpeed;

        public float ProjectileDamage =>
            projectileDamage;

        public float ProjectileLifetime =>
            projectileLifetime;

        public int ProjectilesPerVolley =>
            projectilesPerVolley;

        public float TotalSpreadAngle =>
            totalSpreadAngle;

        public ProjectileSpreadMode SpreadMode =>
            spreadMode;

        public Vector3 SpreadAxis =>
            spreadAxis.sqrMagnitude > 0f
                ? spreadAxis.normalized
                : Vector3.forward;

        public int ShotsPerSequence =>
            shotsPerSequence;

        public float DelayBetweenSequenceShots =>
            delayBetweenSequenceShots;

        public float CooldownAfterSequence =>
            cooldownAfterSequence;

        public void Configure(
            float speed,
            float damage,
            float lifetime,
            int volleyProjectileCount,
            float spreadAngle,
            ProjectileSpreadMode projectileSpreadMode,
            Vector3 projectileSpreadAxis,
            int sequenceShotCount,
            float sequenceShotDelay,
            float sequenceCooldown)
        {
            projectileSpeed =
                Mathf.Max(0f, speed);

            projectileDamage =
                Mathf.Max(0f, damage);

            projectileLifetime =
                Mathf.Max(0.01f, lifetime);

            projectilesPerVolley =
                Mathf.Max(1, volleyProjectileCount);

            totalSpreadAngle =
                Mathf.Max(0f, spreadAngle);

            spreadMode =
                projectileSpreadMode;

            spreadAxis =
                projectileSpreadAxis.sqrMagnitude > 0f
                    ? projectileSpreadAxis.normalized
                    : Vector3.forward;

            shotsPerSequence =
                Mathf.Max(1, sequenceShotCount);

            delayBetweenSequenceShots =
                Mathf.Max(0f, sequenceShotDelay);

            cooldownAfterSequence =
                Mathf.Max(0f, sequenceCooldown);
        }

        public float GetSpreadAngle(
            int projectileIndex)
        {
            if (spreadMode ==
                    ProjectileSpreadMode.None ||
                totalSpreadAngle <= 0f)
            {
                return 0f;
            }

            float halfSpread =
                totalSpreadAngle * 0.5f;

            if (spreadMode ==
                ProjectileSpreadMode.Random)
            {
                return Random.Range(
                    -halfSpread,
                    halfSpread);
            }

            if (projectilesPerVolley <= 1)
                return 0f;

            int safeIndex = Mathf.Clamp(
                projectileIndex,
                0,
                projectilesPerVolley - 1);

            float angleStep =
                totalSpreadAngle /
                (projectilesPerVolley - 1);

            return -halfSpread +
                   angleStep * safeIndex;
        }

        private void OnValidate()
        {
            projectileSpeed =
                Mathf.Max(0f, projectileSpeed);

            projectileDamage =
                Mathf.Max(0f, projectileDamage);

            projectileLifetime =
                Mathf.Max(0.01f, projectileLifetime);

            projectilesPerVolley =
                Mathf.Max(1, projectilesPerVolley);

            totalSpreadAngle =
                Mathf.Max(0f, totalSpreadAngle);

            shotsPerSequence =
                Mathf.Max(1, shotsPerSequence);

            delayBetweenSequenceShots =
                Mathf.Max(
                    0f,
                    delayBetweenSequenceShots);

            cooldownAfterSequence =
                Mathf.Max(
                    0f,
                    cooldownAfterSequence);

            if (spreadAxis.sqrMagnitude <= 0f)
                spreadAxis = Vector3.forward;
        }
    }
}