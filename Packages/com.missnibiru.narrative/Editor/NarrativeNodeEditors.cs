using UnityEditor;
using UnityEngine;

namespace MissNibiru.Narrative.Editor
{
    [CustomEditor(typeof(NarrativeLineNode))]
    internal sealed class NarrativeLineNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "id",
                "editorPosition",
                "nextNodeId");

            SerializedProperty text = serializedObject.FindProperty("text");
            SerializedProperty limit =
                serializedObject.FindProperty("wordLimit");
            int words = NarrativeValidator.CountWords(text.stringValue);
            MessageType type = words > limit.intValue
                ? MessageType.Warning
                : MessageType.Info;
            EditorGUILayout.HelpBox(
                $"Words: {words}/{Mathf.Max(1, limit.intValue)}",
                type);

            if (serializedObject.ApplyModifiedProperties())
                NarrativeEditorEvents.RequestGraphRefresh();
        }
    }

    [CustomEditor(typeof(NarrativeChoiceNode))]
    internal sealed class NarrativeChoiceNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _prompt;
        private SerializedProperty _choices;

        private void OnEnable()
        {
            _prompt = serializedObject.FindProperty("prompt");
            _choices = serializedObject.FindProperty("choices");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_prompt);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                $"Choices ({_choices.arraySize}/5)",
                EditorStyles.boldLabel);

            int removeIndex = -1;

            for (int i = 0; i < _choices.arraySize; i++)
            {
                SerializedProperty choice =
                    _choices.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"Choice {i + 1}", EditorStyles.boldLabel);

                GUI.enabled = i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(26f)))
                    _choices.MoveArrayElement(i, i - 1);

                GUI.enabled = i < _choices.arraySize - 1;
                if (GUILayout.Button("↓", GUILayout.Width(26f)))
                    _choices.MoveArrayElement(i, i + 1);

                GUI.enabled = true;
                if (GUILayout.Button("×", GUILayout.Width(26f)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();

                SerializedProperty text =
                    choice.FindPropertyRelative("text");
                SerializedProperty limit =
                    choice.FindPropertyRelative("wordLimit");
                SerializedProperty condition =
                    choice.FindPropertyRelative("condition");
                EditorGUILayout.PropertyField(text);
                EditorGUILayout.PropertyField(limit);
                EditorGUILayout.PropertyField(condition, true);

                int words = NarrativeValidator.CountWords(text.stringValue);
                GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel);
                countStyle.normal.textColor = words > limit.intValue
                    ? new Color(1f, 0.55f, 0.25f)
                    : new Color(0.68f, 0.50f, 0.90f);
                EditorGUILayout.LabelField(
                    $"Words: {words}/{Mathf.Max(1, limit.intValue)}",
                    countStyle);
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
                _choices.DeleteArrayElementAtIndex(removeIndex);

            GUI.enabled = _choices.arraySize <
                          NarrativeChoiceNode.MaximumChoices;

            if (GUILayout.Button("Add Choice"))
            {
                int index = _choices.arraySize;
                _choices.arraySize++;
                SerializedProperty created =
                    _choices.GetArrayElementAtIndex(index);
                created.FindPropertyRelative("text").stringValue =
                    $"Choice {index + 1}";
                created.FindPropertyRelative("wordLimit").intValue = 12;
                created.FindPropertyRelative("targetNodeId").stringValue =
                    string.Empty;
            }

            GUI.enabled = true;

            if (serializedObject.ApplyModifiedProperties())
                NarrativeEditorEvents.RequestGraphRefresh();
        }
    }
}
