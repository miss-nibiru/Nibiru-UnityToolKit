using System;

namespace MissNibiru.Core.Health
{
    public static class DamageCalculator
    {
        public static float Calculate(
            float value,
            DamageCalculationMode mode,
            IHealthSource healthSource = null)
        {
            return mode switch
            {
                DamageCalculationMode.Flat => value,
                DamageCalculationMode.FractionOfMaximumHealth => CalculateFractionOfMaximumHealth(value, healthSource),

                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        private static float CalculateFractionOfMaximumHealth(
            float fraction,
            IHealthSource healthSource)
        {
            if (healthSource == null)
            {
                throw new ArgumentNullException(nameof(healthSource), "A health source is required for fractional damage.");
            }

            return healthSource.MaxHealth * fraction;
        }
    }
}