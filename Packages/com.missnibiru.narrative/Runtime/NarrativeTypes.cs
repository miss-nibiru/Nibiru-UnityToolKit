using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public enum NarrativePortraitSide
    {
        Left,
        Right,
        Center,
        Hidden
    }

    public enum NarrativeVariableType
    {
        Boolean,
        Integer,
        Float,
        String
    }

    public enum NarrativeConditionMode
    {
        Always,
        Flag,
        Variable
    }

    public enum NarrativeComparison
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Contains
    }

    public enum NarrativeMutation
    {
        Set,
        Add,
        Subtract,
        Toggle
    }

    public enum NarrativeExpressionTokenType
    {
        Condition,
        And,
        Or,
        Not
    }

    public enum NarrativeLayoutElement
    {
        Background,
        LeftPortrait,
        RightPortrait,
        DialogueBox,
        SpeakerName,
        BodyText,
        Choices
    }

    public enum NarrativeValidationSeverity
    {
        Information,
        Warning,
        Error
    }

    [Serializable]
    public struct NarrativeRect
    {
        [Range(0f, 1f)]
        public float x;

        [Range(0f, 1f)]
        public float y;

        [Range(0.01f, 1f)]
        public float width;

        [Range(0.01f, 1f)]
        public float height;

        public NarrativeRect(
            float normalizedX,
            float normalizedY,
            float normalizedWidth,
            float normalizedHeight)
        {
            x = normalizedX;
            y = normalizedY;
            width = normalizedWidth;
            height = normalizedHeight;
            Clamp();
        }

        public void Clamp()
        {
            width = Mathf.Clamp(width, 0.01f, 1f);
            height = Mathf.Clamp(height, 0.01f, 1f);
            x = Mathf.Clamp(x, 0f, 1f - width);
            y = Mathf.Clamp(y, 0f, 1f - height);
        }
    }

    [Serializable]
    public sealed class NarrativeEmotionPortrait
    {
        [SerializeField]
        private NarrativeEmotion emotion;

        [SerializeField]
        private Sprite portrait;

        public NarrativeEmotion Emotion => emotion;
        public Sprite Portrait => portrait;
    }

    [Serializable]
    public sealed class NarrativeCondition
    {
        [SerializeField]
        private NarrativeConditionMode mode;

        [SerializeField]
        private NarrativeFlag flag;

        [SerializeField]
        private NarrativeVariable variable;

        [SerializeField]
        private NarrativeComparison comparison;

        [SerializeField]
        private bool booleanValue = true;

        [SerializeField]
        private int integerValue;

        [SerializeField]
        private float floatValue;

        [SerializeField]
        private string stringValue = string.Empty;

        [SerializeField]
        private bool invert;

        public NarrativeConditionMode Mode => mode;
        public NarrativeFlag Flag => flag;
        public NarrativeVariable Variable => variable;
        public NarrativeComparison Comparison => comparison;
        public bool BooleanValue => booleanValue;
        public int IntegerValue => integerValue;
        public float FloatValue => floatValue;
        public string StringValue => stringValue ?? string.Empty;
        public bool Invert => invert;

        public void ConfigureFlag(
            NarrativeFlag target,
            bool expectedValue = true,
            bool inverted = false)
        {
            mode = NarrativeConditionMode.Flag;
            flag = target;
            variable = null;
            comparison = NarrativeComparison.Equal;
            booleanValue = expectedValue;
            invert = inverted;
        }

        public void ConfigureVariable(
            NarrativeVariable target,
            NarrativeComparison valueComparison,
            bool expectedBoolean,
            int expectedInteger,
            float expectedFloat,
            string expectedString,
            bool inverted = false)
        {
            mode = NarrativeConditionMode.Variable;
            variable = target;
            flag = null;
            comparison = valueComparison;
            booleanValue = expectedBoolean;
            integerValue = expectedInteger;
            floatValue = expectedFloat;
            stringValue = expectedString ?? string.Empty;
            invert = inverted;
        }

        public bool Evaluate(NarrativeBlackboard blackboard)
        {
            if (blackboard == null)
                return mode == NarrativeConditionMode.Always;

            bool result;

            switch (mode)
            {
                case NarrativeConditionMode.Flag:
                    result = flag != null &&
                             blackboard.GetFlag(flag) == booleanValue;
                    break;
                case NarrativeConditionMode.Variable:
                    result = variable != null &&
                             blackboard.Compare(
                                 variable,
                                 comparison,
                                 booleanValue,
                                 integerValue,
                                 floatValue,
                                 StringValue);
                    break;
                default:
                    result = true;
                    break;
            }

            return invert ? !result : result;
        }
    }

    [Serializable]
    public sealed class NarrativeConditionExpressionToken
    {
        [SerializeField]
        private NarrativeExpressionTokenType tokenType;

        [SerializeField]
        private NarrativeCondition condition;

        public NarrativeExpressionTokenType TokenType => tokenType;
        public NarrativeCondition Condition => condition;

        public static NarrativeConditionExpressionToken CreateCondition(
            NarrativeCondition value)
        {
            return new NarrativeConditionExpressionToken
            {
                tokenType = NarrativeExpressionTokenType.Condition,
                condition = value
            };
        }

        public static NarrativeConditionExpressionToken CreateOperator(
            NarrativeExpressionTokenType value)
        {
            return new NarrativeConditionExpressionToken
            {
                tokenType = value
            };
        }
    }

    [Serializable]
    public sealed class NarrativeConditionExpression
    {
        [SerializeField]
        private NarrativeConditionExpressionToken[] tokens =
            Array.Empty<NarrativeConditionExpressionToken>();

        public IReadOnlyList<NarrativeConditionExpressionToken> Tokens =>
            tokens ?? Array.Empty<NarrativeConditionExpressionToken>();
        public bool IsEmpty => tokens == null || tokens.Length == 0;

        public void Configure(
            NarrativeConditionExpressionToken[] value)
        {
            tokens = value ??
                Array.Empty<NarrativeConditionExpressionToken>();
        }

        public bool Evaluate(NarrativeBlackboard blackboard)
        {
            if (IsEmpty)
                return true;

            Stack<bool> values = new Stack<bool>();

            foreach (NarrativeConditionExpressionToken token in tokens)
            {
                if (token == null)
                    return false;

                switch (token.TokenType)
                {
                    case NarrativeExpressionTokenType.Condition:
                        values.Push(
                            token.Condition != null &&
                            token.Condition.Evaluate(blackboard));
                        break;
                    case NarrativeExpressionTokenType.Not:
                        if (values.Count < 1)
                            return false;
                        values.Push(!values.Pop());
                        break;
                    case NarrativeExpressionTokenType.And:
                    case NarrativeExpressionTokenType.Or:
                        if (values.Count < 2)
                            return false;
                        bool right = values.Pop();
                        bool left = values.Pop();
                        values.Push(
                            token.TokenType == NarrativeExpressionTokenType.And
                                ? left && right
                                : left || right);
                        break;
                }
            }

            return values.Count == 1 && values.Pop();
        }
    }

    [Serializable]
    public sealed class NarrativeTextSegment
    {
        [SerializeField, TextArea(1, 8)]
        private string text = string.Empty;

        [SerializeField]
        private NarrativeConditionExpression condition =
            new NarrativeConditionExpression();

        public string Text => text ?? string.Empty;
        public NarrativeConditionExpression Condition => condition;

        public void Configure(
            string value,
            NarrativeConditionExpression requiredCondition = null)
        {
            text = value ?? string.Empty;
            condition = requiredCondition ??
                new NarrativeConditionExpression();
        }

        public bool IsVisible(NarrativeBlackboard blackboard)
        {
            return condition == null || condition.Evaluate(blackboard);
        }
    }

    [Serializable]
    public sealed class NarrativeChoiceOption
    {
        [SerializeField, TextArea(1, 3)]
        private string text = "Choice";

        [SerializeField, Min(1)]
        private int wordLimit = 12;

        [SerializeField]
        private NarrativeCondition condition =
            new NarrativeCondition();

        [SerializeField, HideInInspector]
        private NarrativeConditionExpression importedCondition =
            new NarrativeConditionExpression();

        [SerializeField, HideInInspector]
        private string targetNodeId = string.Empty;

        public string Text => text ?? string.Empty;
        public int WordLimit => Mathf.Max(1, wordLimit);
        public NarrativeCondition Condition => condition;
        public NarrativeConditionExpression ImportedCondition =>
            importedCondition;
        public string TargetNodeId => targetNodeId ?? string.Empty;

        public bool IsAvailable(NarrativeBlackboard blackboard)
        {
            bool manualCondition = condition == null ||
                                   condition.Evaluate(blackboard);
            bool generatedCondition = importedCondition == null ||
                                      importedCondition.Evaluate(blackboard);
            return manualCondition && generatedCondition;
        }

        public void Configure(
            string visibleText,
            int maximumWords,
            string targetId,
            NarrativeConditionExpression requiredCondition = null)
        {
            text = visibleText ?? string.Empty;
            wordLimit = Mathf.Max(1, maximumWords);
            targetNodeId = targetId ?? string.Empty;
            importedCondition = requiredCondition ??
                new NarrativeConditionExpression();
        }

        public void SetTargetNodeId(string value)
        {
            targetNodeId = value ?? string.Empty;
        }
    }

    public sealed class NarrativeValidationIssue
    {
        public NarrativeValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public NarrativeNode Node { get; }
        public UnityEngine.Object Context { get; }

        public NarrativeValidationIssue(
            NarrativeValidationSeverity severity,
            string code,
            string message,
            NarrativeNode node = null,
            UnityEngine.Object context = null)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Node = node;
            Context = context;
        }
    }
}
