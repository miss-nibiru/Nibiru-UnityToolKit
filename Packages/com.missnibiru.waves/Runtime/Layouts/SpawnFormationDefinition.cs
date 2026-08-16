using System;
using UnityEngine;

namespace MissNibiru.Waves.Layouts
{
    [CreateAssetMenu(
        fileName = "SpawnFormation",
        menuName = "Miss Nibiru/Waves/Spawn Formation")]
    public sealed class SpawnFormationDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName;

        [SerializeField, Tooltip("Offsets from anchor.")]
        private Vector2Int[] cellOffsets =
        {
            Vector2Int.zero
        };

        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName;

        public Vector2Int[] CellOffsets => cellOffsets;

        public void Configure(
            string newDisplayName,
            Vector2Int[] newCellOffsets)
        {
            displayName = newDisplayName;
            cellOffsets = newCellOffsets == null ||
                          newCellOffsets.Length == 0
                ? new[] { Vector2Int.zero }
                : newCellOffsets;
        }

        private void OnValidate()
        {
            if (cellOffsets == null || cellOffsets.Length == 0)
                cellOffsets = new[] { Vector2Int.zero };
        }
    }
}
