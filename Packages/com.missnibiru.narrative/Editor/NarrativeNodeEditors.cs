using MissNibiru.Narrative;
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

            NarrativeLineNode line = target as NarrativeLineNode;
            bool flatten = false;

            if (line != null && line.UseImportedSegments)
            {
                EditorGUILayout.HelpBox(
                    "Imported conditions control this text. Flatten it to edit as one unconditional line.",
                    MessageType.Info);
                flatten = GUILayout.Button("Flatten Imported Text");
            }

            serializedObject.ApplyModifiedProperties();

            if (flatten)
            {
                Undo.RecordObject(line, "Flatten Imported Narrative Text");
                line.FlattenImportedText();
                EditorUtility.SetDirty(line);
            }
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
            bool structureChanged = false;
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
                {
                    _choices.MoveArrayElement(i, i - 1);
                    structureChanged = true;
                }

                GUI.enabled = i < _choices.arraySize - 1;
                if (GUILayout.Button("↓", GUILayout.Width(26f)))
                {
                    _choices.MoveArrayElement(i, i + 1);
                    structureChanged = true;
                }

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

                NarrativeChoiceNode choiceNode =
                    target as NarrativeChoiceNode;
                NarrativeChoiceOption model = choiceNode?.GetChoice(i);

                if (model?.ImportedCondition != null &&
                    !model.ImportedCondition.IsEmpty)
                {
                    EditorGUILayout.HelpBox(
                        "Imported Twee condition active.",
                        MessageType.Info);

                    if (GUILayout.Button("Remove Imported Condition"))
                    {
                        SerializedProperty imported =
                            choice.FindPropertyRelative(
                                "importedCondition");
                        SerializedProperty tokens = imported == null
                            ? null
                            : imported.FindPropertyRelative("tokens");

                        if (tokens != null)
                            tokens.arraySize = 0;
                    }
                }

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
            {
                _choices.DeleteArrayElementAtIndex(removeIndex);
                structureChanged = true;
            }

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
                structureChanged = true;
            }

            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();

            if (structureChanged)
                NarrativeEditorEvents.RequestGraphRefresh();
        }
    }

    [CustomEditor(typeof(NarrativeConditionNode))]
    internal sealed class NarrativeConditionNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "id",
                "editorPosition",
                "importedCondition",
                "trueNodeId",
                "falseNodeId");
            NarrativeConditionNode node = target as NarrativeConditionNode;

            if (node?.ImportedCondition != null &&
                !node.ImportedCondition.IsEmpty)
            {
                EditorGUILayout.HelpBox(
                    "Imported Twee condition active.",
                    MessageType.Info);

                if (GUILayout.Button("Remove Imported Condition"))
                {
                    SerializedProperty imported = serializedObject
                        .FindProperty("importedCondition");
                    SerializedProperty tokens = imported?
                        .FindPropertyRelative("tokens");

                    if (tokens != null)
                        tokens.arraySize = 0;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
