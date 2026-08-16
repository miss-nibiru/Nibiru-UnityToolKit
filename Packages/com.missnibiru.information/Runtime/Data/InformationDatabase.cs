using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Information.Data
{
    [CreateAssetMenu(
        fileName = "InformationDatabase",
        menuName = "Miss Nibiru/Information/Database")]
    public sealed class InformationDatabase : ScriptableObject
    {
        [SerializeField]
        private InformationEntry[] entries =
            Array.Empty<InformationEntry>();

        public IReadOnlyList<InformationEntry> Entries =>
            entries ?? Array.Empty<InformationEntry>();

        public void Configure(
            params InformationEntry[] availableEntries)
        {
            entries = availableEntries == null
                ? Array.Empty<InformationEntry>()
                : (InformationEntry[])availableEntries.Clone();
        }

        private void OnValidate()
        {
            if (entries == null)
            {
                entries = Array.Empty<InformationEntry>();
                return;
            }

            HashSet<string> registeredIds =
                new HashSet<string>(
                    StringComparer.Ordinal);

            foreach (InformationEntry entry in entries)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    Debug.LogError(
                        $"Information entry '{entry.name}' has no ID.",
                        this);

                    continue;
                }

                if (!registeredIds.Add(entry.Id))
                {
                    Debug.LogError(
                        $"Duplicate information ID: '{entry.Id}'.",
                        this);
                }
            }
        }
    }
}