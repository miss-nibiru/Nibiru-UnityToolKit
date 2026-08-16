using System;
using System.Collections.Generic;
using MissNibiru.Core.Patterns;
using NUnit.Framework;
using UnityEngine;

namespace MissNibiru.Core.Tests.Patterns
{
    public sealed class PatternDatabaseTests
    {
        private readonly List<UnityEngine.Object>
            _createdAssets =
                new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object asset
                     in _createdAssets)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }

            _createdAssets.Clear();
        }

        [Test]
        public void CreateResolver_ResolvesPatternAssets()
        {
            PatternToken up =
                CreateToken("up", "Up");

            PatternToken confirm =
                CreateToken("confirm", "Confirm");

            PatternDefinition fallback =
                CreatePattern(
                    "fallback",
                    "Fallback");

            PatternDefinition uppercut =
                CreatePattern(
                    "uppercut",
                    "Uppercut",
                    up,
                    confirm);

            PatternDatabase database =
                CreateAsset<PatternDatabase>();

            database.Configure(
                fallback,
                uppercut);

            PatternResolver<
                PatternToken,
                PatternDefinition> resolver =
                    database.CreateResolver();

            PatternDefinition result =
                resolver.Resolve(
                    new[] { up, confirm });

            Assert.AreSame(uppercut, result);
        }

        [Test]
        public void CreateResolver_UsesFallbackAsset()
        {
            PatternToken down =
                CreateToken("down", "Down");

            PatternDefinition fallback =
                CreatePattern(
                    "fallback",
                    "Fallback");

            PatternDatabase database =
                CreateAsset<PatternDatabase>();

            database.Configure(fallback);

            PatternDefinition result =
                database.CreateResolver().Resolve(
                    new[] { down });

            Assert.AreSame(fallback, result);
        }

        [Test]
        public void CreateResolver_RejectsDuplicateSequences()
        {
            PatternToken up =
                CreateToken("up", "Up");

            PatternDefinition first =
                CreatePattern(
                    "first",
                    "First",
                    up);

            PatternDefinition duplicate =
                CreatePattern(
                    "duplicate",
                    "Duplicate",
                    up);

            PatternDatabase database =
                CreateAsset<PatternDatabase>();

            database.Configure(
                null,
                first,
                duplicate);

            Assert.Throws<InvalidOperationException>(
                () => database.CreateResolver());
        }

        private PatternToken CreateToken(
            string id,
            string displayName)
        {
            PatternToken token =
                CreateAsset<PatternToken>();

            token.Configure(id, displayName);

            return token;
        }

        private PatternDefinition CreatePattern(
            string id,
            string displayName,
            params PatternToken[] tokens)
        {
            PatternDefinition pattern =
                CreateAsset<PatternDefinition>();

            pattern.Configure(
                id,
                displayName,
                tokens);

            return pattern;
        }

        private T CreateAsset<T>()
            where T : ScriptableObject
        {
            T asset =
                ScriptableObject.CreateInstance<T>();

            _createdAssets.Add(asset);

            return asset;
        }
    }
}