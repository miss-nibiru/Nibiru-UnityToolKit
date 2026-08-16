using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MissNibiru.Narrative
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NarrativeRunner))]
    [AddComponentMenu("Miss Nibiru/Narrative/Narrative Presenter")]
    public sealed class NarrativePresenter : MonoBehaviour
    {
        [SerializeField]
        private DialoguePresentationProfile presentationOverride;

        [SerializeField]
        private Canvas targetCanvas;

        private NarrativeRunner _runner;
        private DialoguePresentationProfile _profile;
        private Image _background;
        private Image _leftPortrait;
        private Image _rightPortrait;
        private Image _dialogueBox;
        private Text _speaker;
        private Text _body;
        private RectTransform _choiceRoot;
        private readonly List<Button> _choiceButtons = new List<Button>();
        private AudioSource _musicSource;
        private AudioSource _voiceSource;
        private AudioSource _soundSource;
        private Coroutine _typingRoutine;
        private Coroutine _autoAdvanceRoutine;
        private string _completeText = string.Empty;
        private bool _isTyping;

        private void Awake()
        {
            _runner = GetComponent<NarrativeRunner>();
            BuildInterface();
        }

        private void OnEnable()
        {
            if (_runner == null)
                _runner = GetComponent<NarrativeRunner>();

            _runner.LinePresented += PresentLine;
            _runner.ChoicesPresented += PresentChoices;
            _runner.StoryCompleted += HandleCompleted;
            _runner.StoryFaulted += HandleFault;
        }

        private void OnDisable()
        {
            if (_runner == null)
                return;

            _runner.LinePresented -= PresentLine;
            _runner.ChoicesPresented -= PresentChoices;
            _runner.StoryCompleted -= HandleCompleted;
            _runner.StoryFaulted -= HandleFault;
        }

        public void Advance()
        {
            if (_isTyping)
            {
                FinishTyping();
                return;
            }

            _runner.Advance();
        }

        private void BuildInterface()
        {
            if (_dialogueBox != null)
                return;

            _profile = presentationOverride != null
                ? presentationOverride
                : _runner.Story == null
                    ? null
                    : _runner.Story.PresentationProfile;

            if (_profile == null)
            {
                Debug.LogWarning(
                    "Assign a Dialogue Presentation Profile.", this);
                return;
            }

            EnsureCanvas();
            Font font = _profile.Font != null
                ? _profile.Font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _background = CreateImage(
                "Narrative Background",
                NarrativeLayoutElement.Background,
                targetCanvas.transform,
                _profile.PreviewBackground);
            _leftPortrait = CreateImage(
                "Left Portrait",
                NarrativeLayoutElement.LeftPortrait,
                targetCanvas.transform,
                Color.white,
                true);
            _rightPortrait = CreateImage(
                "Right Portrait",
                NarrativeLayoutElement.RightPortrait,
                targetCanvas.transform,
                Color.white,
                true);
            _dialogueBox = CreateImage(
                "Dialogue Box",
                NarrativeLayoutElement.DialogueBox,
                targetCanvas.transform,
                _profile.DialogueBoxColour);
            _dialogueBox.sprite = _profile.DialogueBoxSprite;
            _dialogueBox.gameObject.AddComponent<Button>()
                .onClick.AddListener(Advance);

            _speaker = CreateText(
                "Speaker",
                NarrativeLayoutElement.SpeakerName,
                targetCanvas.transform,
                font,
                _profile.SpeakerFontSize,
                TextAnchor.MiddleLeft);
            _body = CreateText(
                "Dialogue",
                NarrativeLayoutElement.BodyText,
                targetCanvas.transform,
                font,
                _profile.BodyFontSize,
                TextAnchor.UpperLeft);

            GameObject choices = CreateObject(
                "Choices",
                targetCanvas.transform);
            _choiceRoot = choices.GetComponent<RectTransform>();
            ApplyRect(_choiceRoot, _profile.GetRect(
                NarrativeLayoutElement.Choices));
            VerticalLayoutGroup layout =
                choices.AddComponent<VerticalLayoutGroup>();
            layout.spacing = _profile.ChoiceSpacing;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            for (int i = 0; i < NarrativeChoiceNode.MaximumChoices; i++)
                _choiceButtons.Add(CreateChoiceButton(i, font));

            _musicSource = CreateAudioSource("Narrative Music", true);
            _voiceSource = CreateAudioSource("Narrative Voice", false);
            _soundSource = CreateAudioSource("Narrative Sound", false);
            HideInterface();
        }

        private void EnsureCanvas()
        {
            if (targetCanvas == null)
            {
                GameObject canvasObject = new GameObject(
                    "Narrative Canvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                targetCanvas = canvasObject.GetComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();

            if (scaler == null)
                scaler = targetCanvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _profile.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject(
                    "EventSystem",
                    typeof(EventSystem));
                Type inputSystemModule = Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

                if (inputSystemModule != null)
                    eventSystem.AddComponent(inputSystemModule);
                else
                    eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private void PresentLine(NarrativeLineNode line)
        {
            if (_profile == null)
                BuildInterface();

            if (_profile == null)
                return;

            StopPresentationCoroutines();
            SetInterfaceVisible(true);
            HideChoices();

            _speaker.text = line.Character == null
                ? string.Empty
                : line.Character.DisplayName;
            _speaker.color = line.Character == null
                ? _profile.TextColour
                : line.Character.NameColour;

            Sprite portrait = line.Character == null
                ? null
                : line.Character.GetPortrait(line.Emotion);
            ShowPortrait(line.PortraitSide, portrait);

            if (line.Background != null)
            {
                _background.sprite = line.Background;
                _background.color = Color.white;
            }

            if (line.Music != null && _musicSource.clip != line.Music)
            {
                _musicSource.clip = line.Music;
                _musicSource.Play();
            }

            if (line.SoundEffect != null)
                _soundSource.PlayOneShot(line.SoundEffect);

            if (line.VoiceClip != null)
            {
                _voiceSource.clip = line.VoiceClip;
                _voiceSource.Play();
            }

            _completeText = line.Text;
            float speed = line.TypewriterSpeed > 0f
                ? line.TypewriterSpeed
                : _profile.DefaultTypewriterSpeed;
            _typingRoutine = StartCoroutine(TypeLine(line, speed));
        }

        private IEnumerator TypeLine(
            NarrativeLineNode line,
            float delay)
        {
            _isTyping = true;
            _body.text = string.Empty;
            NarrativeAudioProfile voice = line.Character == null
                ? null
                : line.Character.VoiceProfile;

            for (int i = 0; i < _completeText.Length; i++)
            {
                _body.text += _completeText[i];

                if (voice != null && voice.TypingSound != null &&
                    i % voice.CharactersPerSound == 0 &&
                    !char.IsWhiteSpace(_completeText[i]))
                {
                    _soundSource.pitch = UnityEngine.Random.Range(
                        voice.MinimumPitch,
                        voice.MaximumPitch);
                    _soundSource.PlayOneShot(
                        voice.TypingSound,
                        voice.Volume);
                }

                if (delay > 0f)
                {
                    if (_profile.UseUnscaledTime)
                        yield return new WaitForSecondsRealtime(delay);
                    else
                        yield return new WaitForSeconds(delay);
                }
                else
                {
                    yield return null;
                }
            }

            _typingRoutine = null;
            _isTyping = false;

            if (line.AutoAdvance)
            {
                _autoAdvanceRoutine = StartCoroutine(
                    AutoAdvance(line.AutoAdvanceDelay));
            }
        }

        private IEnumerator AutoAdvance(float delay)
        {
            if (delay > 0f)
            {
                if (_profile.UseUnscaledTime)
                    yield return new WaitForSecondsRealtime(delay);
                else
                    yield return new WaitForSeconds(delay);
            }

            _autoAdvanceRoutine = null;
            _runner.Advance();
        }

        private void PresentChoices(
            NarrativeChoiceNode node,
            IReadOnlyList<NarrativeChoiceOption> choices)
        {
            if (_profile == null)
                BuildInterface();

            if (_profile == null)
                return;

            StopPresentationCoroutines();
            SetInterfaceVisible(true);
            _speaker.text = string.Empty;
            _body.text = node.Prompt;
            _choiceRoot.gameObject.SetActive(true);

            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                Button button = _choiceButtons[i];
                bool visible = i < choices.Count;
                button.gameObject.SetActive(visible);

                if (!visible)
                    continue;

                int selected = i;
                Text label = button.GetComponentInChildren<Text>();
                label.text = choices[i].Text;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _runner.Choose(selected));
            }
        }

        private void HandleCompleted(NarrativeEndNode node)
        {
            StopPresentationCoroutines();
            HideInterface();
        }

        private void HandleFault(string message)
        {
            StopPresentationCoroutines();

            if (_body != null)
                _body.text = message;
        }

        private void FinishTyping()
        {
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
                _typingRoutine = null;
            }

            _isTyping = false;
            _body.text = _completeText;
        }

        private void StopPresentationCoroutines()
        {
            if (_typingRoutine != null)
                StopCoroutine(_typingRoutine);
            if (_autoAdvanceRoutine != null)
                StopCoroutine(_autoAdvanceRoutine);

            _typingRoutine = null;
            _autoAdvanceRoutine = null;
            _isTyping = false;
        }

        private void ShowPortrait(
            NarrativePortraitSide side,
            Sprite portrait)
        {
            ApplyRect(
                _leftPortrait.rectTransform,
                _profile.GetRect(NarrativeLayoutElement.LeftPortrait));
            ApplyRect(
                _rightPortrait.rectTransform,
                _profile.GetRect(NarrativeLayoutElement.RightPortrait));
            _leftPortrait.gameObject.SetActive(
                portrait != null &&
                (side == NarrativePortraitSide.Left ||
                 side == NarrativePortraitSide.Center));
            _rightPortrait.gameObject.SetActive(
                portrait != null && side == NarrativePortraitSide.Right);

            if (side == NarrativePortraitSide.Right)
                _rightPortrait.sprite = portrait;
            else if (side == NarrativePortraitSide.Center)
            {
                NarrativeRect left = _profile.GetRect(
                    NarrativeLayoutElement.LeftPortrait);
                left.x = 0.5f - left.width * 0.5f;
                left.Clamp();
                ApplyRect(_leftPortrait.rectTransform, left);
                _leftPortrait.sprite = portrait;
            }
            else if (side != NarrativePortraitSide.Hidden)
                _leftPortrait.sprite = portrait;
        }

        private Button CreateChoiceButton(int index, Font font)
        {
            GameObject buttonObject = CreateObject(
                $"Choice {index + 1}", _choiceRoot);
            LayoutElement sizing = buttonObject.AddComponent<LayoutElement>();
            sizing.preferredHeight = _profile.ChoiceHeight;
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = _profile.ChoiceButtonSprite;
            image.color = _profile.ChoiceColour;
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colours = button.colors;
            colours.normalColor = _profile.ChoiceColour;
            colours.highlightedColor = _profile.ChoiceHighlightColour;
            button.colors = colours;

            GameObject labelObject = CreateObject("Label", buttonObject.transform);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 6f);
            rect.offsetMax = new Vector2(-18f, -6f);
            Text text = labelObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = _profile.ChoiceFontSize;
            text.color = _profile.TextColour;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = _profile.ChoiceFontSize;
            return button;
        }

        private Image CreateImage(
            string objectName,
            NarrativeLayoutElement element,
            Transform parent,
            Color colour,
            bool preserveAspect = false)
        {
            GameObject child = CreateObject(objectName, parent);
            RectTransform rect = child.GetComponent<RectTransform>();
            ApplyRect(rect, _profile.GetRect(element));
            Image image = child.AddComponent<Image>();
            image.color = colour;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = element ==
                                  NarrativeLayoutElement.DialogueBox;
            return image;
        }

        private Text CreateText(
            string objectName,
            NarrativeLayoutElement element,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject child = CreateObject(objectName, parent);
            ApplyRect(
                child.GetComponent<RectTransform>(),
                _profile.GetRect(element));
            Text text = child.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = _profile.TextColour;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject(
            string objectName,
            Transform parent)
        {
            GameObject created = new GameObject(
                objectName,
                typeof(RectTransform));
            created.transform.SetParent(parent, false);
            return created;
        }

        private static void ApplyRect(
            RectTransform target,
            NarrativeRect source)
        {
            target.anchorMin = new Vector2(source.x, source.y);
            target.anchorMax = new Vector2(
                source.x + source.width,
                source.y + source.height);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private AudioSource CreateAudioSource(
            string sourceName,
            bool loop)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            return source;
        }

        private void HideChoices()
        {
            if (_choiceRoot != null)
                _choiceRoot.gameObject.SetActive(false);
        }

        private void SetInterfaceVisible(bool visible)
        {
            if (_background != null)
                _background.gameObject.SetActive(visible);
            if (_dialogueBox != null)
                _dialogueBox.gameObject.SetActive(visible);
            if (_speaker != null)
                _speaker.gameObject.SetActive(visible);
            if (_body != null)
                _body.gameObject.SetActive(visible);
        }

        private void HideInterface()
        {
            SetInterfaceVisible(false);

            if (_leftPortrait != null)
                _leftPortrait.gameObject.SetActive(false);
            if (_rightPortrait != null)
                _rightPortrait.gameObject.SetActive(false);

            HideChoices();
        }
    }
}
