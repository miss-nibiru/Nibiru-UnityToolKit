using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeSetValueNode : NarrativeNode
    {
        [SerializeField]
        private NarrativeFlag flag;

        [SerializeField]
        private NarrativeVariable variable;

        [SerializeField]
        private NarrativeMutation mutation;

        [SerializeField]
        private bool booleanValue = true;

        [SerializeField]
        private int integerValue;

        [SerializeField]
        private float floatValue;

        [SerializeField]
        private string stringValue = string.Empty;

        [SerializeField, HideInInspector]
        private string nextNodeId = string.Empty;

        public override string NodeTitle => "Set Value";
        public NarrativeFlag Flag => flag;
        public NarrativeVariable Variable => variable;
        public NarrativeMutation Mutation => mutation;
        public bool BooleanValue => booleanValue;
        public int IntegerValue => integerValue;
        public float FloatValue => floatValue;
        public string StringValue => stringValue ?? string.Empty;
        public string NextNodeId => nextNodeId ?? string.Empty;

        public void SetNextNodeId(string value)
        {
            nextNodeId = value ?? string.Empty;
        }

        public override IEnumerable<string> GetOutgoingNodeIds()
        {
            if (!string.IsNullOrWhiteSpace(NextNodeId))
                yield return NextNodeId;
        }
    }
}
