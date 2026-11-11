using UnityEngine;
// painted level with tiletype and slot tags are on every call
// create > puzzle > level and paint with window > puzzle > level painter
// assign level to terrain generator level field to play it
[CreateAssetMenu(menuName = "Puzzle/Level", fileName = "NewLevel")]
public class LevelData : ScriptableObject
{
    [SerializeField, HideInInspector] private int width;
    [SerializeField, HideInInspector] private int height;
    [SerializeField, HideInInspector] private TileType[] tiles = new TileType[0];
    [SerializeField, HideInInspector] private TileTag[] tags = new TileTag[0];

    public int Width => width;
    public int Height => height;

    public bool Matches(int w, int h) =>
        width == w && height == h &&
        tiles != null && tags != null &&
        tiles.Length == w * h && tags.Length == w * h;

    public bool InBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;

    public TileType GetTile(Vector2Int cell) => InBounds(cell) ? tiles[Index(cell)] : null;

    public void SetTile(Vector2Int cell, TileType tile)
    {
        if (InBounds(cell)) tiles[Index(cell)] = tile;
    }

    public TileTag GetTags(Vector2Int cell) => InBounds(cell) ? tags[Index(cell)] : TileTag.None;

    public void SetTags(Vector2Int cell, TileTag value)
    {
        // shore is worked out automatically when the level loads, so it's never stored
        if (InBounds(cell)) tags[Index(cell)] = value & ~TileTag.Shore;
    }

    // changes the size, keeping whatever was painted in the overlapping area
    public void Resize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);
        var newTiles = new TileType[newWidth * newHeight];
        var newTags = new TileTag[newWidth * newHeight];
        if (Matches(width, height))
        {
            for (int y = 0; y < Mathf.Min(height, newHeight); y++)
            for (int x = 0; x < Mathf.Min(width, newWidth); x++)
            {
                newTiles[y * newWidth + x] = tiles[y * width + x];
                newTags[y * newWidth + x] = tags[y * width + x];
            }
        }
        width = newWidth;
        height = newHeight;
        tiles = newTiles;
        tags = newTags;
    }

    public void FillTiles(TileType tile)
    {
        for (int i = 0; i < tiles.Length; i++) tiles[i] = tile;
    }

    public void ClearTags(TileTag remove)
    {
        for (int i = 0; i < tags.Length; i++) tags[i] &= ~remove;
    }

    // empty cells become fallback tiles
    public void CopyTo(TileType[] types, TileTag[] extraTags, TileType fallback)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            types[i] = tiles[i] != null ? tiles[i] : fallback;
            extraTags[i] = tags[i];
        }
    }

    // used by the Level Painter's "Start From Random Terrain"
    public void CopyFrom(TileType[] types, TileTag[] extraTags)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = types[i];
            tags[i] = extraTags[i] & ~TileTag.Shore;
        }
    }

    private int Index(Vector2Int cell) => cell.y * width + cell.x;
}