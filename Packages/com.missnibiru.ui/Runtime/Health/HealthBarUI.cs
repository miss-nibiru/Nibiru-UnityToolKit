using MissNibiru.Core.Health;
using UnityEngine;
using UnityEngine.UI;

namespace MissNibiru.UI.Health
{
    [DisallowMultipleComponent]
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField]
        private Image fillImage;

        [SerializeField]
        private MonoBehaviour healthSourceComponent;

        private IHealthSource _healthSource;
        private bool _isSubscribed;

        private void Awake()
        {
            ResolveHealthSource();
        }

        private void OnEnable()
        {
            ResolveHealthSource();
            SubscribeToHealthSource();
            RefreshBar();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealthSource();
        }

        public void SetHealthSource(MonoBehaviour newHealthSourceComponent)
        {
            if (newHealthSourceComponent != null &&
                newHealthSourceComponent is not IHealthSource)
            {
                Debug.LogError(
                    $"{newHealthSourceComponent.name} does not implement IHealthSource.",
                    newHealthSourceComponent);

                return;
            }

            healthSourceComponent = newHealthSourceComponent;
            SetHealthSource(newHealthSourceComponent as IHealthSource);
        }

        public void SetHealthSource(IHealthSource newHealthSource)
        {
            UnsubscribeFromHealthSource();

            _healthSource = newHealthSource;

            SubscribeToHealthSource();
            RefreshBar();
        }

        private void ResolveHealthSource()
        {
            if (healthSourceComponent == null)
            {
                _healthSource = null; return;
            }

            _healthSource = healthSourceComponent as IHealthSource;

            if (_healthSource == null)
            {
                Debug.LogError($"{healthSourceComponent.name} must implement IHealthSource.", healthSourceComponent);
            }
        }

        private void SubscribeToHealthSource()
        {
            if (_healthSource == null || _isSubscribed) return;
            _healthSource.HealthChanged += UpdateHealthBar;
            _isSubscribed = true;
        }

        private void UnsubscribeFromHealthSource()
        {
            if (_healthSource == null || !_isSubscribed) return;
            _healthSource.HealthChanged -= UpdateHealthBar;
            _isSubscribed = false;
        }

        private void RefreshBar()
        {
            if (_healthSource == null) return;

            UpdateHealthBar(
                _healthSource.CurrentHealth,
                _healthSource.MaxHealth);
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (fillImage == null || maxHealth <= 0f) return;
            fillImage.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }
}