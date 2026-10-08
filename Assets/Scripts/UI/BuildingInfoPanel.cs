using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The box that pops up when a building is selected. Shows its name and one progress row
/// per production / upkeep timer. To add a new section, add it in Show().
/// It lives in the screen-space Canvas but follows the building's on-screen position,
/// so it floats above the building at a constant, readable size.
/// </summary>
public class BuildingInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private Button closeButton;
    [Tooltip("Rows are added under this.")]
    [SerializeField] private Transform content;
    [Tooltip("A disabled ProgressRow that gets copied for each row.")]
    [SerializeField] private ProgressRow rowTemplate;

    [Header("Follow building")]
    [Tooltip("Defaults to Camera.main.")]
    [SerializeField] private Camera cam;
    [Tooltip("World units above the top of the building's model.")]
    [SerializeField] private float heightAboveBuilding = 0.5f;
    [Tooltip("Extra offset in canvas pixels.")]
    [SerializeField] private Vector2 screenOffset;

    public PlacedBuilding Building { get; private set; }

    // Each live row and how to refresh it every frame.
    private readonly List<(ProgressRow row, Func<string> text, Func<float> progress)> _rows =
        new List<(ProgressRow, Func<string>, Func<float>)>();

    private RectTransform _rect;
    private RectTransform _parentRect;
    private Canvas _canvas;
    private Vector3 _worldAnchor;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        CacheLayout();
    }

    // Also called from Show, which can run before Awake while the panel is still disabled.
    private void CacheLayout()
    {
        if (_rect != null) return;

        _rect = (RectTransform)transform;
        _parentRect = transform.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>(true);

        // Bottom-center sits on the point above the building; the box grows upward.
        _rect.pivot = new Vector2(0.5f, 0f);
    }

    public void Show(PlacedBuilding building)
    {
        if (building == null)
        {
            Hide();
            return;
        }

        Building = building;
        _worldAnchor = GetWorldAnchor(building);
        if (title != null) title.text = building.Data != null ? building.Data.DisplayName : building.name;

        ClearRows();

        foreach (ResourceProducer producer in building.GetComponents<ResourceProducer>())
        {
            AddRow(() => $"Producing {ResourceAmount.Format(producer.Output)}",
                   () => producer.Progress);
        }

        foreach (ResourceConsumer consumer in building.GetComponents<ResourceConsumer>())
        {
            AddRow(() => UpkeepText(consumer), () => consumer.Progress);
        }

        gameObject.SetActive(true);
        Refresh();
        FollowBuilding();
    }

    public void Hide()
    {
        Building = null;
        ClearRows();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // The building may have been destroyed while the panel was open.
        if (Building == null)
        {
            Hide();
            return;
        }

        Refresh();
    }

    // LateUpdate: runs after the camera moved this frame, so the panel doesn't lag behind.
    private void LateUpdate()
    {
        if (Building != null) FollowBuilding();
    }

    // ---------- Positioning ----------

    private void FollowBuilding()
    {
        CacheLayout();
        if (_parentRect == null) return;

        Camera camera = cam != null ? cam : Camera.main;
        if (camera == null) return;

        Vector3 screen = camera.WorldToScreenPoint(_worldAnchor);

        // Overlay canvases convert with no camera; Screen Space - Camera canvases need theirs.
        Camera canvasCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screen, canvasCamera, out Vector2 local))
        {
            _rect.localPosition = local + screenOffset;
        }
    }

    /// <summary>Point just above the top-center of the building's model.</summary>
    private Vector3 GetWorldAnchor(PlacedBuilding building)
    {
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return building.transform.position + Vector3.up * heightAboveBuilding;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return new Vector3(bounds.center.x, bounds.max.y + heightAboveBuilding, bounds.center.z);
    }

    // ---------- Rows ----------

    private void AddRow(Func<string> text, Func<float> progress)
    {
        if (rowTemplate == null || content == null) return;

        ProgressRow row = Instantiate(rowTemplate, content);
        row.gameObject.SetActive(true);
        _rows.Add((row, text, progress));
    }

    private void ClearRows()
    {
        foreach (var entry in _rows)
        {
            if (entry.row != null) Destroy(entry.row.gameObject);
        }
        _rows.Clear();
    }

    private void Refresh()
    {
        foreach (var entry in _rows)
        {
            entry.row.Set(entry.text(), entry.progress());
        }
    }

    private static string UpkeepText(ResourceConsumer consumer)
    {
        // Show the total this building will pay next tick (per resident x residents).
        int residents = consumer.Residents;
        var total = new List<ResourceAmount>();
        foreach (ResourceAmount item in consumer.PerResident)
        {
            total.Add(new ResourceAmount(item.Type, item.Amount * residents));
        }

        string text = ResourceAmount.Format(total);
        string upkeep = string.IsNullOrEmpty(text) ? "Upkeep: nothing (no residents)" : $"Upkeep: {text}";
        return consumer.IsSupplied ? upkeep : upkeep + " (short!)";
    }
}
