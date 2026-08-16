using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MissNibiru.Information.Data;

namespace MissNibiru.Information.Collection
{
    public sealed class InformationCollection
    {
        private readonly IInformationCollectionStore
            _store;

        private readonly Dictionary<
            string,
            InformationEntry> _registry;

        private readonly List<InformationEntry>
            _registeredEntries;

        private readonly ReadOnlyCollection<
            InformationEntry> _readOnlyRegisteredEntries;

        public event Action<InformationCollectionResult>
            FirstCollected;

        public event Action<InformationCollectionResult>
            CollectionReported;

        public event Action CollectionCleared;

        public IReadOnlyList<InformationEntry>
            RegisteredEntries =>
                _readOnlyRegisteredEntries;

        public IReadOnlyList<InformationEntry>
            CollectedEntries =>
                ResolveCollectedEntries();

        public InformationCollection(
            IInformationCollectionStore store)
        {
            _store = store ??
                throw new ArgumentNullException(
                    nameof(store));

            _registry =
                new Dictionary<string, InformationEntry>(
                    StringComparer.Ordinal);

            _registeredEntries =
                new List<InformationEntry>();

            _readOnlyRegisteredEntries =
                _registeredEntries.AsReadOnly();
        }

        public void Register(
            InformationEntry entry)
        {
            string id = ValidateEntry(entry);

            if (_registry.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    $"An information entry with ID " +
                    $"'{id}' is already registered.");
            }

            AddToRegistry(entry, id);
        }

        public void RegisterRange(
            IEnumerable<InformationEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(
                    nameof(entries));
            }

            List<InformationEntry> pendingEntries =
                new List<InformationEntry>();

            HashSet<string> pendingIds =
                new HashSet<string>(
                    StringComparer.Ordinal);

            foreach (InformationEntry entry in entries)
            {
                string id = ValidateEntry(entry);

                if (_registry.ContainsKey(id) ||
                    !pendingIds.Add(id))
                {
                    throw new InvalidOperationException(
                        $"An information entry with ID " +
                        $"'{id}' is already registered.");
                }

                pendingEntries.Add(entry);
            }

            foreach (InformationEntry entry
                     in pendingEntries)
            {
                AddToRegistry(entry, entry.Id);
            }
        }

        public bool TryCollect(
            InformationEntry entry,
            out InformationCollectionResult result)
        {
            result =
                default(InformationCollectionResult);

            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.Id))
            {
                return false;
            }

            if (!_registry.TryGetValue(
                    entry.Id,
                    out InformationEntry registeredEntry))
            {
                return false;
            }

            bool isFirstCollection =
                _store.TryAdd(registeredEntry.Id);

            result =
                new InformationCollectionResult(
                    registeredEntry,
                    isFirstCollection);

            if (isFirstCollection)
                FirstCollected?.Invoke(result);

            CollectionReported?.Invoke(result);

            return true;
        }

        public bool Contains(string id)
        {
            return _store.Contains(id);
        }

        public bool TryGetRegisteredEntry(
            string id,
            out InformationEntry entry)
        {
            string cleanId = CleanId(id);

            if (cleanId.Length == 0)
            {
                entry = null;
                return false;
            }

            return _registry.TryGetValue(
                cleanId,
                out entry);
        }

        public IReadOnlyList<InformationEntry>
            GetCollectedByType(
                InformationType type)
        {
            if (type == null)
                return Array.Empty<InformationEntry>();

            List<InformationEntry> matches =
                new List<InformationEntry>();

            foreach (InformationEntry entry
                     in CollectedEntries)
            {
                if (entry.Type == type)
                    matches.Add(entry);
            }

            return matches.AsReadOnly();
        }

        public IReadOnlyList<InformationEntry>
            GetCollectedByCategory(
                InformationCategory category)
        {
            if (category == null)
                return Array.Empty<InformationEntry>();

            List<InformationEntry> matches =
                new List<InformationEntry>();

            foreach (InformationEntry entry
                     in CollectedEntries)
            {
                if (entry.Category == category)
                    matches.Add(entry);
            }

            return matches.AsReadOnly();
        }

        public void Clear()
        {
            _store.Clear();
            CollectionCleared?.Invoke();
        }

        private IReadOnlyList<InformationEntry>
            ResolveCollectedEntries()
        {
            List<InformationEntry> resolved =
                new List<InformationEntry>();

            foreach (string id
                     in _store.CollectedIds)
            {
                if (_registry.TryGetValue(
                        id,
                        out InformationEntry entry))
                {
                    resolved.Add(entry);
                }
            }

            return resolved.AsReadOnly();
        }

        private void AddToRegistry(
            InformationEntry entry,
            string id)
        {
            _registry.Add(id, entry);
            _registeredEntries.Add(entry);
        }

        private static string ValidateEntry(
            InformationEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(
                    nameof(entry));
            }

            string id = CleanId(entry.Id);

            if (id.Length == 0)
            {
                throw new ArgumentException(
                    "Information entries require " +
                    "a stable, non-blank ID.",
                    nameof(entry));
            }

            return id;
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}