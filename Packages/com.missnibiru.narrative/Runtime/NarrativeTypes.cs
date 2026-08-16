using System;
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
        private string targetNodeId = string.Empty;

        public string Text => text ?? string.Empty;
        public int WordLimit => Mathf.Max(1, wordLimit);
        public NarrativeCondition Condition => condition;
        public string TargetNodeId => targetNodeId ?? string.Empty;

        public bool IsAvailable(NarrativeBlackboard blackboard)
        {
            return condition == null || condition.Evaluate(blackboard);
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
