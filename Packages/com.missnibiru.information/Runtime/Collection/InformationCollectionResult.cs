using MissNibiru.Information.Data;

namespace MissNibiru.Information.Collection
{
    public readonly struct InformationCollectionResult
    {
        public InformationEntry Entry { get; }
        public string Id { get; }
        public bool IsFirstCollection { get; }

        public InformationCollectionResult(
            InformationEntry entry,
            bool isFirstCollection)
        {
            Entry = entry;

            Id = entry == null
                ? string.Empty
                : entry.Id;

            IsFirstCollection = isFirstCollection;
        }
    }
}