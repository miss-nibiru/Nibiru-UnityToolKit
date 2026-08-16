using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MissNibiru.Information.Collection
{
    public sealed class InMemoryInformationCollectionStore :
        IInformationCollectionStore
    {
        private readonly List<string> _orderedIds;
        private readonly HashSet<string> _idLookup;

        private readonly ReadOnlyCollection<string>
            _readOnlyIds;

        public IReadOnlyList<string> CollectedIds =>
            _readOnlyIds;

        public InMemoryInformationCollectionStore(
            IEnumerable<string> initialIds = null)
        {
            _orderedIds = new List<string>();

            _idLookup = new HashSet<string>(
                StringComparer.Ordinal);

            _readOnlyIds =
                _orderedIds.AsReadOnly();

            if (initialIds == null)
                return;

            foreach (string id in initialIds)
                TryAdd(id);
        }

        public bool Contains(string id)
        {
            string cleanId = CleanId(id);

            return cleanId.Length > 0 &&
                   _idLookup.Contains(cleanId);
        }

        public bool TryAdd(string id)
        {
            string cleanId = CleanId(id);

            if (cleanId.Length == 0)
                return false;

            if (!_idLookup.Add(cleanId))
                return false;

            _orderedIds.Add(cleanId);

            return true;
        }

        public void Clear()
        {
            _idLookup.Clear();
            _orderedIds.Clear();
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}