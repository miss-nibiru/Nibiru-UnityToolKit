using UnityEngine;

namespace MissNibiru.Narrative
{
    [CreateAssetMenu(
        fileName = "NarrativeFlag",
        menuName = "Miss Nibiru/Narrative/Flag")]
    public sealed class NarrativeFlag : ScriptableObject
    {
        [SerializeField]
        private string id = "flag";

        [SerializeField]
        private string displayName = "Flag";

        [SerializeField]
        private bool defaultValue;

        public string Id => CleanId(id);
        public string DisplayName => displayName ?? string.Empty;
        public bool DefaultValue => defaultValue;

        public void Configure(
            string stableId,
            string visibleName,
            bool initialValue = false)
        {
            id = CleanId(stableId);
            displayName = visibleName ?? string.Empty;
            defaultValue = initialValue;
        }

        private void OnValidate()
        {
            id = CleanId(id);
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }
}
