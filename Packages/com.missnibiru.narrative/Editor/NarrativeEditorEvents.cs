using System;

namespace MissNibiru.Narrative.Editor
{
    internal static class NarrativeEditorEvents
    {
        public static event Action GraphRefreshRequested;

        public static void RequestGraphRefresh()
        {
            GraphRefreshRequested?.Invoke();
        }
    }
}
