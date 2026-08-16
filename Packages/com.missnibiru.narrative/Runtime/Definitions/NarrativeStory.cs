using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    [CreateAssetMenu(
        fileName = "NarrativeStory",
        menuName = "Miss Nibiru/Narrative/Story")]
    public sealed class NarrativeStory : ScriptableObject
    {
        [Header("Identity")]

        [SerializeField]
        private string id = "story";

        [SerializeField]
        private string displayName = "Narrative Story";

        [SerializeField, TextArea(2, 4)]
        private string description = string.Empty;

        [Header("Defaults")]

        [SerializeField]
        private DialoguePresentationProfile presentationProfile;

        [SerializeField, Min(1)]
        private int defaultLineWordLimit = 60;

        [Header("Library")]

        [SerializeField]
        private NarrativeCharacter[] characters =
            Array.Empty<NarrativeCharacter>();

        [SerializeField]
        private NarrativeVariable[] variables =
            Array.Empty<NarrativeVariable>();

        [SerializeField]
        private NarrativeFlag[] flags =
            Array.Empty<NarrativeFlag>();

        [SerializeField]
        private NarrativeEvent[] gameplayEvents =
            Array.Empty<NarrativeEvent>();

        [SerializeField, HideInInspector]
        private string startNodeId = string.Empty;

        [SerializeField, HideInInspector]
        private List<NarrativeNode> nodes =
            new List<NarrativeNode>();

        public string Id => CleanId(id);
        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;
        public DialoguePresentationProfile PresentationProfile =>
            presentationProfile;
        public int DefaultLineWordLimit =>
            Mathf.Max(1, defaultLineWordLimit);
        public IReadOnlyList<NarrativeCharacter> Characters =>
            characters ?? Array.Empty<NarrativeCharacter>();
        public IReadOnlyList<NarrativeVariable> Variables =>
            variables ?? Array.Empty<NarrativeVariable>();
        public IReadOnlyList<NarrativeFlag> Flags =>
            flags ?? Array.Empty<NarrativeFlag>();
        public IReadOnlyList<NarrativeEvent> GameplayEvents =>
            gameplayEvents ?? Array.Empty<NarrativeEvent>();
        public IReadOnlyList<NarrativeNode> Nodes => nodes;
        public string StartNodeId => startNodeId ?? string.Empty;

        public NarrativeNode FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return null;

            foreach (NarrativeNode node in nodes)
            {
                if (node != null && node.Id == nodeId)
                    return node;
            }

            return null;
        }

        public void AddNode(NarrativeNode node)
        {
            if (node == null)
                return;

            nodes ??= new List<NarrativeNode>();

            if (!nodes.Contains(node))
                nodes.Add(node);
        }

        public void RemoveNode(NarrativeNode node)
        {
            if (node == null || nodes == null)
                return;

            nodes.Remove(node);

            if (startNodeId == node.Id)
                startNodeId = string.Empty;
        }

        public void SetStartNode(NarrativeNode node)
        {
            startNodeId = node == null
                ? string.Empty
                : node.Id;
        }

        public void Configure(
            string stableId,
            string visibleName,
            DialoguePresentationProfile profile = null)
        {
            id = CleanId(stableId);
            displayName = visibleName ?? string.Empty;
            presentationProfile = profile;
        }

        private void OnValidate()
        {
            id = CleanId(id);
            defaultLineWordLimit = Mathf.Max(1, defaultLineWordLimit);
            characters ??= Array.Empty<NarrativeCharacter>();
            variables ??= Array.Empty<NarrativeVariable>();
            flags ??= Array.Empty<NarrativeFlag>();
            gameplayEvents ??= Array.Empty<NarrativeEvent>();
            nodes ??= new List<NarrativeNode>();
            nodes.RemoveAll(node => node == null);
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }
}
