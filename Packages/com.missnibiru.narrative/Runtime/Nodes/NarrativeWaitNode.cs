using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeWaitNode : NarrativeNode
    {
        [SerializeField, Min(0f)]
        private float duration = 1f;

        [SerializeField]
        private bool useUnscaledTime = true;

        [SerializeField, HideInInspector]
        private string nextNodeId = string.Empty;

        public override string NodeTitle => "Wait";
        public float Duration => Mathf.Max(0f, duration);
        public bool UseUnscaledTime => useUnscaledTime;
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

        private void OnValidate()
        {
            duration = Mathf.Max(0f, duration);
        }
    }
}
