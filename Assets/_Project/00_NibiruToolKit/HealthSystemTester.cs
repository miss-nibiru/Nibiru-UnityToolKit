using MissNibiru.Core.Health;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class HealthSystemTester : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float testAmount = 25f;

    private HealthComponent health;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        health = GetComponent<HealthComponent>();
        health.HealthChanged += OnHealthChanged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.HealthChanged -= OnHealthChanged;
        health.Died -= OnDied;
    }

    [ContextMenu("Test/Take Damage")]
    private void TestDamage()
    {
        health.TakeDamage(testAmount);
    }

    [ContextMenu("Test/Heal")]
    private void TestHeal()
    {
        health.Heal(testAmount);
    }

    [ContextMenu("Test/Reset Health")]
    private void TestReset()
    {
        health.ResetHealth();
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