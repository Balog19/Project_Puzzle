using System;
using UnityEngine;

namespace Puzzle.Grid
{
    /// <summary>What occupies a grid cell.</summary>
    public enum CellState
    {
        Empty,
        Blocked,
        Occupied
    }

    /// <summary>
    /// The grid's data: size, per-cell state, and the only place world/cell math lives.
    /// Plain C# object created by Game. The grid lies on the XZ plane: cell.x maps to world X,
    /// cell.y maps to world Z, and <see cref="Origin"/> is the bottom-left corner of cell (0,0).
    /// </summary>
    public class GridMap
    {
        /// <summary>Raised only when a cell's state actually changes.</summary>
        public event Action<Vector2Int, CellState> CellChanged;

        public int Width { get; }
        public int Height { get; }
        /// <summary>Size of one cell in world units.</summary>
        public float CellSize { get; }
        /// <summary>World position of the bottom-left corner of cell (0,0).</summary>
        public Vector3 Origin { get; }

        public Vector3 WorldSize => new Vector3(Width * CellSize, 0f, Height * CellSize);
        public Vector3 WorldCenter => Origin + WorldSize * 0.5f;

        private readonly CellState[] _cells;

        public GridMap(int width, int height, float cellSize, Vector3 origin)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            CellSize = Mathf.Max(0.01f, cellSize);
            Origin = origin;
            _cells = new CellState[Width * Height];
        }

        // ---------- Bounds ----------

        public bool InBounds(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

        /// <summary>True if the whole area fits inside the grid. RectInt max edges are exclusive.</summary>
        public bool InBounds(RectInt area) =>
            area.width > 0 && area.height > 0 &&
            area.xMin >= 0 && area.yMin >= 0 &&
            area.xMax <= Width && area.yMax <= Height;

        // ---------- Conversions ----------

        /// <summary>
        /// World position to cell coordinate. The result may be out of bounds.
        /// FloorToInt (not a cast) so positions just outside the negative edges map to -1, not 0.
        /// </summary>
        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - Origin;
            return new Vector2Int(
                Mathf.FloorToInt(local.x / CellSize),
                Mathf.FloorToInt(local.z / CellSize));
        }

        /// <summary>World position to cell; false if it's outside the grid.</summary>
        public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
        {
            cell = WorldToCell(worldPosition);
            return InBounds(cell);
        }

        /// <summary>Intersects a ray (e.g. from the mouse) with the grid's plane.</summary>
        public bool TryRaycast(Ray ray, out Vector3 worldPoint)
        {
            var plane = new Plane(Vector3.up, Origin);
            if (plane.Raycast(ray, out float distance))
            {
                worldPoint = ray.GetPoint(distance);
                return true;
            }

            worldPoint = default;
            return false;
        }

        public Vector3 CellToWorldCorner(Vector2Int cell) =>
            Origin + new Vector3(cell.x * CellSize, 0f, cell.y * CellSize);

        public Vector3 CellToWorldCenter(Vector2Int cell) =>
            CellToWorldCorner(cell) + new Vector3(CellSize * 0.5f, 0f, CellSize * 0.5f);

        /// <summary>World-space center of a multi-cell footprint, e.g. for placing a 2x3 building.</summary>
        public Vector3 AreaToWorldCenter(RectInt area) =>
            Origin + new Vector3(
                (area.x + area.width * 0.5f) * CellSize,
                0f,
                (area.y + area.height * 0.5f) * CellSize);

        // ---------- Cell state ----------

        public CellState GetState(Vector2Int cell)
        {
            ThrowIfOutOfBounds(cell);
            return _cells[ToIndex(cell)];
        }

        public bool TryGetState(Vector2Int cell, out CellState state)
        {
            if (!InBounds(cell))
            {
                state = default;
                return false;
            }

            state = _cells[ToIndex(cell)];
            return true;
        }

        public void SetState(Vector2Int cell, CellState state)
        {
            ThrowIfOutOfBounds(cell);

            int index = ToIndex(cell);
            if (_cells[index] == state) return;

            _cells[index] = state;
            CellChanged?.Invoke(cell, state);
        }

        /// <summary>True if the area is fully in bounds and every cell has the given state.</summary>
        public bool IsAreaAll(RectInt area, CellState state)
        {
            if (!InBounds(area)) return false;

            foreach (Vector2Int cell in area.allPositionsWithin)
            {
                if (_cells[ToIndex(cell)] != state) return false;
            }

            return true;
        }

        public void SetArea(RectInt area, CellState state)
        {
            if (!InBounds(area))
                throw new ArgumentOutOfRangeException(nameof(area), area, "Area is not fully inside the grid.");

            foreach (Vector2Int cell in area.allPositionsWithin)
            {
                SetState(cell, state);
            }
        }

        // ---------- Helpers ----------

        /// <summary>The rectangle spanning two corner cells, inclusive (e.g. a drag selection).</summary>
        public static RectInt AreaBetween(Vector2Int a, Vector2Int b)
        {
            Vector2Int min = Vector2Int.Min(a, b);
            Vector2Int max = Vector2Int.Max(a, b);
            return new RectInt(min, max - min + Vector2Int.one);
        }

        private int ToIndex(Vector2Int cell) => cell.y * Width + cell.x;

        private void ThrowIfOutOfBounds(Vector2Int cell)
        {
            if (!InBounds(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), cell, $"Cell is outside the {Width}x{Height} grid.");
        }
    }
}
