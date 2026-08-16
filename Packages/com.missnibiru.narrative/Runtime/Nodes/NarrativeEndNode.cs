using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeEndNode : NarrativeNode
    {
        [SerializeField]
        private string endingId = "complete";

        [SerializeField, TextArea(1, 3)]
        private string description = string.Empty;

        public override string NodeTitle => "End";
        public string EndingId => endingId ?? string.Empty;
        public string Description => description ?? string.Empty;

        public void Configure(string stableEndingId, string details = "")
        {
            endingId = stableEndingId ?? string.Empty;
            description = details ?? string.Empty;
        }

        public override IEnumerable<string> GetOutgoingNodeIds()
        {
            return Array.Empty<string>();
        }
    }
}
