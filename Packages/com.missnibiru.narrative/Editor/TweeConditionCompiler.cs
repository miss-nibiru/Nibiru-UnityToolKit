using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MissNibiru.Narrative;

namespace MissNibiru.Narrative.Editor
{
    public static class TweeConditionCompiler
    {
        private sealed class Parser
        {
            private readonly List<string> _tokens;
            private readonly IReadOnlyDictionary<string, NarrativeFlag> _flags;
            private readonly IReadOnlyDictionary<string, NarrativeVariable>
                _variables;
            private readonly List<TweeImportIssue> _issues;
            private readonly string _passage;
            private int _index;

            public Parser(
                List<string> tokens,
                IReadOnlyDictionary<string, NarrativeFlag> flags,
                IReadOnlyDictionary<string, NarrativeVariable> variables,
                List<TweeImportIssue> issues,
                string passage)
            {
                _tokens = tokens;
                _flags = flags;
                _variables = variables;
                _issues = issues;
                _passage = passage;
            }

            public List<NarrativeConditionExpressionToken> Parse()
            {
                List<NarrativeConditionExpressionToken> result = ParseOr();

                if (_index < _tokens.Count)
                {
                    AddIssue(
                        $"Condition contains unsupported syntax near " +
                        $"'{_tokens[_index]}'.");
                }

                return result;
            }

            private List<NarrativeConditionExpressionToken> ParseOr()
            {
                List<NarrativeConditionExpressionToken> result = ParseAnd();

                while (Match("or"))
                {
                    result.AddRange(ParseAnd());
                    result.Add(
                        NarrativeConditionExpressionToken.CreateOperator(
                            NarrativeExpressionTokenType.Or));
                }

                return result;
            }

            private List<NarrativeConditionExpressionToken> ParseAnd()
            {
                List<NarrativeConditionExpressionToken> result = ParseUnary();

                while (Match("and"))
                {
                    result.AddRange(ParseUnary());
                    result.Add(
                        NarrativeConditionExpressionToken.CreateOperator(
                            NarrativeExpressionTokenType.And));
                }

                return result;
            }

            private List<NarrativeConditionExpressionToken> ParseUnary()
            {
                if (Match("not") || Match("!"))
                {
                    List<NarrativeConditionExpressionToken> value =
                        ParseUnary();
                    value.Add(
                        NarrativeConditionExpressionToken.CreateOperator(
                            NarrativeExpressionTokenType.Not));
                    return value;
                }

                return ParsePrimary();
            }

            private List<NarrativeConditionExpressionToken> ParsePrimary()
            {
                if (Match("("))
                {
                    List<NarrativeConditionExpressionToken> nested = ParseOr();

                    if (!Match(")"))
                        AddIssue("Condition is missing a closing parenthesis.");

                    return nested;
                }

                if (_index >= _tokens.Count)
                {
                    AddIssue("Condition ended unexpectedly.");
                    return FalseCondition();
                }

                string variableToken = _tokens[_index++];

                if (!variableToken.StartsWith("$", StringComparison.Ordinal))
                {
                    AddIssue(
                        $"Expected a variable but found '{variableToken}'.");
                    return FalseCondition();
                }

                string name = variableToken.Substring(1);
                string comparisonToken = PeekComparison();
                string rawValue = "true";

                if (!string.IsNullOrEmpty(comparisonToken))
                {
                    _index++;

                    if (comparisonToken.Equals(
                            "is",
                            StringComparison.OrdinalIgnoreCase) &&
                        Peek("not"))
                    {
                        _index++;
                        comparisonToken = "isnot";
                    }

                    if (_index >= _tokens.Count)
                    {
                        AddIssue($"Condition for ${name} has no value.");
                    }
                    else
                    {
                        rawValue = _tokens[_index++];
                    }
                }

                NarrativeCondition condition = BuildCondition(
                    name,
                    comparisonToken,
                    rawValue);
                return new List<NarrativeConditionExpressionToken>
                {
                    NarrativeConditionExpressionToken.CreateCondition(
                        condition)
                };
            }

            private NarrativeCondition BuildCondition(
                string name,
                string operatorToken,
                string rawValue)
            {
                NarrativeComparison comparison = ToComparison(operatorToken);
                bool boolValue = ParseBoolean(rawValue);
                int integerValue = ParseInteger(rawValue);
                float floatValue = ParseFloat(rawValue);
                string stringValue = Unquote(rawValue);
                NarrativeCondition condition = new NarrativeCondition();

                if (_flags.TryGetValue(name, out NarrativeFlag flag))
                {
                    bool expected = boolValue;
                    bool inverted = comparison ==
                                    NarrativeComparison.NotEqual;
                    condition.ConfigureFlag(flag, expected, inverted);
                    return condition;
                }

                _variables.TryGetValue(
                    name, out NarrativeVariable variable);

                if (variable == null)
                {
                    AddIssue($"Condition references unknown variable ${name}.");
                }

                condition.ConfigureVariable(
                    variable,
                    comparison,
                    boolValue,
                    integerValue,
                    floatValue,
                    stringValue);
                return condition;
            }

            private string PeekComparison()
            {
                if (_index >= _tokens.Count)
                    return string.Empty;

                string token = _tokens[_index].ToLowerInvariant();

                switch (token)
                {
                    case "is":
                    case "isnot":
                    case "eq":
                    case "neq":
                    case "gte":
                    case "lte":
                    case "gt":
                    case "lt":
                    case "==":
                    case "!=":
                    case ">=":
                    case "<=":
                    case ">":
                    case "<":
                        return token;
                    default:
                        return string.Empty;
                }
            }

            private bool Match(string expected)
            {
                if (!Peek(expected))
                    return false;

                _index++;
                return true;
            }

            private bool Peek(string expected)
            {
                return _index < _tokens.Count &&
                       _tokens[_index].Equals(
                           expected,
                           StringComparison.OrdinalIgnoreCase);
            }

            private void AddIssue(string message)
            {
                _issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Warning,
                    _passage,
                    message));
            }

            private static List<NarrativeConditionExpressionToken>
                FalseCondition()
            {
                NarrativeCondition invalid = new NarrativeCondition();
                invalid.ConfigureVariable(
                    null,
                    NarrativeComparison.Equal,
                    true,
                    0,
                    0f,
                    string.Empty);
                return new List<NarrativeConditionExpressionToken>
                {
                    NarrativeConditionExpressionToken.CreateCondition(invalid)
                };
            }
        }

        private static readonly Regex TokenRegex = new Regex(
            "\\s*(\\$[A-Za-z_][A-Za-z0-9_]*|>=|<=|==|!=|>|<|" +
            "\\(|\\)|!|\"(?:\\\\.|[^\"])*\"|'(?:\\\\.|[^'])*'|" +
            "-?\\d+(?:\\.\\d+)?|[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled);

        public static NarrativeConditionExpression Compile(
            string source,
            IReadOnlyDictionary<string, NarrativeFlag> flags,
            IReadOnlyDictionary<string, NarrativeVariable> variables,
            List<TweeImportIssue> issues,
            string passage)
        {
            NarrativeConditionExpression expression =
                new NarrativeConditionExpression();

            if (string.IsNullOrWhiteSpace(source))
                return expression;

            List<string> tokens = Tokenize(source, issues, passage);
            Parser parser = new Parser(
                tokens,
                flags,
                variables,
                issues,
                passage);
            expression.Configure(parser.Parse().ToArray());
            return expression;
        }

        private static List<string> Tokenize(
            string source,
            List<TweeImportIssue> issues,
            string passage)
        {
            List<string> result = new List<string>();
            MatchCollection matches = TokenRegex.Matches(source);
            int cursor = 0;

            foreach (Match match in matches)
            {
                string gap = source.Substring(
                    cursor, match.Index - cursor);

                if (!string.IsNullOrWhiteSpace(gap))
                {
                    issues.Add(new TweeImportIssue(
                        TweeImportIssueSeverity.Warning,
                        passage,
                        $"Unsupported condition text: {gap.Trim()}"));
                }

                result.Add(match.Groups[1].Value);
                cursor = match.Index + match.Length;
            }

            if (cursor < source.Length &&
                !string.IsNullOrWhiteSpace(source.Substring(cursor)))
            {
                issues.Add(new TweeImportIssue(
                    TweeImportIssueSeverity.Warning,
                    passage,
                    "Condition has unsupported trailing text."));
            }

            return result;
        }

        private static NarrativeComparison ToComparison(string value)
        {
            switch ((value ?? string.Empty).ToLowerInvariant())
            {
                case "isnot":
                case "neq":
                case "!=":
                    return NarrativeComparison.NotEqual;
                case "gte":
                case ">=":
                    return NarrativeComparison.GreaterOrEqual;
                case "lte":
                case "<=":
                    return NarrativeComparison.LessOrEqual;
                case "gt":
                case ">":
                    return NarrativeComparison.Greater;
                case "lt":
                case "<":
                    return NarrativeComparison.Less;
                default:
                    return NarrativeComparison.Equal;
            }
        }

        private static bool ParseBoolean(string value)
        {
            bool.TryParse(Unquote(value), out bool result);
            return result;
        }

        private static int ParseInteger(string value)
        {
            int.TryParse(
                Unquote(value),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int result);
            return result;
        }

        private static float ParseFloat(string value)
        {
            float.TryParse(
                Unquote(value),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result);
            return result;
        }

        private static string Unquote(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }
    }
}
