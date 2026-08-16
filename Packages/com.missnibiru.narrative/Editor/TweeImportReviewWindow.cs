using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Narrative.Editor
{
    internal sealed class TweeImportReviewDecision
    {
        public bool Accepted;
        public TweeImportProfile Profile;
    }

    internal sealed class TweeImportReviewWindow : EditorWindow
    {
        private const string BannerPath =
            "Packages/com.missnibiru.narrative/Editor/Branding/NibiruMainBanner.png";

        private TweeImportAnalysis _analysis;
        private TweeImportProfile _profile;
        private UnityEditor.Editor _profileEditor;
        private Vector2 _scroll;
        private TweeImportReviewDecision _decision;

        public static TweeImportReviewDecision ShowReview(
            string sourcePath,
            TweeImportProfile initialProfile = null)
        {
            TweeImportReviewWindow window =
                CreateInstance<TweeImportReviewWindow>();
            window.titleContent = new GUIContent("Review Twee Import");
            window.minSize = new Vector2(660f, 620f);
            window.maxSize = new Vector2(900f, 900f);
            window._analysis = TweeImportAnalyzer.AnalyzeFile(sourcePath);
            window._profile = initialProfile;
            window._decision = new TweeImportReviewDecision();
            window.ShowModalUtility();
            return window._decision;
        }

        private void OnDisable()
        {
            if (_profileEditor != null)
                DestroyImmediate(_profileEditor);
        }

        private void OnGUI()
        {
            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSummary();
            EditorGUILayout.Space(8f);
            DrawProfile();
            EditorGUILayout.Space(8f);
            DrawDetectedMappings();
            EditorGUILayout.EndScrollView();
            DrawActions();
        }

        private void DrawHeader()
        {
            Texture2D banner = AssetDatabase.LoadAssetAtPath<Texture2D>(
                BannerPath);

            if (banner != null)
            {
                Rect rect = GUILayoutUtility.GetRect(
                    100f, 94f, GUILayout.ExpandWidth(true));
                GUI.DrawTexture(rect, banner, ScaleMode.ScaleAndCrop);
            }

            EditorGUILayout.LabelField(
                "Twee Import Review",
                new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    normal = { textColor = new Color(0.82f, 0.61f, 1f) }
                });
            EditorGUILayout.LabelField(
                "Confirm what Unity will generate.",
                EditorStyles.miniLabel);
        }

        private void DrawSummary()
        {
            EditorGUILayout.LabelField(
                "Pre-import Summary",
                EditorStyles.boldLabel);

            if (_analysis == null)
            {
                EditorGUILayout.HelpBox(
                    "No analysis is available.",
                    MessageType.Error);
                return;
            }

            DrawCount("Passages", _analysis.PassageCount);
            DrawCount("Dialogue lines", _analysis.DialogueLineCount);
            DrawCount("Narrator lines", _analysis.NarratorLineCount);
            DrawCount("Character lines", _analysis.CharacterLineCount);
            DrawCount("Choices", _analysis.ChoiceCount);
            DrawCount("Mutations", _analysis.MutationCount);
            DrawCount("Detected colours", _analysis.DetectedColours.Count);
            DrawCount("Audio definitions", _analysis.AudioDefinitionCount);
            DrawCount("Audio uses", _analysis.AudioUsageCount);

            foreach (TweeImportIssue issue in _analysis.Issues)
            {
                MessageType type = issue.Severity ==
                                   TweeImportIssueSeverity.Error
                    ? MessageType.Error
                    : issue.Severity == TweeImportIssueSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;
                EditorGUILayout.HelpBox(issue.ToString(), type);
            }
        }

        private void DrawProfile()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "Import Profile",
                EditorStyles.boldLabel,
                GUILayout.Width(105f));
            TweeImportProfile selected =
                (TweeImportProfile)EditorGUILayout.ObjectField(
                    _profile,
                    typeof(TweeImportProfile),
                    false);

            if (selected != _profile)
            {
                _profile = selected;

                if (_profileEditor != null)
                    DestroyImmediate(_profileEditor);

                _profileEditor = null;
            }

            if (GUILayout.Button("Create", GUILayout.Width(72f)))
                CreateDetectedProfile();

            EditorGUILayout.EndHorizontal();

            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "No profile: placeholder speakers are created.",
                    MessageType.Info);
                return;
            }

            if (_profileEditor == null || _profileEditor.target != _profile)
                _profileEditor = UnityEditor.Editor.CreateEditor(_profile);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _profileEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetectedMappings()
        {
            EditorGUILayout.LabelField(
                "Detected Mappings",
                EditorStyles.boldLabel);

            foreach (string colour in _analysis.DetectedColours)
            {
                TweeSpeakerMapping mapping = _profile?.FindSpeaker(colour);
                string target = mapping == null
                    ? "new placeholder"
                    : mapping.Character != null
                        ? mapping.Character.DisplayName
                        : mapping.DisplayName + " placeholder";
                Color old = GUI.color;
                GUI.color = ColorUtility.TryParseHtmlString(
                    colour, out Color parsed)
                    ? parsed
                    : old;
                EditorGUILayout.LabelField(colour, target);
                GUI.color = old;
            }

            if (_analysis.DetectedAudioKeys.Count > 0)
            {
                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField(
                    "Detected Audio",
                    EditorStyles.boldLabel);

                foreach (string key in _analysis.DetectedAudioKeys)
                {
                    TweeAudioMapping mapping = _profile?.FindAudio(key);
                    TweeAudioDefinitionData detected =
                        _analysis.AudioDefinitions.FirstOrDefault(
                            definition => string.Equals(
                                definition.Key,
                                key,
                                StringComparison.OrdinalIgnoreCase));
                    string source = detected == null ||
                                    string.IsNullOrWhiteSpace(
                                        detected.SourcePath)
                        ? string.Empty
                        : " · " + detected.SourcePath;
                    EditorGUILayout.LabelField(
                        key,
                        mapping?.Clip == null
                            ? "unmapped" + source
                            : mapping.Role + ": " + mapping.Clip.name + source);
                }
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(90f)))
            {
                _decision.Accepted = false;
                Close();
            }

            GUI.enabled = _analysis != null && !_analysis.Issues.Any(issue =>
                issue.Severity == TweeImportIssueSeverity.Error);

            if (GUILayout.Button("Import", GUILayout.Width(120f)))
            {
                if (_profile != null)
                    AssetDatabase.SaveAssetIfDirty(_profile);

                _decision.Accepted = true;
                _decision.Profile = _profile;
                Close();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void CreateDetectedProfile()
        {
            string defaultName = Path.GetFileNameWithoutExtension(
                _analysis.SourcePath) + "_ImportProfile";
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Twee Import Profile",
                defaultName,
                "asset",
                "Save reusable mappings.");

            if (string.IsNullOrWhiteSpace(path))
                return;

            TweeSpeakerMapping[] speakers =
                new TweeSpeakerMapping[_analysis.DetectedColours.Count];

            for (int i = 0; i < speakers.Length; i++)
            {
                string colour = _analysis.DetectedColours[i];
                speakers[i] = new TweeSpeakerMapping();
                speakers[i].Configure(
                    "Speaker " + colour.TrimStart('#'),
                    new[] { colour });
            }

            TweeAudioMapping[] audio =
                new TweeAudioMapping[_analysis.DetectedAudioKeys.Count];

            for (int i = 0; i < audio.Length; i++)
            {
                string key = _analysis.DetectedAudioKeys[i];
                audio[i] = new TweeAudioMapping();
                audio[i].Configure(key, null, GuessAudioRole(key));
            }

            _profile = NarrativeAssetFactory.CreateLibraryAsset<
                TweeImportProfile>(path);
            _profile.Configure(
                defaultName,
                ObjectNames.NicifyVariableName(defaultName),
                speakers,
                audio);
            EditorUtility.SetDirty(_profile);
            AssetDatabase.SaveAssets();

            if (_profileEditor != null)
                DestroyImmediate(_profileEditor);

            _profileEditor = null;
        }

        private static TweeAudioRole GuessAudioRole(string key)
        {
            string value = (key ?? string.Empty).ToLowerInvariant();

            if (value.Contains("music") || value.Contains("bgm") ||
                value.Contains("theme"))
            {
                return TweeAudioRole.Music;
            }

            if (value.Contains("sfx") || value.Contains("sound") ||
                value.Contains("effect"))
            {
                return TweeAudioRole.SoundEffect;
            }

            return TweeAudioRole.Voice;
        }

        private static void DrawCount(string label, int value)
        {
            EditorGUILayout.LabelField(label, value.ToString());
        }
    }
}
