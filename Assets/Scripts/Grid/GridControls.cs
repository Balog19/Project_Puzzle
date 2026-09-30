using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Finds the grid cell under the mouse each frame and tells GridVisual to highlight it.
/// Raycasts against a math plane at the grid's height, so no collider is needed.
/// </summary>
public class GridControls : MonoBehaviour
{
    [SerializeField] private GridMap grid;
    [SerializeField] private GridVisual visual;
    [Tooltip("Defaults to Camera.main.")]
    [SerializeField] private Camera cam;
    
    
    private Vector2Int? _lastStartCell;
    private Vector2Int? _lastEndCell;
    private Vector2Int? _lastMouseCell;
    private Vector2Int[] _hoveredCells = System.Array.Empty<Vector2Int>();

    public Vector2Int[] HoveredCells => _hoveredCells;
    public Vector2Int? MouseCell => _lastMouseCell;
    public RectInt? HoveredArea { get; private set; }   // handy for validating on confirm
    public event Action<Vector2Int[]> HoveredCellsChanged;
    
    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }

    public void UpdateHoveredCells(Vector3? dragStartLocation, Vector3? dragEndLocation, Vector3? mouseWorldPosition)
    {
        // 1. Convert everything to cells first (null if missing or off the grid).
        Vector2Int? start = ToCell(dragStartLocation);
        Vector2Int? end   = ToCell(dragEndLocation);
        Vector2Int? mouse = ToCell(mouseWorldPosition);

        // The mouse cell is always shown, even while the selection itself isn't changing.
        if (mouse != _lastMouseCell)
        {
            _lastMouseCell = mouse;
            if (visual != null) visual.SetMouseCell(mouse);
        }

        // 2. Pick the two corners. This one line covers all three states:
        //    hovering: mouse to mouse, dragging: start to mouse, pending: start to end.
        Vector2Int? from = start ?? mouse;
        Vector2Int? to   = end ?? mouse;

        // 3. Skip if nothing changed.
        if (from == _lastStartCell && to == _lastEndCell) return;
        _lastStartCell = from;
        _lastEndCell = to;

        // 4. Fill in the cells between.
        _hoveredCells = from.HasValue && to.HasValue
            ? CellsIn(AreaBetween(from.Value, to.Value))
            : System.Array.Empty<Vector2Int>();
        
        if (visual != null) visual.SetHoveredCells(_hoveredCells);
        HoveredCellsChanged?.Invoke(_hoveredCells);
    }

    /// <summary>
    /// Forgets the last cells sent to the visual, so the next UpdateHoveredCells pushes fresh values.
    /// Call after something else (e.g. BuildingPlacer) has drawn on the visual.
    /// </summary>
    public void ResetCache()
    {
        _lastStartCell = null;
        _lastEndCell = null;
        _lastMouseCell = null;
        if (visual != null) visual.SetMouseCell(null);
    }

    private Vector2Int? ToCell(Vector3? worldPosition)
    {
        if (!worldPosition.HasValue) return null;
        return grid.TryWorldToCell(worldPosition.Value, out Vector2Int cell) ? cell : null;
    }
    
    private static Vector2Int[] CellsIn(RectInt area)
    {
        var cells = new Vector2Int[area.width * area.height];
        int i = 0;
        foreach (Vector2Int cell in area.allPositionsWithin)
        {
            cells[i++] = cell;
        }
        return cells;
    }
    
    private static RectInt AreaBetween(Vector2Int a, Vector2Int b)
    {
        Vector2Int min = Vector2Int.Min(a, b);
        Vector2Int max = Vector2Int.Max(a, b);
        return new RectInt(min, max - min + Vector2Int.one);
    }
    
    
}
