using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeConditionNode : NarrativeNode
    {
        [SerializeField]
        private NarrativeCondition condition =
            new NarrativeCondition();

        [SerializeField, HideInInspector]
        private string trueNodeId = string.Empty;

        [SerializeField, HideInInspector]
        private string falseNodeId = string.Empty;

        public override string NodeTitle => "Condition";
        public NarrativeCondition Condition => condition;
        public string TrueNodeId => trueNodeId ?? string.Empty;
        public string FalseNodeId => falseNodeId ?? string.Empty;

        public void SetTrueNodeId(string value)
        {
            trueNodeId = value ?? string.Empty;
        }

        public void SetFalseNodeId(string value)
        {
            falseNodeId = value ?? string.Empty;
        }

        public override IEnumerable<string> GetOutgoingNodeIds()
        {
            if (!string.IsNullOrWhiteSpace(TrueNodeId))
                yield return TrueNodeId;

            if (!string.IsNullOrWhiteSpace(FalseNodeId))
                yield return FalseNodeId;
        }
    }
}
