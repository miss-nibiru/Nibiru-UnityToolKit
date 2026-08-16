using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Information.Data
{
    [CreateAssetMenu(
        fileName = "InformationEntry",
        menuName = "Miss Nibiru/Information/Entry")]
    public sealed class InformationEntry : ScriptableObject
    {
        [Header("Identity")]

        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [Header("Classification")]

        [SerializeField]
        private InformationType informationType;

        [SerializeField]
        private InformationCategory category;

        [Header("Presentation")]

        [SerializeField, TextArea(2, 5)]
        private string summary;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private Sprite image;

        [SerializeField]
        private InformationPage[] pages =
            Array.Empty<InformationPage>();

        [Header("Optional Related Asset")]

        [Tooltip(
            "Optional asset (weapon, item or attack)")]
        [SerializeField]
        private UnityEngine.Object relatedAsset;

        public string Id =>
            string.IsNullOrWhiteSpace(id)
                ? string.Empty
                : id.Trim();

        public string DisplayName => displayName;
        public InformationType Type => informationType;
        public InformationCategory Category => category;
        public string Summary => summary;
        public Sprite Icon => icon;
        public Sprite Image => image;

        public IReadOnlyList<InformationPage> Pages =>
            pages ?? Array.Empty<InformationPage>();

        public UnityEngine.Object RelatedAsset =>
            relatedAsset;

        public void Configure(
            string stableId,
            string entryDisplayName,
            string entrySummary,
            InformationType entryType = null,
            InformationCategory entryCategory = null,
            Sprite entryIcon = null,
            Sprite entryImage = null,
            InformationPage[] entryPages = null,
            UnityEngine.Object representedAsset = null)
        {
            id = CleanId(stableId);
            displayName = entryDisplayName;
            summary = entrySummary;
            informationType = entryType;
            category = entryCategory;
            icon = entryIcon;
            image = entryImage;
            relatedAsset = representedAsset;

            pages = entryPages == null
                ? Array.Empty<InformationPage>()
                : (InformationPage[])entryPages.Clone();
        }

        private void OnValidate()
        {
            id = CleanId(id);

            if (pages == null)
                pages = Array.Empty<InformationPage>();
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}