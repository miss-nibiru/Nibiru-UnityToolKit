using System;
using UnityEngine;

namespace MissNibiru.Narrative
{
    [CreateAssetMenu(
        fileName = "NarrativeEvent",
        menuName = "Miss Nibiru/Narrative/Gameplay Event")]
    public sealed class NarrativeEvent : ScriptableObject
    {
        [SerializeField]
        private string id = "event";

        [SerializeField]
        private string displayName = "Gameplay Event";

        [SerializeField, TextArea(2, 4)]
        private string description = string.Empty;

        public string Id => CleanId(id);
        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;

        public event Action<string> Raised;

        public void Raise(string payload = "")
        {
            Raised?.Invoke(payload ?? string.Empty);
        }

        public void Configure(string stableId, string visibleName)
        {
            id = CleanId(stableId);
            displayName = visibleName ?? string.Empty;
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
