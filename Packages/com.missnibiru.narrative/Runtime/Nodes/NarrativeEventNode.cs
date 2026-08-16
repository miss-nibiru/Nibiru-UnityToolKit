using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeEventNode : NarrativeNode
    {
        [SerializeField]
        private NarrativeEvent gameplayEvent;

        [SerializeField]
        private string payload = string.Empty;

        [SerializeField, HideInInspector]
        private string nextNodeId = string.Empty;

        public override string NodeTitle => "Gameplay Event";
        public NarrativeEvent GameplayEvent => gameplayEvent;
        public string Payload => payload ?? string.Empty;
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
