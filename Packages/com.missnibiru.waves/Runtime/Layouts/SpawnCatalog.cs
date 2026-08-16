using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Waves.Layouts
{
    [CreateAssetMenu(
        fileName = "SpawnCatalog",
        menuName = "Miss Nibiru/Waves/Spawn Catalog")]
    public sealed class SpawnCatalog : ScriptableObject
    {
        [SerializeField]
        private List<SpawnableDefinition> spawnables =
            new List<SpawnableDefinition>();

        [SerializeField]
        private List<SpawnFormationDefinition> formations =
            new List<SpawnFormationDefinition>();

        public IReadOnlyList<SpawnableDefinition> Spawnables =>
            spawnables;

        public IReadOnlyList<SpawnFormationDefinition> Formations =>
            formations;

        public List<SpawnableDefinition> MutableSpawnables =>
            spawnables;

        public List<SpawnFormationDefinition> MutableFormations =>
            formations;

        public void Configure(
            IEnumerable<SpawnableDefinition> newSpawnables,
            IEnumerable<SpawnFormationDefinition> newFormations = null)
        {
            spawnables = newSpawnables == null
                ? new List<SpawnableDefinition>()
                : new List<SpawnableDefinition>(newSpawnables);

            formations = newFormations == null
                ? new List<SpawnFormationDefinition>()
                : new List<SpawnFormationDefinition>(newFormations);
        }

        private void OnValidate()
        {
            if (spawnables == null)
                spawnables = new List<SpawnableDefinition>();

            if (formations == null)
                formations = new List<SpawnFormationDefinition>();
        }
    }
}
