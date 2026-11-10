using UnityEngine;

// draws land as flat mesh covering grid with texture with one pixel per square cell
// one draw call
// under GridVisual gridline overlay
// *give MeshRendered a material w/ a white base color like URP Unlit or URP Lit
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainVisual : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");   //URP
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");   //built-in

    [SerializeField] private TileMap tiles;
    [SerializeField] private float heightOffset;
    [Tooltip("changes brightness for cells randomly so it makes it look less flat")]
    [SerializeField, Range(0f, 0.2f)] private float colorVariation = 0.05f;

    private Texture2D _texture;
    private Mesh _mesh;
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _block;

    private void OnEnable()
    {
        if (tiles == null) return;
        tiles.Generated += Rebuild;
        tiles.TileChanged += OnTileChanged;
        if (tiles.IsReady) Rebuild();
    }

    private void OnDisable()
    {
        if (tiles == null) return;
        tiles.Generated -= Rebuild;
        tiles.TileChanged -= OnTileChanged;
    }

    private void OnDestroy()
    {
        if (_texture != null) Destroy(_texture);
        if (_mesh != null) Destroy(_mesh);
    }

    private void Rebuild()
    {
        GridMap grid = tiles.Grid;
        int w = grid.Width, h = grid.Height;

        if (_texture == null || _texture.width != w || _texture.height != h)
        {
            if (_texture != null) Destroy(_texture);
            _texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Terrain Tiles",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            pixels[y * w + x] = ColorFor(new Vector2Int(x, y));

        _texture.SetPixels32(pixels);
        _texture.Apply(false);

        BuildMesh(grid);
        ApplyTexture();
    }

    private void OnTileChanged(Vector2Int cell)
    {
        if (_texture == null) return;
        _texture.SetPixel(cell.x, cell.y, ColorFor(cell));
        _texture.Apply(false);
    }

    private Color32 ColorFor(Vector2Int cell)
    {
        TileType tile = tiles.GetTile(cell);
        Color c = tile != null ? tile.Color : Color.magenta;

        // stable per cell variation
        uint hash = unchecked((uint)(cell.x * 73856093 ^ cell.y * 19349663));
        float t = (hash % 1000) / 999f;
        c *= 1f + (t * 2f - 1f) * colorVariation;
        c.a = 1f;
        return c;
    }

    private void BuildMesh(GridMap grid)
    {
        transform.SetPositionAndRotation(grid.Origin + Vector3.up * heightOffset, Quaternion.identity);
        transform.localScale = Vector3.one;

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "Terrain Quad" };
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        Vector3 size = grid.WorldSize;
        _mesh.Clear();
        _mesh.vertices = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(size.x, 0f, 0f),
            new Vector3(0f, 0f, size.z),
            new Vector3(size.x, 0f, size.z)
        };
        _mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        _mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private void ApplyTexture()
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (_block == null) _block = new MaterialPropertyBlock();

        _renderer.GetPropertyBlock(_block);
        _block.SetTexture(BaseMapId, _texture);
        _block.SetTexture(MainTexId, _texture);
        _renderer.SetPropertyBlock(_block);
    }
}