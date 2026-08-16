#if NIBIRU_ASSIGNMENT_STRESS
using MissNibiru.Combat.Projectiles;
using UnityEngine;

/// <summary>
/// Assignment-only deterministic runtime stress scenario.
/// Included only when the NIBIRU_ASSIGNMENT_STRESS build define is supplied.
/// </summary>
public sealed class AssignmentStressScenario : MonoBehaviour
{
    private const float ScenarioDurationSeconds = 30f;
    private const int EmissionsPerFrame = 4;
    private const float ProjectileLifetimeSeconds = 2f;
    private const float ProjectileSpeed = 8f;

    private PrefabProjectileEmitter _emitter;
    private ProjectileActor _projectileTemplate;

    private float _elapsed;
    private float _frameTimeTotal;
    private float _maxFrameTime;
    private int _frameCount;
    private int _emittedCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartScenario()
    {
        GameObject host = new GameObject("Assignment Stress Scenario");
        DontDestroyOnLoad(host);
        host.AddComponent<AssignmentStressScenario>();
    }

    private void Awake()
    {
        GameObject emitterObject = new GameObject("Stress Projectile Emitter");
        emitterObject.transform.SetParent(transform, false);
        _emitter = emitterObject.AddComponent<PrefabProjectileEmitter>();

        GameObject templateObject = new GameObject("Stress Projectile Template");
        templateObject.transform.SetParent(transform, false);
        _projectileTemplate = templateObject.AddComponent<ProjectileActor>();

        _emitter.Configure(_projectileTemplate, transform);

        Debug.Log(
            "NIBIRU_STRESS_START " +
            $"duration={ScenarioDurationSeconds:0}s " +
            $"emissionsPerFrame={EmissionsPerFrame} " +
            $"projectileLifetime={ProjectileLifetimeSeconds:0.00}s");
    }

    private void Update()
    {
        float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);

        _elapsed += deltaTime;
        _frameTimeTotal += deltaTime;
        _maxFrameTime = Mathf.Max(_maxFrameTime, deltaTime);
        _frameCount++;

        EmitProjectiles();

        if (_elapsed >= ScenarioDurationSeconds)
            CompleteScenario();
    }

    private void EmitProjectiles()
    {
        for (int i = 0; i < EmissionsPerFrame; i++)
        {
            float angle = (_emittedCount * 19f) % 360f;
            float radians = angle * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(
                Mathf.Cos(radians),
                Mathf.Sin(radians),
                0f);

            ProjectileSpawnRequest request =
                new ProjectileSpawnRequest(
                    transform.position,
                    direction,
                    ProjectileSpeed,
                    0f,
                    ProjectileLifetimeSeconds,
                    transform);

            if (_emitter.TryEmit(request))
                _emittedCount++;
        }
    }

    private void CompleteScenario()
    {
        float averageFrameSeconds =
            _frameCount > 0
                ? _frameTimeTotal / _frameCount
                : 0f;

        Debug.Log(
            "NIBIRU_STRESS_RESULT " +
            $"duration={_elapsed:0.000}s " +
            $"frames={_frameCount} " +
            $"emitted={_emittedCount} " +
            $"averageFrameMs={averageFrameSeconds * 1000f:0.000} " +
            $"maxFrameMs={_maxFrameTime * 1000f:0.000}");

        enabled = false;
        Application.Quit(0);
    }
}
#endif
