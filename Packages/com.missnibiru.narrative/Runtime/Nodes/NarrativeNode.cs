using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public abstract class NarrativeNode : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private string id = string.Empty;

        [SerializeField, HideInInspector]
        private Vector2 editorPosition;

        public string Id => id ?? string.Empty;
        public Vector2 EditorPosition => editorPosition;
        public abstract string NodeTitle { get; }

        public void Initialize(string stableId, Vector2 position)
        {
            id = stableId ?? string.Empty;
            editorPosition = position;
        }

        public void SetEditorPosition(Vector2 position)
        {
            editorPosition = position;
        }

        public abstract IEnumerable<string> GetOutgoingNodeIds();
    }
}
