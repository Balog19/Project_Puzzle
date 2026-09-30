using System;
using UnityEngine;

public class GridMap : MonoBehaviour
{
    public enum CellState
    {
        Empty,
        Blocked,
        Occupied
    }

    [Header("Size")]
    [SerializeField, Min(1)] private int width = 60;
    [SerializeField, Min(1)] private int height = 60;
    [SerializeField, Min(0.01f)] private float cellSize = 1f;

    [Header("Gizmos")]
    [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color blockedColor = new Color(1f, 0.2f, 0.2f, 0.4f);
    [SerializeField] private Color occupiedColor = new Color(0.2f, 0.6f, 1f, 0.4f);
    
    public event Action<Vector2Int, CellState> CellChanged;
    
    public event Action Resized;

    private CellState[] _cells;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public Vector3 Origin => transform.position;

    public Vector3 WorldSize => new Vector3(width * cellSize, 0f, height * cellSize);
    public Vector3 WorldCenter => Origin + WorldSize * 0.5f;
    
    private void Awake()
    {
        Initialize(width, height, cellSize);
    }
    
    public void Initialize(int newWidth, int newHeight, float newCellSize)
    {
        width = Mathf.Max(1, newWidth);
        height = Mathf.Max(1, newHeight);
        cellSize = Mathf.Max(0.01f, newCellSize);
        _cells = new CellState[width * height];
        Resized?.Invoke();
    }
    public bool InBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
    
    public bool InBounds(RectInt area) =>
        area.width > 0 && area.height > 0 &&
        area.xMin >= 0 && area.yMin >= 0 &&
        area.xMax <= width && area.yMax <= height;
    
    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - Origin;
        return new Vector2Int(
            Mathf.FloorToInt(local.x / cellSize),
            Mathf.FloorToInt(local.z / cellSize));
    }

    public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
    {
        cell = WorldToCell(worldPosition);
        return InBounds(cell);
    }

    public Vector3 CellToWorldCorner(Vector2Int cell) =>
        Origin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

    public Vector3 CellToWorldCenter(Vector2Int cell) =>
        CellToWorldCorner(cell) + new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f);


    public Vector3 AreaToWorldCenter(RectInt area) =>
        Origin + new Vector3(
            (area.x + area.width * 0.5f) * cellSize,
            0f,
            (area.y + area.height * 0.5f) * cellSize);
    

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

    public void Fill(CellState state)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SetState(new Vector2Int(x, y), state);
            }
        }
    }

    private int ToIndex(Vector2Int cell) => cell.y * width + cell.x;

    private void ThrowIfOutOfBounds(Vector2Int cell)
    {
        if (_cells == null)
            throw new InvalidOperationException("GridMap used before Awake. Access it from Start or later.");

        if (!InBounds(cell))
            throw new ArgumentOutOfRangeException(nameof(cell), cell, $"Cell is outside the {width}x{height} grid.");
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = Origin;
        
        if (_cells != null && _cells.Length == width * height)
        {
            Vector3 fillSize = new Vector3(cellSize * 0.95f, 0.01f, cellSize * 0.95f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CellState state = _cells[y * width + x];
                    if (state == CellState.Empty) continue;

                    Gizmos.color = state == CellState.Blocked ? blockedColor : occupiedColor;
                    Gizmos.DrawCube(CellToWorldCenter(new Vector2Int(x, y)), fillSize);
                }
            }
        }
        
        Gizmos.color = gridLineColor;

        for (int x = 0; x <= width; x++)
        {
            Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
            Gizmos.DrawLine(start, start + new Vector3(0f, 0f, height * cellSize));
        }

        for (int y = 0; y <= height; y++)
        {
            Vector3 start = origin + new Vector3(0f, 0f, y * cellSize);
            Gizmos.DrawLine(start, start + new Vector3(width * cellSize, 0f, 0f));
        }
    }
}
