using UnityEngine;

namespace Puzzle.Grid
{
    /// <summary>
    /// Draws the grid in the world: a plane spawned by Game, using the Puzzle/GridOverlay shader.
    /// Also shows two highlights that interaction modes drive:
    /// the <b>cursor</b> cell (yellow) and an <b>area</b> (drag selection or building footprint,
    /// tinted red when invalid). The shader works in world space, so the plane only has to cover the grid.
    /// </summary>
    public class GridView
    {
        // Unity's built-in Plane mesh is 10x10 units at scale 1.
        private const float PlaneMeshSize = 10f;

        private static readonly int GridOriginId = Shader.PropertyToID("_GridOrigin");
        private static readonly int GridParamsId = Shader.PropertyToID("_GridParams");
        private static readonly int HoverRectId = Shader.PropertyToID("_HoverRect");
        private static readonly int MouseCellId = Shader.PropertyToID("_MouseCell");
        private static readonly int HoverValidId = Shader.PropertyToID("_HoverValid");

        // A rect no cell can be inside, for "no area highlighted".
        private static readonly Vector4 NoArea = new Vector4(-1f, -1f, -2f, -2f);
        private static readonly Vector4 NoCell = new Vector4(-1f, -1f, 0f, 0f);

        private readonly GridMap _grid;
        private readonly MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        private Vector2Int? _cursor;
        private RectInt? _area;
        private bool _areaValid = true;

        /// <summary>Spawns the overlay plane under <paramref name="parent"/>, sized to cover the grid.</summary>
        public static GridView Create(GridMap grid, Material material, float heightOffset, Transform parent)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Grid Overlay";
            Object.Destroy(plane.GetComponent<Collider>()); // picking uses grid math, not physics

            Transform t = plane.transform;
            t.SetParent(parent, true);
            t.SetPositionAndRotation(grid.WorldCenter + Vector3.up * heightOffset, Quaternion.identity);
            t.localScale = new Vector3(grid.WorldSize.x / PlaneMeshSize, 1f, grid.WorldSize.z / PlaneMeshSize);

            var renderer = plane.GetComponent<MeshRenderer>();
            if (material != null) renderer.sharedMaterial = material;
            else Debug.LogWarning("GameConfig has no grid material; the grid won't be visible.");

            return new GridView(grid, renderer);
        }

        private GridView(GridMap grid, MeshRenderer renderer)
        {
            _grid = grid;
            _renderer = renderer;
            Apply();
        }

        // ---------- Highlights ----------

        /// <summary>Highlights the cell under the mouse; null hides it.</summary>
        public void SetCursor(Vector2Int? cell)
        {
            if (cell == _cursor) return;
            _cursor = cell;
            Apply();
        }

        /// <summary>Highlights an area (selection or footprint); null hides it. Invalid areas are tinted red.</summary>
        public void SetArea(RectInt? area, bool valid = true)
        {
            if (area.Equals(_area) && valid == _areaValid) return;
            _area = area;
            _areaValid = valid;
            Apply();
        }

        /// <summary>Removes all highlights.</summary>
        public void Clear()
        {
            _cursor = null;
            _area = null;
            _areaValid = true;
            Apply();
        }

        // ---------- Shader ----------

        private void Apply()
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_block);
            _block.SetVector(GridOriginId, _grid.Origin);
            _block.SetVector(GridParamsId, new Vector4(_grid.Width, _grid.Height, _grid.CellSize, 0f));
            _block.SetVector(MouseCellId, _cursor.HasValue ? new Vector4(_cursor.Value.x, _cursor.Value.y, 0f, 0f) : NoCell);
            _block.SetVector(HoverRectId, _area.HasValue ? ToMinMax(_area.Value) : NoArea);
            _block.SetFloat(HoverValidId, _areaValid ? 1f : 0f);
            _renderer.SetPropertyBlock(_block);
        }

        // The shader expects inclusive (minX, minY, maxX, maxY).
        private static Vector4 ToMinMax(RectInt area) =>
            new Vector4(area.xMin, area.yMin, area.xMax - 1, area.yMax - 1);
    }
}
