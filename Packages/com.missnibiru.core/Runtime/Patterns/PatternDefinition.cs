using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Core.Patterns
{
    [CreateAssetMenu(
        fileName = "PatternDefinition",
        menuName = "Miss Nibiru/Patterns/Pattern")]
    public sealed class PatternDefinition : ScriptableObject
    {
        [Header("Identity")]

        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [Header("Sequence")]

        [SerializeField]
        private PatternToken[] tokens =
            Array.Empty<PatternToken>();

        [Header("Optional Result")]

        [Tooltip(
            "Optional asset produced by this pattern, " +
            "such as an attack or puzzle result.")]
        [SerializeField]
        private UnityEngine.Object resultAsset;

        public string Id =>
            string.IsNullOrWhiteSpace(id)
                ? string.Empty
                : id.Trim();

        public string DisplayName => displayName;

        public IReadOnlyList<PatternToken> Tokens =>
            tokens ?? Array.Empty<PatternToken>();

        public UnityEngine.Object ResultAsset =>
            resultAsset;

        public void Configure(
            string stableId,
            string patternDisplayName,
            PatternToken[] patternTokens,
            UnityEngine.Object associatedResult = null)
        {
            id = CleanId(stableId);
            displayName = patternDisplayName;

            tokens = patternTokens == null
                ? Array.Empty<PatternToken>()
                : (PatternToken[])patternTokens.Clone();

            resultAsset = associatedResult;
        }

        private void OnValidate()
        {
            id = CleanId(id);

            if (tokens == null)
                tokens = Array.Empty<PatternToken>();
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}