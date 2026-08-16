using UnityEngine;

namespace MissNibiru.Information.Data
{
    [CreateAssetMenu(
        fileName = "InformationType",
        menuName = "Miss Nibiru/Information/Type")]
    public sealed class InformationType : ScriptableObject
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
            string typeDisplayName,
            Sprite typeIcon = null)
        {
            id = CleanId(stableId);
            displayName = typeDisplayName;
            icon = typeIcon;
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