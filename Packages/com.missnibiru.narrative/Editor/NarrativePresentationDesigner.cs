using System;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Narrative.Editor
{
    internal sealed class NarrativePresentationDesigner
    {
        private NarrativeLayoutElement _selected =
            NarrativeLayoutElement.DialogueBox;
        private bool _dragging;
        private bool _resizing;
        private Vector2 _lastMouse;
        private UnityEditor.Editor _profileEditor;
        private Vector2 _inspectorScroll;

        public void Draw(
            DialoguePresentationProfile profile,
            NarrativeLineNode previewLine)
        {
            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a presentation profile.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawPalette();
            DrawCanvasColumn(profile, previewLine);
            DrawInspector(profile);
            EditorGUILayout.EndHorizontal();
        }

        public void Dispose()
        {
            if (_profileEditor != null)
                UnityEngine.Object.DestroyImmediate(_profileEditor);

            _profileEditor = null;
        }

        private void DrawPalette()
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.Width(155f),
                GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Screen Elements", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Drag to position.", EditorStyles.miniLabel);
            EditorGUILayout.Space(5f);

            foreach (NarrativeLayoutElement element in
                     Enum.GetValues(typeof(NarrativeLayoutElement)))
            {
                bool selected = element == _selected;
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = selected
                    ? new Color(0.62f, 0.32f, 0.86f)
                    : previous;

                if (GUILayout.Button(
                        ObjectNames.NicifyVariableName(element.ToString()),
                        GUILayout.Height(26f)))
                {
                    _selected = element;
                }

                GUI.backgroundColor = previous;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(
                "Corner handle resizes.",
                MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawCanvasColumn(
            DialoguePresentationProfile profile,
            NarrativeLineNode line)
        {
            EditorGUILayout.BeginVertical(
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField(
                "Player View · 16:9",
                EditorStyles.boldLabel);
            Rect region = GUILayoutUtility.GetRect(
                300f,
                10000f,
                220f,
                10000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            Rect canvas = FitAspect(region, 16f / 9f);
            EditorGUI.DrawRect(region, new Color(0.035f, 0.025f, 0.05f));
            EditorGUI.DrawRect(canvas, profile.PreviewBackground);

            DrawPreviewBackground(canvas, profile, line);
            DrawElement(canvas, profile, NarrativeLayoutElement.LeftPortrait,
                "Left Portrait", line);
            DrawElement(canvas, profile, NarrativeLayoutElement.RightPortrait,
                "Right Portrait", line);
            DrawElement(canvas, profile, NarrativeLayoutElement.DialogueBox,
                "Dialogue Box", line);
            DrawElement(canvas, profile, NarrativeLayoutElement.SpeakerName,
                line?.Character == null ? "Speaker" :
                    line.Character.DisplayName, line);
            DrawElement(canvas, profile, NarrativeLayoutElement.BodyText,
                line == null ? "Dialogue preview" : line.Text, line);
            DrawElement(canvas, profile, NarrativeLayoutElement.Choices,
                "Choices (up to 5)", line);
            DrawSelectedOutline(canvas, profile);
            HandleDrag(canvas, profile);
            EditorGUILayout.EndVertical();
        }

        private void DrawInspector(DialoguePresentationProfile profile)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.Width(285f),
                GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField(
                ObjectNames.NicifyVariableName(_selected.ToString()),
                EditorStyles.boldLabel);

            NarrativeRect rect = profile.GetRect(_selected);
            EditorGUI.BeginChangeCheck();
            rect.x = EditorGUILayout.Slider("X", rect.x, 0f, 1f);
            rect.y = EditorGUILayout.Slider("Y", rect.y, 0f, 1f);
            rect.width = EditorGUILayout.Slider("Width", rect.width, 0.01f, 1f);
            rect.height = EditorGUILayout.Slider("Height", rect.height, 0.01f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(profile, "Edit Narrative Layout");
                profile.SetRect(_selected, rect);
                EditorUtility.SetDirty(profile);
            }

            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField("Profile Style", EditorStyles.boldLabel);
            _inspectorScroll = EditorGUILayout.BeginScrollView(
                _inspectorScroll,
                GUILayout.ExpandHeight(true));
            UnityEditor.Editor.CreateCachedEditor(
                profile, null, ref _profileEditor);
            _profileEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewBackground(
            Rect canvas,
            DialoguePresentationProfile profile,
            NarrativeLineNode line)
        {
            if (line?.Background != null)
            {
                Rect target = ToGuiRect(
                    canvas,
                    profile.GetRect(NarrativeLayoutElement.Background));
                GUI.DrawTexture(
                    target,
                    line.Background.texture,
                    ScaleMode.ScaleAndCrop);
            }
        }

        private void DrawElement(
            Rect canvas,
            DialoguePresentationProfile profile,
            NarrativeLayoutElement element,
            string label,
            NarrativeLineNode line)
        {
            Rect rect = ToGuiRect(canvas, profile.GetRect(element));

            if ((element == NarrativeLayoutElement.LeftPortrait ||
                 element == NarrativeLayoutElement.RightPortrait) &&
                line?.Character != null)
            {
                bool matches =
                    element == NarrativeLayoutElement.LeftPortrait &&
                    line.PortraitSide == NarrativePortraitSide.Left ||
                    element == NarrativeLayoutElement.RightPortrait &&
                    line.PortraitSide == NarrativePortraitSide.Right;
                Sprite portrait = line.Character.GetPortrait(line.Emotion);

                if (matches && portrait != null)
                {
                    Rect textureRect = portrait.textureRect;
                    Rect uv = new Rect(
                        textureRect.x / portrait.texture.width,
                        textureRect.y / portrait.texture.height,
                        textureRect.width / portrait.texture.width,
                        textureRect.height / portrait.texture.height);
                    GUI.DrawTextureWithTexCoords(rect, portrait.texture, uv);
                }
            }

            Color fill = GetElementColour(profile, element);
            EditorGUI.DrawRect(rect, fill);
            GUIStyle style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = element == NarrativeLayoutElement.BodyText
                    ? TextAnchor.UpperLeft
                    : TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = element == NarrativeLayoutElement.BodyText
                    ? 11
                    : 10
            };
            style.normal.textColor = profile.TextColour;
            GUI.Label(new Rect(
                rect.x + 5f,
                rect.y + 3f,
                rect.width - 10f,
                rect.height - 6f), label, style);
        }

        private void DrawSelectedOutline(
            Rect canvas,
            DialoguePresentationProfile profile)
        {
            Rect rect = ToGuiRect(canvas, profile.GetRect(_selected));
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(
                rect,
                Color.clear,
                new Color(0.92f, 0.38f, 1f));
            Handles.EndGUI();

            Rect handle = new Rect(
                rect.xMax - 9f,
                rect.yMax - 9f,
                12f,
                12f);
            EditorGUI.DrawRect(handle, new Color(0.92f, 0.38f, 1f));
        }

        private void HandleDrag(
            Rect canvas,
            DialoguePresentationProfile profile)
        {
            Event current = Event.current;
            Rect selected = ToGuiRect(canvas, profile.GetRect(_selected));
            Rect handle = new Rect(
                selected.xMax - 12f,
                selected.yMax - 12f,
                18f,
                18f);

            if (current.type == EventType.MouseDown &&
                current.button == 0 && selected.Contains(current.mousePosition))
            {
                Undo.RecordObject(profile, "Move Narrative Layout");
                _dragging = true;
                _resizing = handle.Contains(current.mousePosition);
                _lastMouse = current.mousePosition;
                GUIUtility.hotControl = GUIUtility.GetControlID(
                    FocusType.Passive);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && _dragging)
            {
                Vector2 delta = current.mousePosition - _lastMouse;
                _lastMouse = current.mousePosition;
                NarrativeRect value = profile.GetRect(_selected);

                if (_resizing)
                {
                    value.width += delta.x / canvas.width;
                    value.height += delta.y / canvas.height;
                    value.y -= delta.y / canvas.height;
                }
                else
                {
                    value.x += delta.x / canvas.width;
                    value.y -= delta.y / canvas.height;
                }

                value.Clamp();
                profile.SetRect(_selected, value);
                EditorUtility.SetDirty(profile);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && _dragging)
            {
                _dragging = false;
                _resizing = false;
                GUIUtility.hotControl = 0;
                current.Use();
            }
        }

        private static Color GetElementColour(
            DialoguePresentationProfile profile,
            NarrativeLayoutElement element)
        {
            switch (element)
            {
                case NarrativeLayoutElement.DialogueBox:
                    return profile.DialogueBoxColour;
                case NarrativeLayoutElement.Choices:
                    return new Color(
                        profile.ChoiceColour.r,
                        profile.ChoiceColour.g,
                        profile.ChoiceColour.b,
                        0.55f);
                case NarrativeLayoutElement.SpeakerName:
                    return new Color(0.32f, 0.12f, 0.48f, 0.78f);
                case NarrativeLayoutElement.BodyText:
                    return new Color(0.12f, 0.07f, 0.19f, 0.35f);
                default:
                    return new Color(0.34f, 0.18f, 0.48f, 0.20f);
            }
        }

        private static Rect FitAspect(Rect bounds, float aspect)
        {
            float width = bounds.width;
            float height = width / aspect;

            if (height > bounds.height)
            {
                height = bounds.height;
                width = height * aspect;
            }

            return new Rect(
                bounds.x + (bounds.width - width) * 0.5f,
                bounds.y + (bounds.height - height) * 0.5f,
                width,
                height);
        }

        private static Rect ToGuiRect(
            Rect canvas,
            NarrativeRect value)
        {
            return new Rect(
                canvas.x + value.x * canvas.width,
                canvas.y + (1f - value.y - value.height) * canvas.height,
                value.width * canvas.width,
                value.height * canvas.height);
        }
    }
}
