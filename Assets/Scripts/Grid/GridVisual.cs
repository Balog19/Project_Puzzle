using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class GridVisual : MonoBehaviour
{ private const float PlaneMeshSize = 10f;

    private static readonly int GridOriginId = Shader.PropertyToID("_GridOrigin");
    private static readonly int GridParamsId = Shader.PropertyToID("_GridParams");
    private static readonly int HoverRectId = Shader.PropertyToID("_HoverRect");
    private static readonly int MouseCellId = Shader.PropertyToID("_MouseCell");
    private static readonly int HoverValidId = Shader.PropertyToID("_HoverValid");

    [SerializeField] private GridMap grid;
    [Tooltip("Lifts the plane slightly above the grid origin to avoid z-fighting with the ground.")]
    [SerializeField] private float heightOffset = 0.01f;

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _block;
    private Vector2Int[] _hoveredCells = System.Array.Empty<Vector2Int>();
    private Vector2Int? _mouseCell;
    private bool _hoverValid = true;

    private void OnEnable()
    {
        if (grid != null) grid.Resized += Sync;
        Sync();
    }

    private void OnDisable()
    {
        if (grid != null) grid.Resized -= Sync;
    }

    private void Update()
    {
        if (!Application.isPlaying || (grid != null && grid.transform.hasChanged))
        {
            Sync();
            if (grid != null) grid.transform.hasChanged = false;
        }
    }
    
    public void SetHoveredCells(Vector2Int[] cells)
    {
        _hoveredCells = cells ?? System.Array.Empty<Vector2Int>();
        Sync();
    }
    public void SetMouseCell(Vector2Int? cell)
    {
        _mouseCell = cell;
        Sync();
    }
    public void SetHoverValid(bool valid)
    {
        if (_hoverValid == valid) return;
        _hoverValid = valid;
        Sync();
    }

    private void Sync()
    {
        if (grid == null) return;

        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (_block == null) _block = new MaterialPropertyBlock();

        Vector3 worldSize = grid.WorldSize;
        transform.SetPositionAndRotation(grid.WorldCenter + Vector3.up * heightOffset, Quaternion.identity);
        transform.localScale = new Vector3(worldSize.x / PlaneMeshSize, 1f, worldSize.z / PlaneMeshSize);

        _renderer.GetPropertyBlock(_block);
        _block.SetVector(GridOriginId, grid.Origin);
        _block.SetVector(GridParamsId, new Vector4(grid.Width, grid.Height, grid.CellSize, 0f));
        _block.SetVector(HoverRectId, GetHoverRect());
        Vector2Int mouse = _mouseCell ?? new Vector2Int(-1, -1);
        _block.SetVector(MouseCellId, new Vector4(mouse.x, mouse.y, 0f, 0f));
        _block.SetFloat(HoverValidId, _hoverValid ? 1f : 0f);
        _renderer.SetPropertyBlock(_block);
    }
    private Vector4 GetHoverRect()
    {
        if (_hoveredCells.Length == 0) return new Vector4(-1f, -1f, -2f, -2f);

        Vector2Int min = _hoveredCells[0];
        Vector2Int max = _hoveredCells[0];
        foreach (Vector2Int cell in _hoveredCells)
        {
            min = Vector2Int.Min(min, cell);
            max = Vector2Int.Max(max, cell);
        }

        return new Vector4(min.x, min.y, max.x, max.y);
    }
}
