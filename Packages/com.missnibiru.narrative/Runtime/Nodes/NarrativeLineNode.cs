using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MissNibiru.Narrative
{
    public sealed class NarrativeLineNode : NarrativeNode
    {
        [Header("Speaker")]

        [SerializeField]
        private NarrativeCharacter character;

        [SerializeField]
        private NarrativeEmotion emotion;

        [SerializeField]
        private NarrativePortraitSide portraitSide =
            NarrativePortraitSide.Left;

        [Header("Dialogue")]

        [SerializeField, TextArea(4, 10)]
        private string text = "New dialogue line.";

        [SerializeField, HideInInspector]
        private NarrativeTextSegment[] importedSegments =
            System.Array.Empty<NarrativeTextSegment>();

        [SerializeField, HideInInspector]
        private bool useImportedSegments;

        [SerializeField, Min(1)]
        private int wordLimit = 60;

        [SerializeField, Tooltip("Zero uses default.")]
        private float typewriterSpeed;

        [SerializeField]
        private bool autoAdvance;

        [SerializeField, Min(0f)]
        private float autoAdvanceDelay = 1f;

        [Header("Presentation")]

        [SerializeField]
        private Sprite background;

        [SerializeField]
        private AudioClip music;

        [SerializeField]
        private AudioClip voiceClip;

        [SerializeField]
        private AudioClip soundEffect;

        [SerializeField, HideInInspector]
        private string nextNodeId = string.Empty;

        public override string NodeTitle
        {
            get
            {
                string speaker = character == null
                    ? "Narration"
                    : character.DisplayName;

                return string.IsNullOrWhiteSpace(speaker)
                    ? "Dialogue Line"
                    : speaker;
            }
        }

        public NarrativeCharacter Character => character;
        public NarrativeEmotion Emotion => emotion;
        public NarrativePortraitSide PortraitSide => portraitSide;
        public string Text => text ?? string.Empty;
        public IReadOnlyList<NarrativeTextSegment> ImportedSegments =>
            importedSegments ?? System.Array.Empty<NarrativeTextSegment>();
        public bool UseImportedSegments => useImportedSegments;
        public int WordLimit => Mathf.Max(1, wordLimit);
        public float TypewriterSpeed => Mathf.Max(0f, typewriterSpeed);
        public bool AutoAdvance => autoAdvance;
        public float AutoAdvanceDelay => Mathf.Max(0f, autoAdvanceDelay);
        public Sprite Background => background;
        public AudioClip Music => music;
        public AudioClip VoiceClip => voiceClip;
        public AudioClip SoundEffect => soundEffect;
        public string NextNodeId => nextNodeId ?? string.Empty;

        public void SetNextNodeId(string value)
        {
            nextNodeId = value ?? string.Empty;
        }

        public void ConfigureImportedText(
            string editorText,
            NarrativeTextSegment[] segments)
        {
            text = editorText ?? string.Empty;
            importedSegments = segments ??
                System.Array.Empty<NarrativeTextSegment>();
            useImportedSegments = importedSegments.Length > 0;
        }

        public void FlattenImportedText()
        {
            useImportedSegments = false;
            importedSegments = System.Array.Empty<NarrativeTextSegment>();
        }

        public string ResolveText(NarrativeBlackboard blackboard)
        {
            if (!useImportedSegments || importedSegments == null ||
                importedSegments.Length == 0)
                return Text;

            StringBuilder result = new StringBuilder();

            foreach (NarrativeTextSegment segment in importedSegments)
            {
                if (segment == null || !segment.IsVisible(blackboard))
                    continue;

                result.Append(segment.Text);
            }

            return result.ToString().Trim();
        }

        public override IEnumerable<string> GetOutgoingNodeIds()
        {
            if (!string.IsNullOrWhiteSpace(NextNodeId))
                yield return NextNodeId;
        }

        private void OnValidate()
        {
            wordLimit = Mathf.Max(1, wordLimit);
            typewriterSpeed = Mathf.Max(0f, typewriterSpeed);
            autoAdvanceDelay = Mathf.Max(0f, autoAdvanceDelay);
            importedSegments ??=
                System.Array.Empty<NarrativeTextSegment>();
        }
    }
}
