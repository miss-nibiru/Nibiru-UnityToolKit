using UnityEngine;

namespace MissNibiru.Core.Patterns
{
    [CreateAssetMenu(
        fileName = "PatternToken",
        menuName = "Miss Nibiru/Patterns/Token")]
    public sealed class PatternToken : ScriptableObject
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite icon;

        public string Id =>
            string.IsNullOrWhiteSpace(id)
                ? string.Empty
                : id.Trim();

        public string DisplayName => displayName;
        public Sprite Icon => icon;

        public void Configure(
            string stableId,
            string tokenDisplayName,
            Sprite tokenIcon = null)
        {
            id = CleanId(stableId);
            displayName = tokenDisplayName;
            icon = tokenIcon;
        }

        private void OnValidate()
        {
            id = CleanId(id);
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}