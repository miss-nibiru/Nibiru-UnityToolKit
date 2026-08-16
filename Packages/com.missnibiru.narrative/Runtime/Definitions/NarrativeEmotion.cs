using UnityEngine;

namespace MissNibiru.Narrative
{
    [CreateAssetMenu(
        fileName = "NarrativeEmotion",
        menuName = "Miss Nibiru/Narrative/Emotion")]
    public sealed class NarrativeEmotion : ScriptableObject
    {
        [SerializeField]
        private string id = "emotion";

        [SerializeField]
        private string displayName = "Emotion";

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private Color previewTint = Color.white;

        public string Id => CleanId(id);
        public string DisplayName => displayName ?? string.Empty;
        public Sprite Icon => icon;
        public Color PreviewTint => previewTint;

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
