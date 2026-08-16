using System;
using System.IO;
using MissNibiru.Narrative;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Narrative.Editor
{
    public static class NarrativeAssetFactory
    {
        public static NarrativeStory CreateStory(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            string folder = Path.GetDirectoryName(assetPath)
                ?.Replace('\\', '/') ?? "Assets";
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            DialoguePresentationProfile profile =
                ScriptableObject.CreateInstance<DialoguePresentationProfile>();
            profile.name = fileName + " Presentation";
            string profilePath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{fileName}_Presentation.asset");
            AssetDatabase.CreateAsset(profile, profilePath);

            NarrativeStory story =
                ScriptableObject.CreateInstance<NarrativeStory>();
            story.name = fileName;
            story.Configure(CleanId(fileName), fileName, profile);
            AssetDatabase.CreateAsset(story, assetPath);

            NarrativeStartNode start = AddNode<NarrativeStartNode>(
                story, new Vector2(80f, 180f), false);
            NarrativeEndNode end = AddNode<NarrativeEndNode>(
                story, new Vector2(480f, 180f), false);
            start.SetNextNodeId(end.Id);
            story.SetStartNode(start);

            EditorUtility.SetDirty(start);
            EditorUtility.SetDirty(story);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            Selection.activeObject = story;
            return story;
        }

        public static T AddNode<T>(
            NarrativeStory story,
            Vector2 position,
            bool save = true)
            where T : NarrativeNode
        {
            if (story == null)
                return null;

            T node = ScriptableObject.CreateInstance<T>();
            node.name = ObjectNames.NicifyVariableName(typeof(T).Name
                .Replace("Narrative", string.Empty)
                .Replace("Node", string.Empty));
            node.Initialize(CreateNodeId(story, typeof(T).Name), position);

            if (node is NarrativeLineNode)
            {
                SerializedObject serializedNode = new SerializedObject(node);
                serializedNode.FindProperty("wordLimit").intValue =
                    story.DefaultLineWordLimit;
                serializedNode.ApplyModifiedPropertiesWithoutUndo();
            }

            Undo.RegisterCreatedObjectUndo(node, "Create Narrative Node");
            Undo.RecordObject(story, "Add Narrative Node");
            story.AddNode(node);
            AssetDatabase.AddObjectToAsset(node, story);
            EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(story);

            if (save)
                AssetDatabase.SaveAssets();

            return node;
        }

        public static NarrativeNode DuplicateNode(
            NarrativeStory story,
            NarrativeNode source)
        {
            if (story == null || source == null ||
                source is NarrativeStartNode)
            {
                return null;
            }

            NarrativeNode duplicate = UnityEngine.Object.Instantiate(source);
            duplicate.name = source.name + " Copy";
            duplicate.Initialize(
                CreateNodeId(story, source.GetType().Name),
                source.EditorPosition + new Vector2(40f, 40f));
            NarrativeNodeConnectionUtility.ClearAllTargets(duplicate);

            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Narrative Node");
            Undo.RecordObject(story, "Duplicate Narrative Node");
            story.AddNode(duplicate);
            AssetDatabase.AddObjectToAsset(duplicate, story);
            EditorUtility.SetDirty(duplicate);
            EditorUtility.SetDirty(story);
            AssetDatabase.SaveAssets();
            return duplicate;
        }

        public static bool DeleteNode(
            NarrativeStory story,
            NarrativeNode node)
        {
            if (story == null || node == null ||
                node is NarrativeStartNode)
            {
                return false;
            }

            Undo.RecordObject(story, "Delete Narrative Node");

            foreach (NarrativeNode other in story.Nodes)
            {
                if (other == null || other == node)
                    continue;

                Undo.RecordObject(other, "Remove Narrative Link");
            }

            NarrativeNodeConnectionUtility.ClearReferencesTo(
                story.Nodes, node.Id);
            story.RemoveNode(node);
            EditorUtility.SetDirty(story);
            Undo.DestroyObjectImmediate(node);
            AssetDatabase.SaveAssets();
            return true;
        }

        public static T CreateLibraryAsset<T>(
            string assetPath,
            NarrativeStory story = null)
            where T : ScriptableObject
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            ConfigureAsset(asset, asset.name);
            AssetDatabase.CreateAsset(asset, assetPath);

            if (story != null)
                RegisterWithStory(story, asset);

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            return asset;
        }

        public static void RegisterWithStory(
            NarrativeStory story,
            UnityEngine.Object asset)
        {
            if (story == null || asset == null)
                return;

            string propertyName = GetLibraryProperty(asset);

            if (string.IsNullOrEmpty(propertyName))
                return;

            Undo.RecordObject(story, "Register Narrative Asset");
            SerializedObject serialized = new SerializedObject(story);
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
                return;

            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i)
                        .objectReferenceValue == asset)
                {
                    return;
                }
            }

            property.InsertArrayElementAtIndex(property.arraySize);
            property.GetArrayElementAtIndex(property.arraySize - 1)
                .objectReferenceValue = asset;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(story);
        }

        private static string GetLibraryProperty(UnityEngine.Object asset)
        {
            if (asset is NarrativeCharacter)
                return "characters";
            if (asset is NarrativeVariable)
                return "variables";
            if (asset is NarrativeFlag)
                return "flags";
            if (asset is NarrativeEvent)
                return "gameplayEvents";
            return string.Empty;
        }

        private static void ConfigureAsset(
            ScriptableObject asset,
            string visibleName)
        {
            string id = CleanId(visibleName);

            if (asset is NarrativeCharacter character)
                character.Configure(id, visibleName);
            else if (asset is NarrativeEmotion emotion)
                emotion.Configure(id, visibleName);
            else if (asset is NarrativeVariable variable)
            {
                variable.Configure(
                    id, visibleName, NarrativeVariableType.Boolean);
            }
            else if (asset is NarrativeFlag flag)
                flag.Configure(id, visibleName);
            else if (asset is NarrativeEvent gameplayEvent)
                gameplayEvent.Configure(id, visibleName);
        }

        private static string CreateNodeId(
            NarrativeStory story,
            string typeName)
        {
            string prefix = CleanId(typeName
                .Replace("Narrative", string.Empty)
                .Replace("Node", string.Empty));
            string candidate;

            do
            {
                candidate = prefix + "_" +
                    Guid.NewGuid().ToString("N").Substring(0, 8);
            }
            while (story.FindNode(candidate) != null);

            return candidate;
        }

        private static string CleanId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "asset"
                : value.Trim().ToLowerInvariant()
                    .Replace(' ', '_')
                    .Replace('-', '_');
        }
    }
}
