using System;
using System.Collections.Generic;
using MissNibiru.Waves.Data;
using MissNibiru.Waves.Spawning;
using MissNibiru.Waves.Tracking;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MissNibiru.Waves.Execution
{
    public enum WaveRunnerState
    {
        Idle,
        InitialDelay,
        Spawning,
        WaitingForCompletion,
        Completed,
        Stopped
    }

    [DisallowMultipleComponent]
    public sealed class WaveRunner : MonoBehaviour
    {
        private sealed class GroupRuntime
        {
            public GroupRuntime(WaveSpawnGroupData data)
            {
                Data = data;
            }

            public WaveSpawnGroupData Data { get; }
            public int SpawnedCount;
            public float Cooldown;

            public readonly HashSet<WaveSpawnedObject>
                ActiveObjects =
                    new HashSet<WaveSpawnedObject>();
        }

        [Header("Wave Sequence")]

        [SerializeField]
        private WaveData[] waves;

        [SerializeField]
        private bool playOnStart;

        [SerializeField]
        private bool loopSequence;

        [Header("Dependencies")]

        [SerializeField]
        private MonoBehaviour spawnPointProviderSource;

        [SerializeField]
        private MonoBehaviour spawnerSource;

        [SerializeField]
        private Transform spawnedObjectParent;

        private readonly List<GroupRuntime> _groups =
            new List<GroupRuntime>();

        private ISpawnPointProvider _spawnPointProvider;
        private IWaveSpawner _spawner;

        private WaveData _currentWave;
        private int _currentWaveIndex = -1;
        private int _nextGroupIndex;

        private float _initialDelayRemaining;
        private float _waveElapsed;
        private float _globalSpawnCooldown;

        public event Action<int, WaveData> WaveStarted;
        public event Action<int, WaveData> WaveCompleted;
        public event Action SequenceCompleted;
        public event Action<GameObject> ObjectSpawned;

        public WaveRunnerState State { get; private set; } =
            WaveRunnerState.Idle;

        public int CurrentWaveIndex => _currentWaveIndex;
        public WaveData CurrentWave => _currentWave;

        public bool IsRunning =>
            State == WaveRunnerState.InitialDelay ||
            State == WaveRunnerState.Spawning ||
            State == WaveRunnerState.WaitingForCompletion;

        public int ActiveObjectCount
        {
            get
            {
                int total = 0;

                foreach (GroupRuntime group in _groups)
                    total += group.ActiveObjects.Count;

                return total;
            }
        }

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Start()
        {
            if (playOnStart)
                StartSequence();
        }

        private void Update()
        {
            if (IsRunning)
                Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            if (IsRunning)
                Stop(false);
        }

        public void Configure(
            WaveData[] waveSequence,
            ISpawnPointProvider spawnPointProvider,
            IWaveSpawner spawner)
        {
            waves = waveSequence;
            _spawnPointProvider = spawnPointProvider;
            _spawner = spawner;
        }

        [ContextMenu("Waves/Start Sequence")]
        public bool StartSequence()
        {
            return StartWave(0);
        }

        public bool StartWave(int waveIndex)
        {
            if (!ResolveDependencies())
            {
                Debug.LogError(
                    "WaveRunner requires an ISpawnPointProvider " +
                    "and an IWaveSpawner.",
                    this);

                return false;
            }

            if (waves == null ||
                waveIndex < 0 ||
                waveIndex >= waves.Length ||
                waves[waveIndex] == null)
            {
                Debug.LogError(
                    "WaveRunner cannot start because the wave " +
                    "index or WaveData is invalid.",
                    this);

                return false;
            }

            if (IsRunning || ActiveObjectCount > 0)
                Stop(true);

            BeginWave(waveIndex);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
                return;

            float remainingTime = Mathf.Max(0f, deltaTime);

            if (State == WaveRunnerState.InitialDelay)
            {
                _initialDelayRemaining -= remainingTime;

                if (_initialDelayRemaining > 0f)
                    return;

                remainingTime =
                    Mathf.Max(0f, -_initialDelayRemaining);

                State = WaveRunnerState.Spawning;
            }

            if (State == WaveRunnerState.Spawning)
                TickSpawning(remainingTime);

            if (State ==
                    WaveRunnerState.WaitingForCompletion &&
                (!_currentWave.WaitForActiveObjectsToClear ||
                 ActiveObjectCount == 0))
            {
                CompleteCurrentWave();
            }
        }

        [ContextMenu("Waves/Stop")]
        public void Stop()
        {
            Stop(false);
        }

        public void Stop(bool despawnActiveObjects)
        {
            if (despawnActiveObjects)
                DespawnTrackedObjects();
            else
                DetachTrackedObjects();

            _currentWave = null;
            _currentWaveIndex = -1;
            _groups.Clear();

            State = WaveRunnerState.Stopped;
        }

        private bool ResolveDependencies()
        {
            if (_spawnPointProvider == null)
            {
                _spawnPointProvider =
                    spawnPointProviderSource
                        as ISpawnPointProvider;
            }

            if (_spawner == null)
                _spawner = spawnerSource as IWaveSpawner;

            return _spawnPointProvider != null &&
                   _spawner != null;
        }

        private void BeginWave(int waveIndex)
        {
            _currentWaveIndex = waveIndex;
            _currentWave = waves[waveIndex];

            _groups.Clear();
            _nextGroupIndex = 0;
            _waveElapsed = 0f;
            _globalSpawnCooldown = 0f;

            WaveSpawnGroupData[] spawnGroups =
                _currentWave.SpawnGroups;

            if (spawnGroups != null)
            {
                foreach (WaveSpawnGroupData group in spawnGroups)
                {
                    if (group == null || group.Prefab == null)
                        continue;

                    _groups.Add(new GroupRuntime(group));
                }
            }

            _initialDelayRemaining =
                Mathf.Max(0f, _currentWave.InitialDelay);

            State = _initialDelayRemaining > 0f
                ? WaveRunnerState.InitialDelay
                : WaveRunnerState.Spawning;

            WaveStarted?.Invoke(
                _currentWaveIndex,
                _currentWave);
        }

        private void TickSpawning(float deltaTime)
        {
            _waveElapsed += deltaTime;
            _globalSpawnCooldown -= deltaTime;

            foreach (GroupRuntime group in _groups)
                group.Cooldown -= deltaTime;

            bool durationFinished =
                _currentWave.UsesDuration &&
                _waveElapsed >= _currentWave.Duration;

            if (!durationFinished)
                TrySpawnNextGroup();

            if (durationFinished ||
                AreAllGroupsExhausted())
            {
                State =
                    WaveRunnerState.WaitingForCompletion;
            }
        }

        private bool TrySpawnNextGroup()
        {
            if (_globalSpawnCooldown > 0f ||
                _groups.Count == 0)
            {
                return false;
            }

            for (int offset = 0;
                 offset < _groups.Count;
                 offset++)
            {
                int index =
                    (_nextGroupIndex + offset) %
                    _groups.Count;

                GroupRuntime group = _groups[index];

                if (!CanSpawn(group))
                    continue;

                _nextGroupIndex =
                    (index + 1) % _groups.Count;

                bool spawned = SpawnGroup(group);

                float minimumDelay = Mathf.Max(
                    0f,
                    _currentWave.SpawnInterval);

                _globalSpawnCooldown = spawned
                    ? minimumDelay
                    : Mathf.Max(0.1f, minimumDelay);

                group.Cooldown = spawned
                    ? Mathf.Max(
                        0f,
                        group.Data.DelayBetweenSpawns)
                    : Mathf.Max(
                        0.1f,
                        group.Data.DelayBetweenSpawns);

                return spawned;
            }

            return false;
        }

        private static bool CanSpawn(GroupRuntime group)
        {
            if (group == null ||
                group.Data == null ||
                group.Data.Prefab == null ||
                group.Cooldown > 0f)
            {
                return false;
            }

            if (group.Data.LimitTotalSpawns &&
                group.SpawnedCount >=
                Mathf.Max(
                    1,
                    group.Data.TotalSpawnLimit))
            {
                return false;
            }

            if (group.Data.LimitActiveInstances &&
                group.ActiveObjects.Count >=
                Mathf.Max(
                    1,
                    group.Data.MaximumActiveInstances))
            {
                return false;
            }

            return true;
        }

        private bool SpawnGroup(GroupRuntime group)
        {
            int spawnCount = Mathf.Max(
                1,
                group.Data.InstancesPerSpawn);

            if (group.Data.LimitTotalSpawns)
            {
                int remainingTotal =
                    Mathf.Max(
                        1,
                        group.Data.TotalSpawnLimit) -
                    group.SpawnedCount;

                spawnCount = Mathf.Min(
                    spawnCount,
                    remainingTotal);
            }

            if (group.Data.LimitActiveInstances)
            {
                int remainingActive =
                    Mathf.Max(
                        1,
                        group.Data.MaximumActiveInstances) -
                    group.ActiveObjects.Count;

                spawnCount = Mathf.Min(
                    spawnCount,
                    remainingActive);
            }

            if (spawnCount <= 0)
                return false;

            if (!_spawnPointProvider.TryGetSpawnPoint(
                    group.Data.SpawnPointTags,
                    out Pose basePose))
            {
                return false;
            }

            WaveSpawnGroupData.SpawnPattern pattern =
                SelectPattern(group.Data);

            bool spawnedAnything = false;

            for (int i = 0; i < spawnCount; i++)
            {
                Pose pose = CreatePatternPose(
                    basePose,
                    group.Data,
                    pattern,
                    i,
                    spawnCount);

                GameObject instance = _spawner.Spawn(
                    group.Data.Prefab,
                    pose,
                    spawnedObjectParent);

                if (instance == null)
                    continue;

                WaveSpawnedObject trackedObject =
                    instance.GetComponent<
                        WaveSpawnedObject>();

                if (trackedObject == null)
                {
                    trackedObject =
                        instance.AddComponent<
                            WaveSpawnedObject>();
                }

                trackedObject.Released +=
                    HandleTrackedObjectReleased;

                group.ActiveObjects.Add(trackedObject);
                group.SpawnedCount++;

                spawnedAnything = true;
                ObjectSpawned?.Invoke(instance);
            }

            return spawnedAnything;
        }

        private static WaveSpawnGroupData.SpawnPattern
            SelectPattern(WaveSpawnGroupData data)
        {
            WaveSpawnGroupData.SpawnPattern[] patterns =
                data.AllowedPatterns;

            if (patterns == null || patterns.Length == 0)
            {
                return WaveSpawnGroupData
                    .SpawnPattern.Single;
            }

            return patterns[
                Random.Range(0, patterns.Length)];
        }

        private static Pose CreatePatternPose(
            Pose basePose,
            WaveSpawnGroupData data,
            WaveSpawnGroupData.SpawnPattern pattern,
            int index,
            int totalCount)
        {
            Vector3 localOffset = Vector3.zero;
            float spacing = Mathf.Max(
                0f,
                data.PatternSpacing);

            switch (pattern)
            {
                case WaveSpawnGroupData
                    .SpawnPattern.Cluster:

                    if (totalCount > 1)
                    {
                        float angle =
                            Mathf.PI * 2f *
                            index /
                            totalCount;

                        localOffset = new Vector3(
                            Mathf.Cos(angle),
                            0f,
                            Mathf.Sin(angle)) *
                            spacing;
                    }

                    break;

                case WaveSpawnGroupData
                    .SpawnPattern.Line:

                    Vector3 direction =
                        data.LineDirection.sqrMagnitude >
                        0f
                            ? data.LineDirection.normalized
                            : Vector3.right;

                    localOffset =
                        direction * spacing * index;

                    break;
            }

            Vector3 worldOffset =
                basePose.rotation * localOffset;

            return new Pose(
                basePose.position + worldOffset,
                basePose.rotation);
        }

        private void HandleTrackedObjectReleased(
            WaveSpawnedObject trackedObject)
        {
            if (trackedObject == null)
                return;

            trackedObject.Released -=
                HandleTrackedObjectReleased;

            foreach (GroupRuntime group in _groups)
                group.ActiveObjects.Remove(trackedObject);
        }

        private bool AreAllGroupsExhausted()
        {
            if (_groups.Count == 0)
                return true;

            foreach (GroupRuntime group in _groups)
            {
                if (!group.Data.LimitTotalSpawns)
                    return false;

                if (group.SpawnedCount <
                    Mathf.Max(
                        1,
                        group.Data.TotalSpawnLimit))
                {
                    return false;
                }
            }

            return true;
        }

        private void CompleteCurrentWave()
        {
            int completedIndex = _currentWaveIndex;
            WaveData completedWave = _currentWave;

            if (_currentWave
                .DespawnActiveObjectsOnCompletion)
            {
                DespawnTrackedObjects();
            }
            else
            {
                DetachTrackedObjects();
            }

            WaveCompleted?.Invoke(
                completedIndex,
                completedWave);

            int nextIndex = completedIndex + 1;

            if (waves != null && nextIndex < waves.Length)
            {
                BeginWave(nextIndex);
                return;
            }

            if (loopSequence &&
                waves != null &&
                waves.Length > 0)
            {
                BeginWave(0);
                return;
            }

            _currentWave = null;
            State = WaveRunnerState.Completed;
            SequenceCompleted?.Invoke();
        }

        private void DetachTrackedObjects()
        {
            foreach (GroupRuntime group in _groups)
            {
                foreach (
                    WaveSpawnedObject trackedObject
                    in group.ActiveObjects)
                {
                    if (trackedObject != null)
                    {
                        trackedObject.Released -=
                            HandleTrackedObjectReleased;
                    }
                }

                group.ActiveObjects.Clear();
            }
        }

        private void DespawnTrackedObjects()
        {
            List<WaveSpawnedObject> objectsToDespawn =
                new List<WaveSpawnedObject>();

            foreach (GroupRuntime group in _groups)
            {
                objectsToDespawn.AddRange(
                    group.ActiveObjects);

                group.ActiveObjects.Clear();
            }

            foreach (
                WaveSpawnedObject trackedObject
                in objectsToDespawn)
            {
                if (trackedObject == null)
                    continue;

                trackedObject.Released -=
                    HandleTrackedObjectReleased;

                _spawner?.Despawn(
                    trackedObject.gameObject);
            }
        }
    }
}