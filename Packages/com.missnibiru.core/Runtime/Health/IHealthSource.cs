using System;

namespace MissNibiru.Core.Health
{
    public interface IHealthSource
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }

        event Action<float, float> HealthChanged;
        event Action Died;
    }
}