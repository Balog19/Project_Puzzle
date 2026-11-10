using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
// this class is basically just one giant tooltip that follows the mouse cursor and shows different helpful information
// we can actually have this in the game or just use it for testing but thought it would be useful
// info shown:
// while placing -> cost, why it cant go there or how efficient it would be on this land
// not while placing -> what the hovered tile would be good for
// panel pivot should be top left (0, 1), raycast target turned off on the panel and text
public class TileHoverInfo : MonoBehaviour
{
    [SerializeField] private TileMap tiles;
    [SerializeField] private BuildingPlacer placer;
    [Tooltip("defaults to Camera.main.")]
    [SerializeField] private Camera cam;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    private readonly StringBuilder _sb = new StringBuilder();

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || tiles == null || !tiles.IsReady || IsPointerOverUI() ||
            !TryGetMouseCell(mouse.position.ReadValue(), out Vector2Int cell))
        {
            Hide();
            return;
        }
        _sb.Clear();
        if (placer != null && placer.IsPlacing) DescribePlacement();
        else DescribeTile(cell);
        if (_sb.Length == 0) { Hide(); return; }
        text.text = _sb.ToString();
        panel.gameObject.SetActive(true);
        panel.position = mouse.position.ReadValue() + offset;
    }

    private void DescribePlacement()
    {
        BuildingData b = placer.Selected;
        _sb.Append("<b>").Append(b.DisplayName).Append("</b>");
        bool first = true;
        foreach (ResourceAmount c in b.Cost)
        {
            if (c.Type == null) continue;
            _sb.Append(first ? "\nCost: " : ", ").Append(c.Amount).Append(' ').Append(c.Type.DisplayName);
            first = false;
        }

        if (!string.IsNullOrEmpty(placer.InvalidReason))
            _sb.Append("\n<color=#FF6B5B>").Append(placer.InvalidReason).Append("</color>");
        else if (b.Production.Count > 0)
            _sb.Append("\nSpeed here: ").Append((placer.PreviewEfficiency * 100f).ToString("0")).Append('%');
    }

    private void DescribeTile(Vector2Int cell)
    {
        TileType t = tiles.GetTile(cell);
        if (t == null) return;
        _sb.Append("<b>").Append(t.DisplayName).Append("</b>");
        if (t.Buildable)
            _sb.Append("\nFertility ").Append((t.Fertility * 100f).ToString("0")).Append('%');
        if (t.Resource != null && t.ResourceAmount > 0)
            _sb.Append('\n').Append(t.Resource.DisplayName).Append(": ").Append(tiles.GetResource(cell));
        TileTag shown = tiles.GetTags(cell) & ~TileTag.Ground;
        if (shown != TileTag.None)
            _sb.Append("\n<size=80%>").Append(shown.ToString()).Append("</size>");
        if (placer != null && placer.TryGetBuildingAt(cell, out PlacedBuilding building) &&
            building.TryGetComponent(out LandEfficiency land) && land.Building != null)
        {
            _sb.Append('\n').Append(land.Building.DisplayName);
            if (land.Building.Production.Count > 0)
                _sb.Append(" · ").Append((land.Value * 100f).ToString("0")).Append("% speed");
        }
    }

    private bool TryGetMouseCell(Vector2 screenPosition, out Vector2Int cell)
    {
        cell = default;
        Camera camera = cam != null ? cam : Camera.main;
        if (camera == null) return false;
        GridMap grid = tiles.Grid;
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (!new Plane(Vector3.up, grid.Origin).Raycast(ray, out float distance)) return false;
        return grid.TryWorldToCell(ray.GetPoint(distance), out cell);
    }

    private void Hide()
    {
        if (panel != null && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
    }

    private static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
