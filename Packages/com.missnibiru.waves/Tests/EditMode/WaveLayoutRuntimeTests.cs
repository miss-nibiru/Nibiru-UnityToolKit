using System.Collections.Generic;
using MissNibiru.Waves.Execution;
using MissNibiru.Waves.Layouts;
using MissNibiru.Waves.Planning;
using MissNibiru.Waves.Spawning;
using NUnit.Framework;
using UnityEngine;

namespace MissNibiru.Waves.Tests
{
    public sealed class WaveLayoutRuntimeTests
    {
        private sealed class RecordingSpawner : IWaveSpawner
        {
            public readonly List<GameObject> Instances =
                new List<GameObject>();

            public readonly List<Pose> Poses =
                new List<Pose>();

            public GameObject Spawn(
                GameObject prefab,
                Pose pose,
                Transform parent)
            {
                GameObject instance = new GameObject("Spawned");
                instance.transform.SetPositionAndRotation(
                    pose.position,
                    pose.rotation);
                Instances.Add(instance);
                Poses.Add(pose);
                return instance;
            }

            public void Despawn(GameObject instance)
            {
                if (instance != null)
                    Object.DestroyImmediate(instance);
            }
        }

        private readonly List<Object> _createdObjects =
            new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in _createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Footprint_UsesPivotAndRotation()
        {
            SpawnableDefinition spawnable = Spawnable(
                SpawnableKind.Enemy,
                new Vector2Int(2, 1),
                new Vector2Int(0, 0));

            IReadOnlyList<Vector2Int> cells =
                WaveLayoutGeometry.GetOccupiedCells(
                    spawnable,
                    new Vector2Int(5, 5),
                    WaveGridRotation.Degrees90,
                    false,
                    false);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    new Vector2Int(5, 5),
                    new Vector2Int(5, 6)
                },
                cells);
        }

        [Test]
        public void Compiler_BuildsSequentialFormation()
        {
            WaveLayoutData layout = Layout();
            SpawnableDefinition spawnable = Spawnable();
            SpawnFormationDefinition formation = Formation(
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.right * 2);

            WaveLayoutPlacement placement =
                new WaveLayoutPlacement
                {
                    Spawnable = spawnable,
                    Formation = formation,
                    Cell = new Vector2Int(2, 3),
                    SpawnDelay = 1f,
                    Sequential = true,
                    SequenceInterval = 0.5f
                };

            layout.Waves[0].Placements.Add(placement);

            List<WaveSpawnInstruction> instructions =
                WaveLayoutCompiler.CompileWave(layout, 0);

            Assert.AreEqual(3, instructions.Count);
            Assert.AreEqual(1f, instructions[0].SpawnTime);
            Assert.AreEqual(1.5f, instructions[1].SpawnTime);
            Assert.AreEqual(2f, instructions[2].SpawnTime);
            Assert.AreEqual(new Vector2Int(4, 3), instructions[2].Cell);
        }

        [Test]
        public void Compiler_RepeatsAtConfiguredTimes()
        {
            WaveLayoutData layout = Layout();
            WaveLayoutPlacement placement =
                new WaveLayoutPlacement
                {
                    Spawnable = Spawnable(),
                    Repetitions = 3,
                    RepeatInterval = 2f
                };

            layout.Waves[0].Placements.Add(placement);

            List<WaveSpawnInstruction> instructions =
                WaveLayoutCompiler.CompileWave(layout, 0);

            Assert.AreEqual(3, instructions.Count);
            Assert.AreEqual(0f, instructions[0].SpawnTime);
            Assert.AreEqual(2f, instructions[1].SpawnTime);
            Assert.AreEqual(4f, instructions[2].SpawnTime);
        }

        [Test]
        public void Calculator_ReportsCountsAndBudget()
        {
            WaveLayoutData layout = Layout();
            layout.ActiveEnemyBudget = 1;

            WaveLayoutPlacement placement =
                new WaveLayoutPlacement
                {
                    Spawnable = Spawnable(),
                    Formation = Formation(
                        Vector2Int.zero,
                        Vector2Int.right)
                };

            layout.Waves[0].Placements.Add(placement);

            WaveLayoutWaveStatistics statistics =
                WaveLayoutCalculator.CalculateWave(layout, 0);

            Assert.AreEqual(2, statistics.TotalSpawns);
            Assert.AreEqual(2, statistics.EnemySpawns);
            Assert.AreEqual(2, statistics.MaximumSimultaneous);
            Assert.IsTrue(statistics.ExceedsBudget);
        }

        [Test]
        public void Runner_SpawnsAtDesignedWorldPosition()
        {
            WaveLayoutData layout = Layout();
            layout.ConfigureGrid(20, 12, WaveGridPlane.XY, 2f);
            layout.Waves[0].WaitForActiveObjectsToClear = false;

            WaveLayoutPlacement placement =
                new WaveLayoutPlacement
                {
                    Spawnable = Spawnable(),
                    Cell = new Vector2Int(2, 3)
                };

            layout.Waves[0].Placements.Add(placement);

            GameObject originObject = new GameObject("Origin");
            originObject.transform.position = new Vector3(10f, 5f, 0f);
            _createdObjects.Add(originObject);

            GameObject runnerObject = new GameObject("Runner");
            WaveRunner runner = runnerObject.AddComponent<WaveRunner>();
            _createdObjects.Add(runnerObject);

            RecordingSpawner spawner = new RecordingSpawner();
            runner.ConfigureLayout(layout, originObject.transform, spawner);

            Assert.IsTrue(runner.StartSequence());
            runner.Tick(0f);

            Assert.AreEqual(1, spawner.Poses.Count);
            Assert.AreEqual(
                new Vector3(15f, 12f, 0f),
                spawner.Poses[0].position);

            foreach (GameObject instance in spawner.Instances)
                _createdObjects.Add(instance);
        }

        private WaveLayoutData Layout()
        {
            WaveLayoutData layout =
                ScriptableObject.CreateInstance<WaveLayoutData>();
            _createdObjects.Add(layout);
            return layout;
        }

        private SpawnableDefinition Spawnable(
            SpawnableKind kind = SpawnableKind.Enemy,
            Vector2Int? footprint = null,
            Vector2Int? pivot = null)
        {
            GameObject prefab = new GameObject("Prefab");
            _createdObjects.Add(prefab);

            SpawnableDefinition spawnable =
                ScriptableObject.CreateInstance<
                    SpawnableDefinition>();

            spawnable.Configure(
                "Test Spawnable",
                prefab,
                kind,
                footprint ?? Vector2Int.one,
                pivot ?? Vector2Int.zero);

            _createdObjects.Add(spawnable);
            return spawnable;
        }

        private SpawnFormationDefinition Formation(
            params Vector2Int[] offsets)
        {
            SpawnFormationDefinition formation =
                ScriptableObject.CreateInstance<
                    SpawnFormationDefinition>();

            formation.Configure("Test Formation", offsets);
            _createdObjects.Add(formation);
            return formation;
        }
    }
}
