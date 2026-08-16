using System.Collections.Generic;
using MissNibiru.Waves.Data;
using MissNibiru.Waves.Execution;
using MissNibiru.Waves.Spawning;
using MissNibiru.Waves.Tracking;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Waves.Tests
{
    public sealed class WaveRunnerTests
    {
        private sealed class FixedSpawnProvider :
            ISpawnPointProvider
        {
            public Pose Pose = new Pose(
                Vector3.zero,
                Quaternion.identity);

            public bool TryGetSpawnPoint(
                string[] allowedTags,
                out Pose spawnPose)
            {
                spawnPose = Pose;
                return true;
            }
        }

        private sealed class RecordingSpawner :
            IWaveSpawner
        {
            public readonly List<GameObject> Instances =
                new List<GameObject>();

            public readonly List<Pose> SpawnedPoses =
                new List<Pose>();

            public int DespawnCount;

            public GameObject Spawn(
                GameObject prefab,
                Pose pose,
                Transform parent)
            {
                GameObject instance =
                    new GameObject("Recorded Spawn");

                instance.transform.SetPositionAndRotation(
                    pose.position,
                    pose.rotation);

                Instances.Add(instance);
                SpawnedPoses.Add(pose);

                return instance;
            }

            public void Despawn(GameObject instance)
            {
                DespawnCount++;

                if (instance != null)
                    Object.DestroyImmediate(instance);
            }
        }

        private GameObject _runnerObject;
        private GameObject _prefab;
        private WaveRunner _runner;
        private FixedSpawnProvider _provider;
        private RecordingSpawner _spawner;

        private readonly List<ScriptableObject>
            _createdData =
                new List<ScriptableObject>();

        [SetUp]
        public void SetUp()
        {
            _runnerObject =
                new GameObject("Wave Runner Test");

            _prefab =
                new GameObject("Wave Prefab Template");

            _runner =
                _runnerObject.AddComponent<WaveRunner>();

            _provider = new FixedSpawnProvider();
            _spawner = new RecordingSpawner();
        }

        [TearDown]
        public void TearDown()
        {
            _runner?.Stop(true);

            foreach (
                GameObject instance
                in _spawner.Instances)
            {
                if (instance != null)
                    Object.DestroyImmediate(instance);
            }

            foreach (
                ScriptableObject data
                in _createdData)
            {
                if (data != null)
                    Object.DestroyImmediate(data);
            }

            if (_prefab != null)
                Object.DestroyImmediate(_prefab);

            if (_runnerObject != null)
                Object.DestroyImmediate(_runnerObject);

            _createdData.Clear();
        }

        [Test]
        public void FiniteWave_StartsSpawnsAndCompletes()
        {
            WaveSpawnGroupData group = CreateGroup(
                totalLimit: 1);

            WaveData wave = CreateWave(group);

            int started = 0;
            int completed = 0;
            int sequenceCompleted = 0;

            _runner.WaveStarted +=
                (_, _) => started++;

            _runner.WaveCompleted +=
                (_, _) => completed++;

            _runner.SequenceCompleted +=
                () => sequenceCompleted++;

            Configure(wave);

            Assert.IsTrue(_runner.StartSequence());

            _runner.Tick(0f);

            Assert.AreEqual(1, started);
            Assert.AreEqual(1, completed);
            Assert.AreEqual(1, sequenceCompleted);
            Assert.AreEqual(1, _spawner.SpawnedPoses.Count);
            Assert.AreEqual(
                WaveRunnerState.Completed,
                _runner.State);
        }

        [Test]
        public void InitialDelay_PreventsEarlySpawn()
        {
            WaveSpawnGroupData group = CreateGroup(
                totalLimit: 1);

            WaveData wave = CreateWave(
                group,
                initialDelay: 1f);

            Configure(wave);
            _runner.StartSequence();

            _runner.Tick(0.5f);

            Assert.AreEqual(
                0,
                _spawner.SpawnedPoses.Count);

            _runner.Tick(0.5f);

            Assert.AreEqual(
                1,
                _spawner.SpawnedPoses.Count);
        }

        [Test]
        public void ActiveLimit_BlocksAdditionalSpawns()
        {
            WaveSpawnGroupData group = CreateGroup(
                totalLimit: 2,
                instancesPerSpawn: 1,
                limitActive: true,
                maximumActive: 1);

            WaveData wave = CreateWave(
                group,
                waitForClear: true);

            Configure(wave);
            _runner.StartSequence();

            _runner.Tick(0f);
            _runner.Tick(0f);

            Assert.AreEqual(
                1,
                _spawner.SpawnedPoses.Count);

            WaveSpawnedObject trackedObject =
                _spawner.Instances[0]
                    .GetComponent<WaveSpawnedObject>();

            trackedObject.Release();

            _runner.Tick(0f);

            Assert.AreEqual(
                2,
                _spawner.SpawnedPoses.Count);
        }

        [Test]
        public void WaitForClear_DelaysCompletion()
        {
            WaveSpawnGroupData group = CreateGroup(
                totalLimit: 1);

            WaveData wave = CreateWave(
                group,
                waitForClear: true);

            int sequenceCompleted = 0;

            _runner.SequenceCompleted +=
                () => sequenceCompleted++;

            Configure(wave);
            _runner.StartSequence();
            _runner.Tick(0f);

            Assert.AreEqual(0, sequenceCompleted);
            Assert.AreEqual(
                WaveRunnerState.WaitingForCompletion,
                _runner.State);

            WaveSpawnedObject trackedObject =
                _spawner.Instances[0]
                    .GetComponent<WaveSpawnedObject>();

            trackedObject.Release();
            _runner.Tick(0f);

            Assert.AreEqual(1, sequenceCompleted);
        }

        [Test]
        public void TimedWave_CleansUpOnCompletion()
        {
            WaveSpawnGroupData group = CreateGroup(
                limitTotal: false);

            WaveData wave = CreateWave(
                group,
                usesDuration: true,
                duration: 1f,
                cleanup: true);

            Configure(wave);
            _runner.StartSequence();

            _runner.Tick(0f);
            _runner.Tick(1f);

            Assert.AreEqual(1, _spawner.DespawnCount);
            Assert.AreEqual(
                WaveRunnerState.Completed,
                _runner.State);
        }

        [Test]
        public void LinePattern_UsesConfiguredSpacing()
        {
            WaveSpawnGroupData group = CreateGroup(
                totalLimit: 3,
                instancesPerSpawn: 3,
                pattern:
                    WaveSpawnGroupData
                        .SpawnPattern.Line,
                spacing: 2f);

            WaveData wave = CreateWave(group);

            Configure(wave);
            _runner.StartSequence();
            _runner.Tick(0f);

            Assert.AreEqual(
                3,
                _spawner.SpawnedPoses.Count);

            Assert.AreEqual(
                new Vector3(0f, 0f, 0f),
                _spawner.SpawnedPoses[0].position);

            Assert.AreEqual(
                new Vector3(2f, 0f, 0f),
                _spawner.SpawnedPoses[1].position);

            Assert.AreEqual(
                new Vector3(4f, 0f, 0f),
                _spawner.SpawnedPoses[2].position);
        }

        private void Configure(WaveData wave)
        {
            _runner.Configure(
                new[] { wave },
                _provider,
                _spawner);
        }

        private WaveSpawnGroupData CreateGroup(
            bool limitTotal = true,
            int totalLimit = 1,
            int instancesPerSpawn = 1,
            bool limitActive = false,
            int maximumActive = 1,
            WaveSpawnGroupData.SpawnPattern pattern =
                WaveSpawnGroupData.SpawnPattern.Single,
            float spacing = 1f)
        {
            WaveSpawnGroupData group =
                ScriptableObject.CreateInstance<
                    WaveSpawnGroupData>();

            _createdData.Add(group);

            SerializedObject serializedGroup =
                new SerializedObject(group);

            serializedGroup
                .FindProperty("prefab")
                .objectReferenceValue = _prefab;

            serializedGroup
                .FindProperty("limitTotalSpawns")
                .boolValue = limitTotal;

            serializedGroup
                .FindProperty("totalSpawnLimit")
                .intValue = totalLimit;

            serializedGroup
                .FindProperty("instancesPerSpawn")
                .intValue = instancesPerSpawn;

            serializedGroup
                .FindProperty("limitActiveInstances")
                .boolValue = limitActive;

            serializedGroup
                .FindProperty("maximumActiveInstances")
                .intValue = maximumActive;

            serializedGroup
                .FindProperty("patternSpacing")
                .floatValue = spacing;

            serializedGroup
                .FindProperty("lineDirection")
                .vector3Value = Vector3.right;

            SerializedProperty patterns =
                serializedGroup.FindProperty(
                    "allowedPatterns");

            patterns.arraySize = 1;

            patterns
                .GetArrayElementAtIndex(0)
                .enumValueIndex = (int)pattern;

            serializedGroup
                .ApplyModifiedPropertiesWithoutUndo();

            return group;
        }

        private WaveData CreateWave(
            WaveSpawnGroupData group,
            float initialDelay = 0f,
            bool usesDuration = false,
            float duration = 0f,
            bool waitForClear = false,
            bool cleanup = false)
        {
            WaveData wave =
                ScriptableObject.CreateInstance<WaveData>();

            _createdData.Add(wave);

            SerializedObject serializedWave =
                new SerializedObject(wave);

            serializedWave
                .FindProperty("initialDelay")
                .floatValue = initialDelay;

            serializedWave
                .FindProperty("usesDuration")
                .boolValue = usesDuration;

            serializedWave
                .FindProperty("duration")
                .floatValue = duration;

            serializedWave
                .FindProperty(
                    "waitForActiveObjectsToClear")
                .boolValue = waitForClear;

            serializedWave
                .FindProperty(
                    "despawnActiveObjectsOnCompletion")
                .boolValue = cleanup;

            SerializedProperty groups =
                serializedWave.FindProperty(
                    "spawnGroups");

            groups.arraySize = 1;

            groups
                .GetArrayElementAtIndex(0)
                .objectReferenceValue = group;

            serializedWave
                .ApplyModifiedPropertiesWithoutUndo();

            return wave;
        }
    }
}