using System;
using MissNibiru.Information.Collection;
using MissNibiru.Information.Data;
using UnityEngine;
using UnityEngine.Events;

namespace MissNibiru.Information.Unity
{
    public sealed class InformationSource :
        MonoBehaviour
    {
        [SerializeField]
        private InformationEntry entry;

        [SerializeField]
        private InformationCollectionComponent collection;

        [Header("Collection Events")]

        [SerializeField]
        private UnityEvent onCollected =
            new UnityEvent();

        [SerializeField]
        private UnityEvent onFirstCollected =
            new UnityEvent();

        [SerializeField]
        private UnityEvent onRepeatedCollection =
            new UnityEvent();

        public event Action<InformationCollectionResult>
            CollectionSucceeded;

        public InformationEntry Entry => entry;

        public InformationCollectionComponent Collection =>
            collection;

        public void Configure(
            InformationEntry informationEntry,
            InformationCollectionComponent
                collectionComponent)
        {
            entry = informationEntry;
            collection = collectionComponent;
        }

        public void Collect()
        {
            TryCollect(
                out InformationCollectionResult _);
        }

        public bool TryCollect(
            out InformationCollectionResult result)
        {
            result =
                default(InformationCollectionResult);

            if (entry == null ||
                collection == null)
            {
                return false;
            }

            if (!collection.TryCollect(
                    entry,
                    out result))
            {
                return false;
            }

            onCollected?.Invoke();

            if (result.IsFirstCollection)
                onFirstCollected?.Invoke();
            else
                onRepeatedCollection?.Invoke();

            CollectionSucceeded?.Invoke(result);

            return true;
        }
    }
}