using UnityEngine;

namespace MissNibiru.Information.Data
{
    [CreateAssetMenu(
        fileName = "InformationCategory",
        menuName = "Miss Nibiru/Information/Category")]
    public sealed class InformationCategory : ScriptableObject
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
            string categoryDisplayName,
            Sprite categoryIcon = null)
        {
            id = CleanId(stableId);
            displayName = categoryDisplayName;
            icon = categoryIcon;
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