using System;
using System.Collections.Generic;
using UnityEngine;

// decides what buildings/ actions can be taken on land based on its characterisics
// each land: one tiletype per gridmap cell and per cell tags and resource deposits
// this sits next to the gridmap which controls empty/blocked/occupied 
public class TileMap : MonoBehaviour
{
    [SerializeField] private GridMap grid;
    [SerializeField] private TerrainGenerator generator;
    [Tooltip("optional: spawns trees, rocks, etc. on tiles")]
    [SerializeField] private TileDecorator decorator;
    [SerializeField] private bool generateOnStart = true;

    //raised after the whole map is regenerate
    public event Action Generated;
    // raised when one cells tile or tags change (cleared forest, meteor crater, etc)
    public event Action<Vector2Int> TileChanged;

    private TileType[] _types;
    private TileTag[] _extraTags;
    private int[] _resources;
    private List<GameObject>[] _decorations;
    private Transform _decorationRoot;
    private bool _started;

    public GridMap Grid => grid;
    public bool IsReady => grid != null && _types != null && _types.Length == grid.Width * grid.Height;

    private void OnEnable()
    {
        if (grid != null) grid.Resized += OnGridResized;
    }

    private void OnDisable()
    {
        if (grid != null) grid.Resized -= OnGridResized;
    }

    private void Start()
    {
        _started = true;
        if (generateOnStart) Generate();
    }

    private void OnGridResized()
    {
        if (_started && generateOnStart) Generate();
    }


    public void Generate()
    {
        if (grid == null || generator == null)
        {
            Debug.LogError("TileMap: assign the GridMap and TerrainGenerator", this);
            return;
        }

        ResetDecorationRoot();

        int count = grid.Width * grid.Height;
        _types = new TileType[count];
        _extraTags = new TileTag[count];
        _resources = new int[count];
        _decorations = new List<GameObject>[count];

        generator.Generate(grid.Width, grid.Height, _types, _extraTags);

        for (int i = 0; i < count; i++)
            _resources[i] = _types[i] != null ? _types[i].ResourceAmount : 0;

        if (decorator != null)
        {
            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                var cell = new Vector2Int(x, y);
                decorator.Decorate(this, cell, DecorationsAt(Index(cell)), _decorationRoot);
            }
        }

        Generated?.Invoke();
    }

    public TileType GetTile(Vector2Int cell) =>
        IsReady && grid.InBounds(cell) ? _types[Index(cell)] : null;

    public TileTag GetTags(Vector2Int cell)
    {
        if (!IsReady || !grid.InBounds(cell)) return TileTag.None;
        int i = Index(cell);
        return (_types[i] != null ? _types[i].Tags : TileTag.None) | _extraTags[i];
    }

    public int GetResource(Vector2Int cell) =>
        IsReady && grid.InBounds(cell) ? _resources[Index(cell)] : 0;

    //returns how much was taken when mines deplete land
    public int TakeResource(Vector2Int cell, int amount)
    {
        if (!IsReady || !grid.InBounds(cell) || amount <= 0) return 0;
        int i = Index(cell);
        int taken = Mathf.Min(amount, _resources[i]);
        _resources[i] -= taken;
        return taken;
    }

    public void SetTile(Vector2Int cell, TileType type)
    {
        if (!IsReady || !grid.InBounds(cell) || type == null) return;

        int i = Index(cell);
        _types[i] = type;
        _resources[i] = type.ResourceAmount;

        ClearDecorations(cell);
        if (decorator != null) decorator.Decorate(this, cell, DecorationsAt(i), _decorationRoot);

        TileChanged?.Invoke(cell);
    }

    public void AddTags(Vector2Int cell, TileTag tags)
    {
        if (!IsReady || !grid.InBounds(cell)) return;
        _extraTags[Index(cell)] |= tags;
        TileChanged?.Invoke(cell);
    }

    public void ClearDecorations(Vector2Int cell)
    {
        if (!IsReady || !grid.InBounds(cell)) return;
        List<GameObject> list = _decorations[Index(cell)];
        if (list == null) return;
        foreach (GameObject model in list)
            if (model != null) Destroy(model);
        list.Clear();
    }

    //land rules only 
    // GridMap occupancy and cost  checked by BuildingPlacer
    public bool CanPlace(BuildingData building, RectInt area, out string reason)
    {
        reason = null;
        if (!IsReady) return true;

        bool touchesRequired = building.RequireAnyTags == TileTag.None;

        foreach (Vector2Int cell in area.allPositionsWithin)
        {
            if (!grid.InBounds(cell)) { reason = "Outside the map"; return false; }

            TileType tile = GetTile(cell);
            TileTag tags = GetTags(cell);
            string name = tile != null ? tile.DisplayName : "this tile";

            if (tile != null && !tile.Buildable && !building.IgnoreUnbuildable)
            { reason = $"Can't build on {name}"; return false; }

            if ((tags & building.ForbiddenTags) != 0)
            { reason = $"Can't build on {name}"; return false; }

            if ((tags & building.RequiredTags) != building.RequiredTags)
            { reason = $"Needs {Pretty(building.RequiredTags)} land"; return false; }

            if ((tags & building.RequireAnyTags) != 0) touchesRequired = true;
        }

        if (!touchesRequired)
        {
            reason = $"Must be on {Pretty(building.RequireAnyTags)}";
            return false;
        }
        return true;
    }

    // average land multiplier across the footprint (1 = normal)
    public float GetEfficiency(BuildingData building, RectInt area)
    {
        if (!IsReady) return 1f;

        float total = 0f;
        int count = 0;
        foreach (Vector2Int cell in area.allPositionsWithin)
        {
            TileType tile = GetTile(cell);
            if (tile == null) continue;

            TileTag tags = GetTags(cell);
            float e = 1f;
            foreach (TileBonus bonus in building.TileBonuses)
            {
                if (bonus != null && bonus.tag != TileTag.None && (tags & bonus.tag) == bonus.tag)
                    e *= bonus.multiplier;
            }
            if (building.UsesFertility) e *= tile.Fertility;

            total += e;
            count++;
        }
        return count > 0 ? total / count : 1f;
    }

    // clears trees / converts land under a newly placed building
    public void ApplyBuilding(BuildingData building, RectInt area)
    {
        if (!IsReady) return;

        foreach (Vector2Int cell in area.allPositionsWithin)
        {
            TileType tile = GetTile(cell);
            if (building.ConvertToClearedTile && tile != null && tile.ClearedVersion != null)
                SetTile(cell, tile.ClearedVersion);
            if (building.ClearDecorations)
                ClearDecorations(cell);
        }
    }
    private int Index(Vector2Int cell) => cell.y * grid.Width + cell.x;

    private List<GameObject> DecorationsAt(int index) =>
        _decorations[index] ?? (_decorations[index] = new List<GameObject>());

    private void ResetDecorationRoot()
    {
        if (_decorationRoot != null) Destroy(_decorationRoot.gameObject);
        _decorationRoot = new GameObject("Tile Models").transform;
        _decorationRoot.SetParent(transform, false);
    }

    private static string Pretty(TileTag tags) => tags.ToString().Replace(", ", " / ");
}