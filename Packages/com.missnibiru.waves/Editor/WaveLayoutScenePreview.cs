using System.Collections.Generic;
using MissNibiru.Waves.Layouts;
using MissNibiru.Waves.Planning;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Waves.Editor
{
    [InitializeOnLoad]
    public static class WaveLayoutScenePreview
    {
        private static readonly Color GridColour =
            new Color(0.50f, 0.30f, 0.74f, 0.70f);

        private static readonly Color PlacementColour =
            new Color(0.14f, 0.80f, 0.50f, 0.88f);

        static WaveLayoutScenePreview()
        {
            SceneView.duringSceneGui += DrawScene;
        }

        private static void DrawScene(SceneView sceneView)
        {
            WaveLayoutBuilderWindow window =
                WaveLayoutBuilderWindow.ActiveWindow;

            if (window == null ||
                window.ActiveLayout == null ||
                window.ActiveOrigin == null)
            {
                return;
            }

            WaveLayoutData layout = window.ActiveLayout;
            int waveIndex = window.ActiveWaveIndex;

            if (waveIndex < 0 ||
                waveIndex >= layout.Waves.Count ||
                layout.Waves[waveIndex] == null)
            {
                return;
            }

            Transform origin = window.ActiveOrigin;
            DrawGrid(layout, origin);
            DrawPlacements(window, layout, origin, waveIndex);

            Handles.Label(
                origin.position,
                $"Wave Grid {layout.Columns} × {layout.Rows}");
        }

        private static void DrawGrid(
            WaveLayoutData layout,
            Transform origin)
        {
            Handles.color = GridColour;

            for (int column = 0;
                 column <= layout.Columns;
                 column++)
            {
                Vector3 start = CornerToWorld(
                    layout,
                    origin,
                    column,
                    0);
                Vector3 end = CornerToWorld(
                    layout,
                    origin,
                    column,
                    layout.Rows);
                Handles.DrawLine(start, end);
            }

            for (int row = 0; row <= layout.Rows; row++)
            {
                Vector3 start = CornerToWorld(
                    layout,
                    origin,
                    0,
                    row);
                Vector3 end = CornerToWorld(
                    layout,
                    origin,
                    layout.Columns,
                    row);
                Handles.DrawLine(start, end);
            }
        }

        private static void DrawPlacements(
            WaveLayoutBuilderWindow window,
            WaveLayoutData layout,
            Transform origin,
            int waveIndex)
        {
            Handles.color = PlacementColour;

            foreach (
                WaveLayoutPlacement placement
                in layout.Waves[waveIndex].Placements)
            {
                if (placement == null ||
                    !placement.Enabled ||
                    placement.Spawnable == null)
                {
                    continue;
                }

                IReadOnlyList<Vector2Int> formationCells =
                    WaveLayoutGeometry.GetFormationCells(placement);

                foreach (Vector2Int formationCell in formationCells)
                {
                    IReadOnlyList<Vector2Int> occupied =
                        WaveLayoutGeometry.GetOccupiedCells(
                            placement.Spawnable,
                            formationCell,
                            placement.Rotation,
                            placement.FlipHorizontal,
                            placement.FlipVertical);

                    foreach (Vector2Int cell in occupied)
                        DrawCell(layout, origin, cell);
                }

                Vector3 position = WaveLayoutGeometry.CellToWorld(
                    layout,
                    origin,
                    placement.Cell);

                float handleSize = HandleUtility.GetHandleSize(position) *
                                   0.08f;

                if (Handles.Button(
                        position,
                        Quaternion.identity,
                        handleSize,
                        handleSize * 1.25f,
                        Handles.DotHandleCap))
                {
                    window.SelectPlacementFromScene(placement.Id);
                }

                Handles.Label(
                    position,
                    placement.Spawnable.DisplayName);
            }
        }

        private static void DrawCell(
            WaveLayoutData layout,
            Transform origin,
            Vector2Int cell)
        {
            Vector3 first = CornerToWorld(
                layout,
                origin,
                cell.x,
                cell.y);
            Vector3 second = CornerToWorld(
                layout,
                origin,
                cell.x + 1,
                cell.y);
            Vector3 third = CornerToWorld(
                layout,
                origin,
                cell.x + 1,
                cell.y + 1);
            Vector3 fourth = CornerToWorld(
                layout,
                origin,
                cell.x,
                cell.y + 1);

            Handles.DrawAAPolyLine(
                3f,
                first,
                second,
                third,
                fourth,
                first);
        }

        private static Vector3 CornerToWorld(
            WaveLayoutData layout,
            Transform origin,
            int column,
            int row)
        {
            float size = Mathf.Max(0.01f, layout.CellSize);
            Vector3 local = layout.GridPlane == WaveGridPlane.XY
                ? new Vector3(column * size, row * size, 0f)
                : new Vector3(column * size, 0f, row * size);

            return origin.TransformPoint(local);
        }
    }
}
