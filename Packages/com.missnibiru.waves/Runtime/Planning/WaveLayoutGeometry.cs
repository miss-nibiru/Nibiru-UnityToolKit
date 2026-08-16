using System.Collections.Generic;
using MissNibiru.Waves.Layouts;
using UnityEngine;

namespace MissNibiru.Waves.Planning
{
    public static class WaveLayoutGeometry
    {
        public static Vector2Int TransformOffset(
            Vector2Int offset,
            WaveGridRotation rotation,
            bool flipHorizontal,
            bool flipVertical)
        {
            if (flipHorizontal)
                offset.x = -offset.x;

            if (flipVertical)
                offset.y = -offset.y;

            switch (rotation)
            {
                case WaveGridRotation.Degrees90:
                    return new Vector2Int(-offset.y, offset.x);

                case WaveGridRotation.Degrees180:
                    return new Vector2Int(-offset.x, -offset.y);

                case WaveGridRotation.Degrees270:
                    return new Vector2Int(offset.y, -offset.x);

                default:
                    return offset;
            }
        }

        public static IReadOnlyList<Vector2Int>
            GetFormationCells(WaveLayoutPlacement placement)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            if (placement == null)
                return cells;

            Vector2Int[] offsets =
                placement.Formation == null
                    ? null
                    : placement.Formation.CellOffsets;

            if (offsets == null || offsets.Length == 0)
            {
                cells.Add(placement.Cell);
                return cells;
            }

            foreach (Vector2Int offset in offsets)
            {
                cells.Add(
                    placement.Cell + TransformOffset(
                        offset,
                        placement.Rotation,
                        placement.FlipHorizontal,
                        placement.FlipVertical));
            }

            return cells;
        }

        public static IReadOnlyList<Vector2Int>
            GetOccupiedCells(
                SpawnableDefinition spawnable,
                Vector2Int anchorCell,
                WaveGridRotation rotation,
                bool flipHorizontal,
                bool flipVertical)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            if (spawnable == null)
                return cells;

            Vector2Int size = spawnable.SafeFootprint;
            Vector2Int pivot = spawnable.SafePivot;

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int local =
                        new Vector2Int(x, y) - pivot;

                    cells.Add(
                        anchorCell + TransformOffset(
                            local,
                            rotation,
                            flipHorizontal,
                            flipVertical));
                }
            }

            return cells;
        }

        public static Vector3 CellToLocal(
            WaveLayoutData layout,
            Vector2Int cell)
        {
            if (layout == null)
                return Vector3.zero;

            float size = Mathf.Max(0.01f, layout.CellSize);
            float first = (cell.x + 0.5f) * size;
            float second = (cell.y + 0.5f) * size;

            return layout.GridPlane == WaveGridPlane.XY
                ? new Vector3(first, second, 0f)
                : new Vector3(first, 0f, second);
        }

        public static Vector3 CellToWorld(
            WaveLayoutData layout,
            Transform origin,
            Vector2Int cell)
        {
            Vector3 local = CellToLocal(layout, cell);
            return origin == null
                ? local
                : origin.TransformPoint(local);
        }

        public static Quaternion GetWorldRotation(
            WaveLayoutData layout,
            Transform origin,
            WaveGridRotation rotation)
        {
            float degrees = 90f * (int)rotation;
            Vector3 localAxis =
                layout != null &&
                layout.GridPlane == WaveGridPlane.XY
                    ? Vector3.forward
                    : Vector3.up;

            Quaternion localRotation =
                Quaternion.AngleAxis(degrees, localAxis);

            return origin == null
                ? localRotation
                : origin.rotation * localRotation;
        }

        public static bool IsInside(
            WaveLayoutData layout,
            Vector2Int cell)
        {
            return layout != null &&
                   cell.x >= 0 &&
                   cell.y >= 0 &&
                   cell.x < layout.Columns &&
                   cell.y < layout.Rows;
        }
    }
}
