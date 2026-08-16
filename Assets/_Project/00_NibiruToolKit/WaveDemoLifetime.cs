using UnityEngine;

public sealed class WaveDemoLifetime : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float lifetime = 2f;

    private void OnEnable()
    {
        Invoke(nameof(EndLifetime), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void EndLifetime()
    {
        Destroy(gameObject);
    }
}