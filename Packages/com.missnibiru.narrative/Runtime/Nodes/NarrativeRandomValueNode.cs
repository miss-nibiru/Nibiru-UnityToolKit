using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeRandomValueNode : NarrativeNode
    {
        [SerializeField]
        private NarrativeVariable variable;

        [SerializeField]
        private int minimumInclusive;

        [SerializeField]
        private int maximumInclusive = 1;

        [SerializeField, HideInInspector]
        private string nextNodeId = string.Empty;

        public override string NodeTitle => "Random Value";
        public NarrativeVariable Variable => variable;
        public int MinimumInclusive => minimumInclusive;
        public int MaximumInclusive => maximumInclusive;
        public string NextNodeId => nextNodeId ?? string.Empty;

        public void Configure(
            NarrativeVariable target,
            int minimum,
            int maximum)
        {
            variable = target;
            minimumInclusive = Mathf.Min(minimum, maximum);
            maximumInclusive = Mathf.Max(minimum, maximum);
        }

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
            if (maximumInclusive < minimumInclusive)
                maximumInclusive = minimumInclusive;
        }
    }
}
