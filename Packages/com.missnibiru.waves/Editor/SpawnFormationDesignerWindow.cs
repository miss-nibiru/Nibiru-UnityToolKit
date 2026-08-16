using System.Collections.Generic;
using MissNibiru.Waves.Layouts;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Waves.Editor
{
    public sealed class SpawnFormationDesignerWindow : EditorWindow
    {
        private const int Radius = 4;
        private const int GridSize = Radius * 2 + 1;
        private const float CellSize = 30f;
        private const string BrandBannerPath =
            "Packages/com.missnibiru.core/Editor/Branding/" +
            "NibiruMainBanner.png";

        private static readonly Color HeaderColour =
            new Color(0.11f, 0.075f, 0.17f);

        [SerializeField]
        private SpawnFormationDefinition formation;

        private string _displayName;
        private Texture2D _brandBanner;
        private GUIStyle _headerTitle;
        private GUIStyle _headerSubtitle;

        public static void Open(SpawnFormationDefinition target)
        {
            if (target == null)
                return;

            SpawnFormationDesignerWindow window =
                GetWindow<SpawnFormationDesignerWindow>(true);

            window.formation = target;
            window._displayName = target.DisplayName;
            window.titleContent = new GUIContent("Formation Designer");
            window.minSize = new Vector2(380f, 570f);
            window.maxSize = new Vector2(620f, 780f);
            window.Show();
        }

        private void OnEnable()
        {
            _brandBanner = AssetDatabase.LoadAssetAtPath<Texture2D>(
                BrandBannerPath);

            if (formation != null)
                _displayName = formation.DisplayName;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();

            if (formation == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a formation asset.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4f);

            string nextName = EditorGUILayout.TextField(
                "Name",
                _displayName);

            if (nextName != _displayName)
            {
                _displayName = nextName;
                Apply(CurrentOffsets(), "Rename formation");
            }

            DrawPresetButtons();
            EditorGUILayout.Space(4f);
            DrawGrid();
            EditorGUILayout.Space(4f);
            DrawTransformButtons();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear"))
                    Apply(new[] { Vector2Int.zero }, "Clear formation");

                if (GUILayout.Button("Save"))
                {
                    EditorUtility.SetDirty(formation);
                    AssetDatabase.SaveAssets();
                    SceneView.RepaintAll();
                }
            }
        }

        private void DrawHeader()
        {
            Rect header = GUILayoutUtility.GetRect(
                0f,
                136f,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(header, HeaderColour);

            Rect banner = new Rect(
                header.x + 8f,
                header.y + 4f,
                header.width - 16f,
                96f);

            if (_brandBanner != null)
            {
                GUI.DrawTexture(
                    banner,
                    _brandBanner,
                    ScaleMode.ScaleToFit,
                    true);
            }

            GUI.Label(
                new Rect(
                    header.x + 8f,
                    header.y + 99f,
                    header.width - 16f,
                    23f),
                "Formation Designer",
                _headerTitle);

            GUI.Label(
                new Rect(
                    header.x + 8f,
                    header.y + 120f,
                    header.width - 16f,
                    16f),
                "Click cells to toggle spawn points.",
                _headerSubtitle);
        }

        private void EnsureStyles()
        {
            if (_headerTitle != null)
                return;

            _headerTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };
            _headerTitle.normal.textColor = Color.white;

            _headerSubtitle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _headerSubtitle.normal.textColor =
                new Color(0.78f, 0.74f, 0.84f);
        }

        private void DrawPresetButtons()
        {
            GUILayout.Label("Presets", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Single"))
                    Apply(Single(), "Use single formation");

                if (GUILayout.Button("Horizontal"))
                    Apply(Horizontal(), "Use horizontal formation");

                if (GUILayout.Button("Vertical"))
                    Apply(Vertical(), "Use vertical formation");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cluster"))
                    Apply(Cluster(), "Use cluster formation");

                if (GUILayout.Button("Circle"))
                    Apply(Circle(), "Use circle formation");
            }
        }

        private void DrawGrid()
        {
            HashSet<Vector2Int> offsets =
                new HashSet<Vector2Int>(CurrentOffsets());

            float width = GridSize * CellSize;
            Rect area = GUILayoutUtility.GetRect(
                width,
                width,
                GUILayout.ExpandWidth(false));

            area.x = (position.width - width) * 0.5f;

            Event current = Event.current;

            for (int visualY = 0;
                 visualY < GridSize;
                 visualY++)
            {
                for (int visualX = 0;
                     visualX < GridSize;
                     visualX++)
                {
                    Vector2Int offset = new Vector2Int(
                        visualX - Radius,
                        Radius - visualY);

                    Rect cell = new Rect(
                        area.x + visualX * CellSize,
                        area.y + visualY * CellSize,
                        CellSize - 1f,
                        CellSize - 1f);

                    bool active = offsets.Contains(offset);
                    Color colour = active
                        ? new Color(0.58f, 0.25f, 0.78f)
                        : new Color(0.18f, 0.18f, 0.20f);

                    if (offset == Vector2Int.zero)
                    {
                        colour = active
                            ? new Color(0.22f, 0.72f, 0.42f)
                            : new Color(0.27f, 0.31f, 0.28f);
                    }

                    EditorGUI.DrawRect(cell, colour);

                    if (offset == Vector2Int.zero)
                    {
                        GUI.Label(
                            cell,
                            "0",
                            new GUIStyle(EditorStyles.miniLabel)
                            {
                                alignment = TextAnchor.MiddleCenter
                            });
                    }

                    if (current.type == EventType.MouseDown &&
                        current.button == 0 &&
                        cell.Contains(current.mousePosition))
                    {
                        if (active)
                            offsets.Remove(offset);
                        else
                            offsets.Add(offset);

                        if (offsets.Count == 0)
                            offsets.Add(Vector2Int.zero);

                        Apply(
                            new List<Vector2Int>(offsets).ToArray(),
                            "Edit formation cells");
                        current.Use();
                        Repaint();
                    }
                }
            }
        }

        private void DrawTransformButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rotate Left"))
                    TransformOffsets(-1, false, false);

                if (GUILayout.Button("Rotate Right"))
                    TransformOffsets(1, false, false);

                if (GUILayout.Button("Flip X"))
                    TransformOffsets(0, true, false);

                if (GUILayout.Button("Flip Y"))
                    TransformOffsets(0, false, true);
            }
        }

        private void TransformOffsets(
            int rotation,
            bool flipX,
            bool flipY)
        {
            Vector2Int[] source = CurrentOffsets();
            Vector2Int[] result = new Vector2Int[source.Length];

            for (int index = 0; index < source.Length; index++)
            {
                Vector2Int value = source[index];

                if (flipX)
                    value.x = -value.x;

                if (flipY)
                    value.y = -value.y;

                if (rotation < 0)
                    value = new Vector2Int(value.y, -value.x);
                else if (rotation > 0)
                    value = new Vector2Int(-value.y, value.x);

                result[index] = value;
            }

            Apply(result, "Transform formation");
        }

        private Vector2Int[] CurrentOffsets()
        {
            Vector2Int[] offsets = formation.CellOffsets;
            return offsets == null || offsets.Length == 0
                ? new[] { Vector2Int.zero }
                : offsets;
        }

        private void Apply(Vector2Int[] offsets, string undoName)
        {
            Undo.RecordObject(formation, undoName);
            formation.Configure(_displayName, offsets);
            EditorUtility.SetDirty(formation);
            SceneView.RepaintAll();
        }

        private static Vector2Int[] Single()
        {
            return new[] { Vector2Int.zero };
        }

        private static Vector2Int[] Horizontal()
        {
            return new[]
            {
                new Vector2Int(-1, 0),
                Vector2Int.zero,
                new Vector2Int(1, 0)
            };
        }

        private static Vector2Int[] Vertical()
        {
            return new[]
            {
                new Vector2Int(0, -1),
                Vector2Int.zero,
                new Vector2Int(0, 1)
            };
        }

        private static Vector2Int[] Cluster()
        {
            List<Vector2Int> result = new List<Vector2Int>();

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                    result.Add(new Vector2Int(x, y));
            }

            return result.ToArray();
        }

        private static Vector2Int[] Circle()
        {
            return new[]
            {
                new Vector2Int(-1, -1),
                new Vector2Int(0, -2),
                new Vector2Int(1, -1),
                new Vector2Int(2, 0),
                new Vector2Int(1, 1),
                new Vector2Int(0, 2),
                new Vector2Int(-1, 1),
                new Vector2Int(-2, 0)
            };
        }
    }
}
