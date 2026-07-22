using MissNibiru.Core.Health;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class HealthSystemTester : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float testValue;

    [SerializeField]
    private DamageCalculationMode calculationMode;

    private HealthComponent _health;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        _health = GetComponent<HealthComponent>();
        _health.HealthChanged += OnHealthChanged;
        _health.Died += OnDied;
    }

    private void OnDisable()
    {
        _health.HealthChanged -= OnHealthChanged;
        _health.Died -= OnDied;
    }

    [ContextMenu("Test/Take Calculated Damage")]
    private void TestDamage()
    {
        float calculatedDamage = DamageCalculator.Calculate(
            testValue,
            calculationMode,
            _health);

        Debug.Log(
            $"Calculated damage: {calculatedDamage} using {calculationMode}");

        _health.TakeDamage(calculatedDamage);
    }

    [ContextMenu("Test/Heal")]
    private void TestHeal()
    {
        _health.Heal(testValue);
    }

    [ContextMenu("Test/Reset Health")]
    private void TestReset()
    {
        _health.ResetHealth();
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        Debug.Log($"Health changed: {currentHealth}/{maxHealth}");
    }

    private void OnDied()
    {
        Debug.Log("Health target died!");
    }
}