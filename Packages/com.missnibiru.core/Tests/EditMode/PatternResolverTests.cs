using System;
using MissNibiru.Core.Patterns;
using NUnit.Framework;

namespace MissNibiru.Core.Tests.Patterns
{
    public sealed class PatternResolverTests
    {
        private enum TestToken
        {
            Up,
            Down,
            Attack
        }

        [Test]
        public void Resolve_ReturnsResultForExactMatch()
        {
            PatternResolver<TestToken, string> resolver =
                CreateResolver();

            string result = resolver.Resolve(
                new[]
                {
                    TestToken.Up,
                    TestToken.Attack
                });

            Assert.AreEqual("Uppercut", result);
        }

        [Test]
        public void TryResolve_ReturnsFallbackForFailedMatch()
        {
            PatternResolver<TestToken, string> resolver =
                CreateResolver();

            bool matched = resolver.TryResolve(
                new[]
                {
                    TestToken.Down,
                    TestToken.Attack
                },
                out string result);

            Assert.IsFalse(matched);
            Assert.AreEqual("Basic Attack", result);
        }

        [Test]
        public void Register_RejectsDuplicatePattern()
        {
            PatternResolver<TestToken, string> resolver =
                CreateResolver();

            Assert.Throws<InvalidOperationException>(
                () => resolver.Register(
                    new[]
                    {
                        TestToken.Up,
                        TestToken.Attack
                    },
                    "Duplicate"));
        }

        private static PatternResolver<
            TestToken,
            string> CreateResolver()
        {
            PatternResolver<TestToken, string> resolver =
                new PatternResolver<TestToken, string>(
                    "Basic Attack");

            resolver.Register(
                new[]
                {
                    TestToken.Up,
                    TestToken.Attack
                },
                "Uppercut");

            return resolver;
        }
    }
}