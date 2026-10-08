using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Left-click a placed building to open its info panel; click empty ground or press Escape to close it.
/// Uses the grid (mouse cell -> building on that cell), so models don't need colliders.
/// </summary>
public class BuildingSelector : MonoBehaviour
{
    [SerializeField] private GridMap grid;
    [SerializeField] private BuildingPlacer placer;
    [Tooltip("Defaults to Camera.main.")]
    [SerializeField] private Camera cam;
    [SerializeField] private BuildingInfoPanel panel;

    /// <summary>Raised when the selected building changes. Null means nothing is selected.</summary>
    public event Action<PlacedBuilding> SelectionChanged;

    public PlacedBuilding Selected { get; private set; }

    private void OnEnable()
    {
        if (placer != null) placer.SelectionChanged += OnPlacementSelectionChanged;
    }

    private void OnDisable()
    {
        if (placer != null) placer.SelectionChanged -= OnPlacementSelectionChanged;
    }

    private void Update()
    {
        if (placer != null && placer.IsPlacing) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Select(null);
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || IsPointerOverUI()) return;

        Select(FindBuildingUnderMouse(mouse.position.ReadValue()));
    }

    public void Select(PlacedBuilding building)
    {
        // A destroyed building counts as nothing selected.
        if (building == null) building = null;

        // Re-clicking the same building reopens the panel if it was closed with its X button.
        bool panelOpen = panel != null && panel.gameObject.activeSelf;
        if (building == Selected && (building == null || panelOpen)) return;

        Selected = building;

        if (panel != null)
        {
            if (building != null) panel.Show(building);
            else panel.Hide();
        }

        SelectionChanged?.Invoke(Selected);
    }

    private PlacedBuilding FindBuildingUnderMouse(Vector2 screenPosition)
    {
        if (grid == null || placer == null) return null;

        Camera camera = cam != null ? cam : Camera.main;
        if (camera == null) return null;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        Plane gridPlane = new Plane(Vector3.up, grid.Origin);
        if (!gridPlane.Raycast(ray, out float distance)) return null;

        if (!grid.TryWorldToCell(ray.GetPoint(distance), out Vector2Int cell)) return null;

        return placer.TryGetBuildingAt(cell, out PlacedBuilding building) ? building : null;
    }

    // Entering placement mode closes the panel.
    private void OnPlacementSelectionChanged(BuildingData building)
    {
        if (building != null) Select(null);
    }

    private static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
