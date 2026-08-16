using System.Collections.Generic;

namespace MissNibiru.Information.Collection
{
    public interface IInformationCollectionStore
    {
        IReadOnlyList<string> CollectedIds { get; }

        bool Contains(string id);

        bool TryAdd(string id);

        void Clear();
    }
}