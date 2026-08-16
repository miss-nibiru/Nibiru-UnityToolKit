using System;
using UnityEngine;

namespace MissNibiru.Waves.Layouts
{
    public enum SpawnableKind
    {
        Enemy,
        Hazard,
        Pickup,
        Other
    }

    [CreateAssetMenu(
        fileName = "SpawnableDefinition",
        menuName = "Miss Nibiru/Waves/Spawnable Definition")]
    public sealed class SpawnableDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName;

        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private SpawnableKind kind;

        [SerializeField]
        private string[] tags = Array.Empty<string>();

        [SerializeField, Tooltip("Cells this object occupies.")]
        private Vector2Int gridFootprint = Vector2Int.one;

        [SerializeField, Tooltip("Prefab anchor cell.")]
        private Vector2Int footprintPivot = Vector2Int.zero;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName;

        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public SpawnableKind Kind => kind;
        public string[] Tags => tags;
        public Vector2Int GridFootprint => gridFootprint;
        public Vector2Int FootprintPivot => footprintPivot;

        public Vector2Int SafeFootprint => new Vector2Int(
            Mathf.Max(1, gridFootprint.x),
            Mathf.Max(1, gridFootprint.y));

        public Vector2Int SafePivot
        {
            get
            {
                Vector2Int size = SafeFootprint;

                return new Vector2Int(
                    Mathf.Clamp(footprintPivot.x, 0, size.x - 1),
                    Mathf.Clamp(footprintPivot.y, 0, size.y - 1));
            }
        }

        public void Configure(
            string newDisplayName,
            GameObject newPrefab,
            SpawnableKind newKind,
            Vector2Int newFootprint,
            Vector2Int newPivot)
        {
            displayName = newDisplayName;
            prefab = newPrefab;
            kind = newKind;
            gridFootprint = newFootprint;
            footprintPivot = newPivot;
            ClampFootprint();
        }

        private void OnValidate()
        {
            ClampFootprint();
        }

        private void ClampFootprint()
        {
            gridFootprint.x = Mathf.Max(1, gridFootprint.x);
            gridFootprint.y = Mathf.Max(1, gridFootprint.y);
            footprintPivot = SafePivot;
        }
    }
}
