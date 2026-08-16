using System;
using MissNibiru.Core.Patterns;
using UnityEngine;

namespace MissNibiru.Toolkit.Examples
{
    public sealed class PatternResolverExample :
        MonoBehaviour
    {
        [SerializeField]
        private PatternDatabase database;

        [SerializeField]
        private PatternToken[] submittedTokens =
            Array.Empty<PatternToken>();

        private void Start()
        {
            if (database == null)
            {
                Debug.LogError(
                    "Assign a Pattern Database.",
                    this);

                return;
            }

            PatternResolver<
                PatternToken,
                PatternDefinition> resolver =
                database.CreateResolver();

            PatternDefinition result =
                resolver.Resolve(submittedTokens);

            if (result == null)
            {
                Debug.Log(
                    "No pattern or fallback was found.",
                    this);

                return;
            }

            Debug.Log(
                $"Resolved pattern: {result.DisplayName}",
                this);

            if (result.ResultAsset != null)
            {
                Debug.Log(
                    $"Result asset: {result.ResultAsset.name}",
                    this);
            }
        }
    }
}