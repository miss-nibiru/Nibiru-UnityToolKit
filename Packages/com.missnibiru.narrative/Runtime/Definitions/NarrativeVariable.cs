using UnityEngine;

namespace MissNibiru.Narrative
{
    [CreateAssetMenu(
        fileName = "NarrativeVariable",
        menuName = "Miss Nibiru/Narrative/Variable")]
    public sealed class NarrativeVariable : ScriptableObject
    {
        [SerializeField]
        private string id = "variable";

        [SerializeField]
        private string displayName = "Variable";

        [SerializeField]
        private NarrativeVariableType variableType;

        [SerializeField]
        private bool defaultBoolean;

        [SerializeField]
        private int defaultInteger;

        [SerializeField]
        private float defaultFloat;

        [SerializeField]
        private string defaultString = string.Empty;

        public string Id => CleanId(id);
        public string DisplayName => displayName ?? string.Empty;
        public NarrativeVariableType VariableType => variableType;
        public bool DefaultBoolean => defaultBoolean;
        public int DefaultInteger => defaultInteger;
        public float DefaultFloat => defaultFloat;
        public string DefaultString => defaultString ?? string.Empty;

        public void Configure(
            string stableId,
            string visibleName,
            NarrativeVariableType type)
        {
            id = CleanId(stableId);
            displayName = visibleName ?? string.Empty;
            variableType = type;
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
