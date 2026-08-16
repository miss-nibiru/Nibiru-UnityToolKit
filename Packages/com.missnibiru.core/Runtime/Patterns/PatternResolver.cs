using System;
using System.Collections.Generic;

namespace MissNibiru.Core.Patterns
{
    public sealed class PatternResolver<TToken, TResult>
    {
        private sealed class RegisteredPattern
        {
            public TToken[] Tokens { get; }
            public TResult Result { get; }

            public RegisteredPattern(
                TToken[] tokens,
                TResult result)
            {
                Tokens = tokens;
                Result = result;
            }
        }

        private readonly List<RegisteredPattern> _patterns =
            new List<RegisteredPattern>();

        private readonly IEqualityComparer<TToken>
            _tokenComparer;

        public TResult FallbackResult { get; }

        public PatternResolver(
            TResult fallbackResult,
            IEqualityComparer<TToken> tokenComparer = null)
        {
            FallbackResult = fallbackResult;

            _tokenComparer =
                tokenComparer ??
                EqualityComparer<TToken>.Default;
        }

        public void Register(
            IReadOnlyList<TToken> tokens,
            TResult result)
        {
            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));

            if (tokens.Count == 0)
            {
                throw new ArgumentException(
                    "A pattern requires at least one token.",
                    nameof(tokens));
            }

            TToken[] tokenCopy = CopyTokens(tokens);

            if (ContainsPattern(tokenCopy))
            {
                throw new InvalidOperationException(
                    "The same pattern cannot be registered twice.");
            }

            _patterns.Add(
                new RegisteredPattern(tokenCopy, result));
        }

        public TResult Resolve(
            IReadOnlyList<TToken> submittedTokens)
        {
            TryResolve(
                submittedTokens,
                out TResult result);

            return result;
        }

        public bool TryResolve(
            IReadOnlyList<TToken> submittedTokens,
            out TResult result)
        {
            if (submittedTokens == null)
            {
                throw new ArgumentNullException(
                    nameof(submittedTokens));
            }

            foreach (RegisteredPattern pattern in _patterns)
            {
                if (!SequencesMatch(
                        submittedTokens,
                        pattern.Tokens))
                {
                    continue;
                }

                result = pattern.Result;
                return true;
            }

            result = FallbackResult;
            return false;
        }

        private bool ContainsPattern(
            IReadOnlyList<TToken> tokens)
        {
            foreach (RegisteredPattern pattern in _patterns)
            {
                if (SequencesMatch(tokens, pattern.Tokens))
                    return true;
            }

            return false;
        }

        private bool SequencesMatch(
            IReadOnlyList<TToken> first,
            IReadOnlyList<TToken> second)
        {
            if (first.Count != second.Count)
                return false;

            for (int index = 0;
                 index < first.Count;
                 index++)
            {
                if (!_tokenComparer.Equals(
                        first[index],
                        second[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static TToken[] CopyTokens(
            IReadOnlyList<TToken> source)
        {
            TToken[] copy = new TToken[source.Count];

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }
}