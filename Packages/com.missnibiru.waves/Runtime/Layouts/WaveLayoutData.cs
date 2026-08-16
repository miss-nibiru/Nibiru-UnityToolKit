using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Waves.Layouts
{
    public enum WaveGridPlane
    {
        XY,
        XZ
    }

    public enum WaveGridRotation
    {
        Degrees0,
        Degrees90,
        Degrees180,
        Degrees270
    }

    [Serializable]
    public sealed class WaveLayoutPlacement
    {
        [SerializeField]
        private string id = Guid.NewGuid().ToString("N");

        [SerializeField]
        private bool enabled = true;

        [SerializeField]
        private SpawnableDefinition spawnable;

        [SerializeField]
        private SpawnFormationDefinition formation;

        [SerializeField]
        private Vector2Int cell;

        [SerializeField]
        private WaveGridRotation rotation;

        [SerializeField]
        private bool flipHorizontal;

        [SerializeField]
        private bool flipVertical;

        [SerializeField, Min(0f)]
        private float spawnDelay;

        [SerializeField]
        private bool sequential;

        [SerializeField, Min(0f)]
        private float sequenceInterval = 0.25f;

        [SerializeField, Min(1)]
        private int repetitions = 1;

        [SerializeField, Min(0f)]
        private float repeatInterval = 1f;

        public string Id
        {
            get
            {
                if (string.IsNullOrWhiteSpace(id))
                    id = Guid.NewGuid().ToString("N");

                return id;
            }
            set => id = value;
        }

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        public SpawnableDefinition Spawnable
        {
            get => spawnable;
            set => spawnable = value;
        }

        public SpawnFormationDefinition Formation
        {
            get => formation;
            set => formation = value;
        }

        public Vector2Int Cell
        {
            get => cell;
            set => cell = value;
        }

        public WaveGridRotation Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        public bool FlipHorizontal
        {
            get => flipHorizontal;
            set => flipHorizontal = value;
        }

        public bool FlipVertical
        {
            get => flipVertical;
            set => flipVertical = value;
        }

        public float SpawnDelay
        {
            get => spawnDelay;
            set => spawnDelay = value;
        }

        public bool Sequential
        {
            get => sequential;
            set => sequential = value;
        }

        public float SequenceInterval
        {
            get => sequenceInterval;
            set => sequenceInterval = value;
        }

        public int Repetitions
        {
            get => repetitions;
            set => repetitions = value;
        }

        public float RepeatInterval
        {
            get => repeatInterval;
            set => repeatInterval = value;
        }

        public WaveLayoutPlacement Duplicate()
        {
            return new WaveLayoutPlacement
            {
                id = Guid.NewGuid().ToString("N"),
                enabled = enabled,
                spawnable = spawnable,
                formation = formation,
                cell = cell,
                rotation = rotation,
                flipHorizontal = flipHorizontal,
                flipVertical = flipVertical,
                spawnDelay = spawnDelay,
                sequential = sequential,
                sequenceInterval = sequenceInterval,
                repetitions = repetitions,
                repeatInterval = repeatInterval
            };
        }
    }

    [Serializable]
    public sealed class WaveLayoutWave
    {
        [SerializeField]
        private string waveName = "Wave 1";

        [SerializeField, Min(0f)]
        private float initialDelay;

        [SerializeField]
        private bool usesDuration;

        [SerializeField, Min(0f)]
        private float duration;

        [SerializeField]
        private bool waitForActiveObjectsToClear = true;

        [SerializeField]
        private bool autoProgress = true;

        [SerializeField]
        private bool despawnActiveObjectsOnCompletion;

        [SerializeField]
        private List<WaveLayoutPlacement> placements =
            new List<WaveLayoutPlacement>();

        public string WaveName
        {
            get => waveName;
            set => waveName = value;
        }

        public float InitialDelay
        {
            get => initialDelay;
            set => initialDelay = value;
        }

        public bool UsesDuration
        {
            get => usesDuration;
            set => usesDuration = value;
        }

        public float Duration
        {
            get => duration;
            set => duration = value;
        }

        public bool WaitForActiveObjectsToClear
        {
            get => waitForActiveObjectsToClear;
            set => waitForActiveObjectsToClear = value;
        }

        public bool AutoProgress
        {
            get => autoProgress;
            set => autoProgress = value;
        }

        public bool DespawnActiveObjectsOnCompletion
        {
            get => despawnActiveObjectsOnCompletion;
            set => despawnActiveObjectsOnCompletion = value;
        }

        public List<WaveLayoutPlacement> Placements => placements;

        public WaveLayoutWave Duplicate(string duplicateName)
        {
            WaveLayoutWave copy = new WaveLayoutWave
            {
                waveName = duplicateName,
                initialDelay = initialDelay,
                usesDuration = usesDuration,
                duration = duration,
                waitForActiveObjectsToClear =
                    waitForActiveObjectsToClear,
                autoProgress = autoProgress,
                despawnActiveObjectsOnCompletion =
                    despawnActiveObjectsOnCompletion
            };

            foreach (WaveLayoutPlacement placement in placements)
            {
                if (placement != null)
                    copy.placements.Add(placement.Duplicate());
            }

            return copy;
        }
    }

    [CreateAssetMenu(
        fileName = "WaveLayoutData",
        menuName = "Miss Nibiru/Waves/Wave Layout")]
    public sealed class WaveLayoutData : ScriptableObject
    {
        public const int MaximumGridSize = 100;

        [SerializeField]
        private SpawnCatalog catalog;

        [SerializeField, Range(1, MaximumGridSize)]
        private int columns = 20;

        [SerializeField, Range(1, MaximumGridSize)]
        private int rows = 12;

        [SerializeField]
        private WaveGridPlane gridPlane = WaveGridPlane.XY;

        [SerializeField, Min(0.01f)]
        private float cellSize = 1f;

        [SerializeField, Min(1)]
        private int activeEnemyBudget = 20;

        [SerializeField]
        private List<WaveLayoutWave> waves =
            new List<WaveLayoutWave>
            {
                new WaveLayoutWave()
            };

        public SpawnCatalog Catalog
        {
            get => catalog;
            set => catalog = value;
        }

        public int Columns
        {
            get => columns;
            set => columns = value;
        }

        public int Rows
        {
            get => rows;
            set => rows = value;
        }

        public WaveGridPlane GridPlane
        {
            get => gridPlane;
            set => gridPlane = value;
        }

        public float CellSize
        {
            get => cellSize;
            set => cellSize = value;
        }

        public int ActiveEnemyBudget
        {
            get => activeEnemyBudget;
            set => activeEnemyBudget = value;
        }

        public List<WaveLayoutWave> Waves => waves;

        public void ConfigureGrid(
            int newColumns,
            int newRows,
            WaveGridPlane newPlane,
            float newCellSize)
        {
            columns = newColumns;
            rows = newRows;
            gridPlane = newPlane;
            cellSize = newCellSize;
            ClampSettings();
        }

        private void OnValidate()
        {
            ClampSettings();
        }

        private void ClampSettings()
        {
            columns = Mathf.Clamp(columns, 1, MaximumGridSize);
            rows = Mathf.Clamp(rows, 1, MaximumGridSize);
            cellSize = Mathf.Max(0.01f, cellSize);
            activeEnemyBudget = Mathf.Max(1, activeEnemyBudget);

            if (waves == null)
                waves = new List<WaveLayoutWave>();
        }
    }
}
