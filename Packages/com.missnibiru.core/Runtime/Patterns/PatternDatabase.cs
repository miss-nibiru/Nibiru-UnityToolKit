using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Core.Patterns
{
    [CreateAssetMenu(
        fileName = "PatternDatabase",
        menuName = "Miss Nibiru/Patterns/Database")]
    public sealed class PatternDatabase : ScriptableObject
    {
        [SerializeField]
        private PatternDefinition fallbackPattern;

        [SerializeField]
        private PatternDefinition[] patterns =
            Array.Empty<PatternDefinition>();

        public PatternDefinition FallbackPattern =>
            fallbackPattern;

        public IReadOnlyList<PatternDefinition> Patterns =>
            patterns ?? Array.Empty<PatternDefinition>();

        public PatternResolver<
            PatternToken,
            PatternDefinition> CreateResolver()
        {
            PatternResolver<
                PatternToken,
                PatternDefinition> resolver =
                    new PatternResolver<
                        PatternToken,
                        PatternDefinition>(
                            fallbackPattern);

            HashSet<string> registeredIds =
                new HashSet<string>(
                    StringComparer.Ordinal);

            if (fallbackPattern != null)
            {
                RegisterId(
                    fallbackPattern,
                    registeredIds);
            }

            foreach (PatternDefinition pattern in Patterns)
            {
                if (pattern == null)
                    continue;

                RegisterId(pattern, registeredIds);
                ValidateTokens(pattern);

                resolver.Register(
                    pattern.Tokens,
                    pattern);
            }

            return resolver;
        }

        public void Configure(
            PatternDefinition fallback,
            params PatternDefinition[] availablePatterns)
        {
            fallbackPattern = fallback;

            patterns = availablePatterns == null
                ? Array.Empty<PatternDefinition>()
                : (PatternDefinition[])
                    availablePatterns.Clone();
        }

        private void OnValidate()
        {
            if (patterns == null)
                patterns =
                    Array.Empty<PatternDefinition>();

            try
            {
                CreateResolver();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    exception.Message,
                    this);
            }
        }

        private static void RegisterId(
            PatternDefinition pattern,
            HashSet<string> registeredIds)
        {
            if (string.IsNullOrWhiteSpace(pattern.Id))
            {
                throw new InvalidOperationException(
                    $"Pattern '{pattern.name}' requires an ID.");
            }

            if (!registeredIds.Add(pattern.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate pattern ID: '{pattern.Id}'.");
            }
        }

        private static void ValidateTokens(
            PatternDefinition pattern)
        {
            if (pattern.Tokens.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Pattern '{pattern.name}' has no tokens.");
            }

            foreach (PatternToken token in pattern.Tokens)
            {
                if (token == null)
                {
                    throw new InvalidOperationException(
                        $"Pattern '{pattern.name}' " +
                        "contains an empty token.");
                }
            }
        }
    }
}