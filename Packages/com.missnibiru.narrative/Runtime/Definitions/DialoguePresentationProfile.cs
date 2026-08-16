using UnityEngine;

namespace MissNibiru.Narrative
{
    [CreateAssetMenu(
        fileName = "DialoguePresentationProfile",
        menuName = "Miss Nibiru/Narrative/Presentation Profile")]
    public sealed class DialoguePresentationProfile : ScriptableObject
    {
        [Header("Canvas")]

        [SerializeField]
        private Vector2 referenceResolution =
            new Vector2(1920f, 1080f);

        [SerializeField]
        private Color previewBackground =
            new Color(0.055f, 0.035f, 0.09f, 1f);

        [Header("Layout")]

        [SerializeField]
        private NarrativeRect background =
            new NarrativeRect(0f, 0f, 1f, 1f);

        [SerializeField]
        private NarrativeRect leftPortrait =
            new NarrativeRect(0.02f, 0.18f, 0.36f, 0.78f);

        [SerializeField]
        private NarrativeRect rightPortrait =
            new NarrativeRect(0.62f, 0.18f, 0.36f, 0.78f);

        [SerializeField]
        private NarrativeRect dialogueBox =
            new NarrativeRect(0.04f, 0.03f, 0.92f, 0.30f);

        [SerializeField]
        private NarrativeRect speakerName =
            new NarrativeRect(0.07f, 0.275f, 0.30f, 0.065f);

        [SerializeField]
        private NarrativeRect bodyText =
            new NarrativeRect(0.075f, 0.07f, 0.85f, 0.19f);

        [SerializeField]
        private NarrativeRect choices =
            new NarrativeRect(0.55f, 0.35f, 0.40f, 0.50f);

        [Header("Style")]

        [SerializeField]
        private Sprite dialogueBoxSprite;

        [SerializeField]
        private Sprite choiceButtonSprite;

        [SerializeField]
        private Color dialogueBoxColour =
            new Color(0.10f, 0.055f, 0.18f, 0.94f);

        [SerializeField]
        private Color choiceColour =
            new Color(0.26f, 0.12f, 0.40f, 0.96f);

        [SerializeField]
        private Color choiceHighlightColour =
            new Color(0.68f, 0.30f, 0.92f, 1f);

        [SerializeField]
        private Color textColour = Color.white;

        [SerializeField]
        private Font font;

        [SerializeField, Min(8)]
        private int speakerFontSize = 32;

        [SerializeField, Min(8)]
        private int bodyFontSize = 28;

        [SerializeField, Min(8)]
        private int choiceFontSize = 24;

        [SerializeField, Min(20f)]
        private float choiceHeight = 72f;

        [SerializeField, Min(0f)]
        private float choiceSpacing = 12f;

        [Header("Playback")]

        [SerializeField, Min(0f)]
        private float defaultTypewriterSpeed = 0.035f;

        [SerializeField]
        private bool useUnscaledTime = true;

        public Vector2 ReferenceResolution => referenceResolution;
        public Color PreviewBackground => previewBackground;
        public Sprite DialogueBoxSprite => dialogueBoxSprite;
        public Sprite ChoiceButtonSprite => choiceButtonSprite;
        public Color DialogueBoxColour => dialogueBoxColour;
        public Color ChoiceColour => choiceColour;
        public Color ChoiceHighlightColour => choiceHighlightColour;
        public Color TextColour => textColour;
        public Font Font => font;
        public int SpeakerFontSize => speakerFontSize;
        public int BodyFontSize => bodyFontSize;
        public int ChoiceFontSize => choiceFontSize;
        public float ChoiceHeight => choiceHeight;
        public float ChoiceSpacing => choiceSpacing;
        public float DefaultTypewriterSpeed => defaultTypewriterSpeed;
        public bool UseUnscaledTime => useUnscaledTime;

        public NarrativeRect GetRect(NarrativeLayoutElement element)
        {
            switch (element)
            {
                case NarrativeLayoutElement.Background:
                    return background;
                case NarrativeLayoutElement.LeftPortrait:
                    return leftPortrait;
                case NarrativeLayoutElement.RightPortrait:
                    return rightPortrait;
                case NarrativeLayoutElement.DialogueBox:
                    return dialogueBox;
                case NarrativeLayoutElement.SpeakerName:
                    return speakerName;
                case NarrativeLayoutElement.BodyText:
                    return bodyText;
                case NarrativeLayoutElement.Choices:
                    return choices;
                default:
                    return dialogueBox;
            }
        }

        public void SetRect(
            NarrativeLayoutElement element,
            NarrativeRect value)
        {
            value.Clamp();

            switch (element)
            {
                case NarrativeLayoutElement.Background:
                    background = value;
                    break;
                case NarrativeLayoutElement.LeftPortrait:
                    leftPortrait = value;
                    break;
                case NarrativeLayoutElement.RightPortrait:
                    rightPortrait = value;
                    break;
                case NarrativeLayoutElement.DialogueBox:
                    dialogueBox = value;
                    break;
                case NarrativeLayoutElement.SpeakerName:
                    speakerName = value;
                    break;
                case NarrativeLayoutElement.BodyText:
                    bodyText = value;
                    break;
                case NarrativeLayoutElement.Choices:
                    choices = value;
                    break;
            }
        }

        private void OnValidate()
        {
            referenceResolution.x = Mathf.Max(320f, referenceResolution.x);
            referenceResolution.y = Mathf.Max(180f, referenceResolution.y);
            speakerFontSize = Mathf.Max(8, speakerFontSize);
            bodyFontSize = Mathf.Max(8, bodyFontSize);
            choiceFontSize = Mathf.Max(8, choiceFontSize);
            choiceHeight = Mathf.Max(20f, choiceHeight);
            choiceSpacing = Mathf.Max(0f, choiceSpacing);
            defaultTypewriterSpeed =
                Mathf.Max(0f, defaultTypewriterSpeed);

            background.Clamp();
            leftPortrait.Clamp();
            rightPortrait.Clamp();
            dialogueBox.Clamp();
            speakerName.Clamp();
            bodyText.Clamp();
            choices.Clamp();
        }
    }
}
