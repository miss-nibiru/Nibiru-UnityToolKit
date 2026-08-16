using System;
using System.Collections.Generic;
using MissNibiru.Information.Collection;
using MissNibiru.Information.Data;
using UnityEngine;

using InformationCollectionService =
    MissNibiru.Information.Collection.InformationCollection;

namespace MissNibiru.Information.Unity
{
    public sealed class InformationCollectionComponent :
        MonoBehaviour
    {
        [SerializeField]
        private InformationDatabase database;

        private InformationCollectionService _service;

        public event Action<InformationCollectionResult>
            FirstCollected;

        public event Action<InformationCollectionResult>
            CollectionReported;

        public event Action CollectionCleared;

        public InformationDatabase Database =>
            database;

        public InformationCollectionService Service
        {
            get
            {
                EnsureInitialized();
                return _service;
            }
        }

        public IReadOnlyList<InformationEntry>
            CollectedEntries =>
                Service.CollectedEntries;

        public IReadOnlyList<InformationEntry>
            RegisteredEntries =>
                Service.RegisteredEntries;

        private void Awake()
        {
            Initialize(null);
        }

        private void OnDestroy()
        {
            UnsubscribeFromService();
        }

        public void Configure(
            InformationDatabase configuredDatabase,
            IInformationCollectionStore store = null)
        {
            database = configuredDatabase;
            Initialize(store);
        }

        public void Register(
            InformationEntry entry)
        {
            Service.Register(entry);
        }

        public bool TryCollect(
            InformationEntry entry,
            out InformationCollectionResult result)
        {
            return Service.TryCollect(
                entry,
                out result);
        }

        public bool Contains(string id)
        {
            return Service.Contains(id);
        }

        public bool TryGetRegisteredEntry(
            string id,
            out InformationEntry entry)
        {
            return Service.TryGetRegisteredEntry(
                id,
                out entry);
        }

        public IReadOnlyList<InformationEntry>
            GetCollectedByType(
                InformationType type)
        {
            return Service.GetCollectedByType(type);
        }

        public IReadOnlyList<InformationEntry>
            GetCollectedByCategory(
                InformationCategory category)
        {
            return Service.GetCollectedByCategory(
                category);
        }

        public void Clear()
        {
            Service.Clear();
        }

        private void EnsureInitialized()
        {
            if (_service == null)
                Initialize(null);
        }

        private void Initialize(
            IInformationCollectionStore store)
        {
            UnsubscribeFromService();

            _service =
                new InformationCollectionService(
                    store ??
                    new InMemoryInformationCollectionStore());

            if (database != null)
                _service.RegisterRange(database.Entries);

            SubscribeToService();
        }

        private void SubscribeToService()
        {
            if (_service == null)
                return;

            _service.FirstCollected +=
                HandleFirstCollected;

            _service.CollectionReported +=
                HandleCollectionReported;

            _service.CollectionCleared +=
                HandleCollectionCleared;
        }

        private void UnsubscribeFromService()
        {
            if (_service == null)
                return;

            _service.FirstCollected -=
                HandleFirstCollected;

            _service.CollectionReported -=
                HandleCollectionReported;

            _service.CollectionCleared -=
                HandleCollectionCleared;
        }

        private void HandleFirstCollected(
            InformationCollectionResult result)
        {
            FirstCollected?.Invoke(result);
        }

        private void HandleCollectionReported(
            InformationCollectionResult result)
        {
            CollectionReported?.Invoke(result);
        }

        private void HandleCollectionCleared()
        {
            CollectionCleared?.Invoke();
        }
    }
}