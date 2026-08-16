using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeStartNode : NarrativeNode
    {
        [SerializeField, HideInInspector]
        private string nextNodeId = string.Empty;

        public override string NodeTitle => "Start";
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
