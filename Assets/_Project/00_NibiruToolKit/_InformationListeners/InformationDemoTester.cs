using MissNibiru.Information.Collection;
using MissNibiru.Information.Unity;
using UnityEngine;

namespace MissNibiru.Information.Demo
{
    public sealed class InformationDemoTester :
        MonoBehaviour
    {
        [SerializeField]
        private InformationCollectionComponent collection;

        [SerializeField]
        private InformationSource source;

        private void OnEnable()
        {
            if (collection == null)
                return;

            collection.CollectionReported +=
                HandleCollectionReported;

            collection.CollectionCleared +=
                HandleCollectionCleared;
        }

        private void OnDisable()
        {
            if (collection == null)
                return;

            collection.CollectionReported -=
                HandleCollectionReported;

            collection.CollectionCleared -=
                HandleCollectionCleared;
        }

        [ContextMenu("Collect Information")]
        public void CollectInformation()
        {
            if (source == null)
            {
                Debug.LogError(
                    "The demo source is missing.",
                    this);

                return;
            }

            source.Collect();
        }

        [ContextMenu("Clear Information")]
        public void ClearInformation()
        {
            if (collection == null)
                return;

            collection.Clear();
        }

        private void HandleCollectionReported(
            InformationCollectionResult result)
        {
            Debug.Log(
                $"Collected: {result.Entry.DisplayName} | " +
                $"First: {result.IsFirstCollection} | " +
                $"Total: {collection.CollectedEntries.Count}",
                this);
        }

        private void HandleCollectionCleared()
        {
            Debug.Log(
                "Information collection cleared.",
                this);
        }
    }
}