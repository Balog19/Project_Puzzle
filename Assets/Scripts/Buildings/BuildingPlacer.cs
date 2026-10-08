using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildingPlacer : MonoBehaviour
{
    [SerializeField] private GridMap grid;
    [SerializeField] private GridVisual visual;
    [Tooltip("Defaults to Camera.main.")]
    [SerializeField] private Camera cam;
    [Tooltip("Optional parent for placed buildings, keeps the Hierarchy tidy.")]
    [SerializeField] private Transform buildingsParent;
    [Tooltip("Pays building costs. If empty, buildings are free.")]
    [SerializeField] private ResourceManager resources;

    public event Action<BuildingData> SelectionChanged;

    /// <summary>Raised after a building is placed: its data, the spawned instance, and the cells it covers.</summary>
    public event Action<BuildingData, GameObject, RectInt> BuildingPlaced;

    public BuildingData Selected { get; private set; }
    public bool IsPlacing => Selected != null;

    private GameObject _ghost;
    private RectInt? _lastArea;
    private bool _lastValid;
    private readonly Dictionary<Vector2Int, PlacedBuilding> _buildingsByCell = new Dictionary<Vector2Int, PlacedBuilding>();

    /// <summary>The placed building covering this cell, if any.</summary>
    public bool TryGetBuildingAt(Vector2Int cell, out PlacedBuilding building)
    {
        // Unity's null check also skips buildings that have been destroyed.
        return _buildingsByCell.TryGetValue(cell, out building) && building != null;
    }
    
    public void Select(BuildingData building)
    {
        if (building == null || building.Prefab == null)
        {
            Debug.LogWarning("BuildingPlacer: building or its prefab is not assigned.", this);
            return;
        }

        Cancel();

        Selected = building;
        _ghost = SpawnHologram(building.Prefab);
        if (visual != null) visual.SetMouseCell(null);

        SelectionChanged?.Invoke(Selected);
    }

    public void Cancel()
    {
        if (!IsPlacing) return;

        Selected = null;
        _lastArea = null;
        if (_ghost != null) Destroy(_ghost);

        if (visual != null)
        {
            visual.SetHoveredCells(null);
            visual.SetHoverValid(true);
        }

        SelectionChanged?.Invoke(null);
    }
    
    private void Update()
    {
        if (!IsPlacing || grid == null) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            Cancel();
            return;
        }

        RectInt? area = GetFootprintUnderMouse(mouse.position.ReadValue());
        bool valid = area.HasValue
                     && grid.IsAreaAll(area.Value, GridMap.CellState.Empty)
                     && CanAffordSelected();

        UpdatePreview(area, valid);

        if (valid && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
        {
            Place(area.Value);
        }
    }

    private bool CanAffordSelected() => resources == null || resources.CanAfford(Selected.Cost);

    private void Place(RectInt area)
    {
        if (resources != null && !resources.TrySpend(Selected.Cost)) return;

        GameObject instance = Instantiate(Selected.Prefab, grid.AreaToWorldCenter(area), _ghost.transform.rotation);
        grid.SetArea(area, GridMap.CellState.Occupied);

        PlacedBuilding placed = instance.AddComponent<PlacedBuilding>();
        placed.Init(Selected, area);
        foreach (Vector2Int cell in area.allPositionsWithin)
        {
            _buildingsByCell[cell] = placed;
        }

        BuildingPlaced?.Invoke(Selected, instance, area);
        
        _lastArea = null;
    }
    
    private void UpdatePreview(RectInt? area, bool valid)
    {
        if (_ghost != null)
        {
            _ghost.SetActive(area.HasValue);
            if (area.HasValue) _ghost.transform.position = grid.AreaToWorldCenter(area.Value);
        }
        
        // Only push to the shader when something changed (validity can change from resources alone).
        if (area.Equals(_lastArea) && valid == _lastValid) return;
        _lastArea = area;
        _lastValid = valid;

        if (visual == null) return;
        visual.SetHoveredCells(area.HasValue ? CellsIn(area.Value) : null);
        visual.SetHoverValid(valid);
    }
    private RectInt? GetFootprintUnderMouse(Vector2 screenPosition)
    {
        Camera camera = cam != null ? cam : Camera.main;
        if (camera == null) return null;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        Plane gridPlane = new Plane(Vector3.up, grid.Origin);
        if (!gridPlane.Raycast(ray, out float distance)) return null;

        if (!grid.TryWorldToCell(ray.GetPoint(distance), out Vector2Int mouseCell)) return null;

        Vector2Int size = Selected.Size;
        Vector2Int corner = mouseCell - (size - Vector2Int.one) / 2;
        return new RectInt(corner, size);
    }

    private static GameObject SpawnHologram(GameObject prefab)
    {
        GameObject ghost = Instantiate(prefab);
        ghost.name = prefab.name + " (Ghost)";
        
        foreach (Collider col in ghost.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        ghost.SetActive(false);
        return ghost;
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

    private static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
