using System;
using System.Collections.Generic;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeBlackboard
    {
        private sealed class RuntimeValue
        {
            public NarrativeVariableType Type;
            public bool BooleanValue;
            public int IntegerValue;
            public float FloatValue;
            public string StringValue = string.Empty;
        }

        private readonly Dictionary<NarrativeFlag, bool> _flags =
            new Dictionary<NarrativeFlag, bool>();

        private readonly Dictionary<NarrativeVariable, RuntimeValue>
            _variables =
                new Dictionary<NarrativeVariable, RuntimeValue>();

        public NarrativeBlackboard(NarrativeStory story)
        {
            if (story == null)
                return;

            foreach (NarrativeFlag flag in story.Flags)
            {
                if (flag != null && !_flags.ContainsKey(flag))
                    _flags.Add(flag, flag.DefaultValue);
            }

            foreach (NarrativeVariable variable in story.Variables)
            {
                if (variable != null && !_variables.ContainsKey(variable))
                    _variables.Add(variable, CreateDefault(variable));
            }
        }

        public bool GetFlag(NarrativeFlag flag)
        {
            if (flag == null)
                return false;

            if (_flags.TryGetValue(flag, out bool value))
                return value;

            value = flag.DefaultValue;
            _flags.Add(flag, value);
            return value;
        }

        public void SetFlag(NarrativeFlag flag, bool value)
        {
            if (flag != null)
                _flags[flag] = value;
        }

        public bool Compare(
            NarrativeVariable variable,
            NarrativeComparison comparison,
            bool booleanValue,
            int integerValue,
            float floatValue,
            string stringValue)
        {
            if (variable == null)
                return false;

            RuntimeValue current = GetValue(variable);

            switch (variable.VariableType)
            {
                case NarrativeVariableType.Boolean:
                    return CompareBoolean(
                        current.BooleanValue,
                        booleanValue,
                        comparison);
                case NarrativeVariableType.Integer:
                    return CompareNumber(
                        current.IntegerValue,
                        integerValue,
                        comparison);
                case NarrativeVariableType.Float:
                    return CompareNumber(
                        current.FloatValue,
                        floatValue,
                        comparison);
                case NarrativeVariableType.String:
                    return CompareString(
                        current.StringValue,
                        stringValue,
                        comparison);
                default:
                    return false;
            }
        }

        public void Apply(NarrativeSetValueNode node)
        {
            if (node == null)
                return;

            if (node.Flag != null)
            {
                bool current = GetFlag(node.Flag);
                bool value = node.Mutation == NarrativeMutation.Toggle
                    ? !current
                    : node.BooleanValue;
                SetFlag(node.Flag, value);
            }

            if (node.Variable == null)
                return;

            RuntimeValue target = GetValue(node.Variable);

            switch (node.Variable.VariableType)
            {
                case NarrativeVariableType.Boolean:
                    target.BooleanValue =
                        node.Mutation == NarrativeMutation.Toggle
                            ? !target.BooleanValue
                            : node.BooleanValue;
                    break;
                case NarrativeVariableType.Integer:
                    target.IntegerValue = MutateNumber(
                        target.IntegerValue,
                        node.IntegerValue,
                        node.Mutation);
                    break;
                case NarrativeVariableType.Float:
                    target.FloatValue = MutateNumber(
                        target.FloatValue,
                        node.FloatValue,
                        node.Mutation);
                    break;
                case NarrativeVariableType.String:
                    target.StringValue =
                        node.Mutation == NarrativeMutation.Add
                            ? target.StringValue + node.StringValue
                            : node.StringValue;
                    break;
            }
        }

        public NarrativeSaveData CreateSaveData(
            NarrativeStory story,
            string currentNodeId)
        {
            NarrativeSaveData data = new NarrativeSaveData
            {
                storyId = story == null ? string.Empty : story.Id,
                currentNodeId = currentNodeId ?? string.Empty
            };

            foreach (KeyValuePair<NarrativeFlag, bool> pair in _flags)
            {
                if (pair.Key == null)
                    continue;

                data.flags.Add(
                    new NarrativeFlagSaveValue
                    {
                        id = pair.Key.Id,
                        value = pair.Value
                    });
            }

            foreach (KeyValuePair<NarrativeVariable, RuntimeValue> pair
                     in _variables)
            {
                if (pair.Key == null)
                    continue;

                data.variables.Add(
                    new NarrativeVariableSaveValue
                    {
                        id = pair.Key.Id,
                        type = pair.Value.Type,
                        booleanValue = pair.Value.BooleanValue,
                        integerValue = pair.Value.IntegerValue,
                        floatValue = pair.Value.FloatValue,
                        stringValue = pair.Value.StringValue
                    });
            }

            return data;
        }

        public void Restore(
            NarrativeStory story,
            NarrativeSaveData data)
        {
            if (story == null || data == null)
                return;

            foreach (NarrativeFlagSaveValue saved in data.flags)
            {
                NarrativeFlag flag = FindFlag(story, saved.id);

                if (flag != null)
                    SetFlag(flag, saved.value);
            }

            foreach (NarrativeVariableSaveValue saved in data.variables)
            {
                NarrativeVariable variable =
                    FindVariable(story, saved.id);

                if (variable == null)
                    continue;

                RuntimeValue target = GetValue(variable);
                target.BooleanValue = saved.booleanValue;
                target.IntegerValue = saved.integerValue;
                target.FloatValue = saved.floatValue;
                target.StringValue = saved.stringValue ?? string.Empty;
            }
        }

        private RuntimeValue GetValue(NarrativeVariable variable)
        {
            if (!_variables.TryGetValue(variable, out RuntimeValue value))
            {
                value = CreateDefault(variable);
                _variables.Add(variable, value);
            }

            return value;
        }

        private static RuntimeValue CreateDefault(
            NarrativeVariable variable)
        {
            return new RuntimeValue
            {
                Type = variable.VariableType,
                BooleanValue = variable.DefaultBoolean,
                IntegerValue = variable.DefaultInteger,
                FloatValue = variable.DefaultFloat,
                StringValue = variable.DefaultString
            };
        }

        private static bool CompareBoolean(
            bool current,
            bool expected,
            NarrativeComparison comparison)
        {
            return comparison == NarrativeComparison.NotEqual
                ? current != expected
                : current == expected;
        }

        private static bool CompareNumber(
            float current,
            float expected,
            NarrativeComparison comparison)
        {
            switch (comparison)
            {
                case NarrativeComparison.NotEqual:
                    return Math.Abs(current - expected) > 0.0001f;
                case NarrativeComparison.Greater:
                    return current > expected;
                case NarrativeComparison.GreaterOrEqual:
                    return current >= expected;
                case NarrativeComparison.Less:
                    return current < expected;
                case NarrativeComparison.LessOrEqual:
                    return current <= expected;
                default:
                    return Math.Abs(current - expected) <= 0.0001f;
            }
        }

        private static bool CompareString(
            string current,
            string expected,
            NarrativeComparison comparison)
        {
            current ??= string.Empty;
            expected ??= string.Empty;

            if (comparison == NarrativeComparison.Contains)
            {
                return current.IndexOf(
                           expected,
                           StringComparison.OrdinalIgnoreCase) >= 0;
            }

            bool equal = string.Equals(
                current,
                expected,
                StringComparison.OrdinalIgnoreCase);

            return comparison == NarrativeComparison.NotEqual
                ? !equal
                : equal;
        }

        private static int MutateNumber(
            int current,
            int value,
            NarrativeMutation mutation)
        {
            switch (mutation)
            {
                case NarrativeMutation.Add:
                    return current + value;
                case NarrativeMutation.Subtract:
                    return current - value;
                default:
                    return value;
            }
        }

        private static float MutateNumber(
            float current,
            float value,
            NarrativeMutation mutation)
        {
            switch (mutation)
            {
                case NarrativeMutation.Add:
                    return current + value;
                case NarrativeMutation.Subtract:
                    return current - value;
                default:
                    return value;
            }
        }

        private static NarrativeFlag FindFlag(
            NarrativeStory story,
            string id)
        {
            foreach (NarrativeFlag flag in story.Flags)
            {
                if (flag != null && flag.Id == id)
                    return flag;
            }

            return null;
        }

        private static NarrativeVariable FindVariable(
            NarrativeStory story,
            string id)
        {
            foreach (NarrativeVariable variable in story.Variables)
            {
                if (variable != null && variable.Id == id)
                    return variable;
            }

            return null;
        }
    }
}
