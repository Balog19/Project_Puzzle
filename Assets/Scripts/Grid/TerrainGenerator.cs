using System;
using UnityEngine;
// picks tiletype per cell
// two noise maps height and moisture check against a list of ordered rules and then scatter overlays (the veins of ore),
// and u can add a custom map level on top for aesthetics
// for the puzzle levels put the river/mountains/slats where we want them and leave the rest transparent
public class TerrainGenerator : MonoBehaviour
{
    [Serializable]
    public class Rule
    {
        public TileType tile;
        public Vector2 heightRange = new Vector2(0f, 1f);
        public Vector2 moistureRange = new Vector2(0f, 1f);
    }       

    [Serializable]
    public class Overlay
    {
        public TileType tile;
        [Tooltip("only tiles that can be replaced (empty = any)")]
        public TileType[] canReplace;
        public float noiseScale = 0.25f;
        [Range(0f, 1f)] public float threshold = 0.75f;
    }

    [Serializable]
    public class ColorMapping
    {
        public Color color = Color.blue;
        [Tooltip("leave empty to keep the generated tile and only add the tags (ex: paint slots)")]
        public TileType tile;
        public TileTag extraTags;
    }

    [Header("Painted level (Level Painter)")]
    [Tooltip("if set, this level is loaded exactly as painted instead of generating terrain")]
    [SerializeField] private LevelData level;

    public LevelData Level { get => level; set => level = value; }
    public TileType Fallback => fallback;

    [Header("custom level (optional)")]
    [Tooltip("import with read/write on, compression None, filter point. 1 pixel = 1 cell (stretched to fit)")]
    [SerializeField] private Texture2D levelMap;
    [SerializeField] private ColorMapping[] colorMappings;
    [SerializeField, Range(0f, 0.5f)] private float colorTolerance = 0.1f;

    [Header("Seed/Levels")]
    [SerializeField] private int seed = 1234;
    [SerializeField] private bool randomizeSeed;
    [SerializeField] private float heightScale = 0.06f;
    [SerializeField] private float moistureScale = 0.09f;
    [SerializeField, Range(1, 6)] private int octaves = 3;
    [Tooltip("checked top to bottom and first match wins (noise mostly lands between 0.3 and 0.7)")]
    [SerializeField] private Rule[] rules;
    [Tooltip("used when no rule matches (*required)")]
    [SerializeField] private TileType fallback;
    [SerializeField] private Overlay[] overlays;

    private static readonly Vector2Int[] Neighbours =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    // fills in both arrays and indexed y * width + x
    public void Generate(int width, int height, TileType[] types, TileTag[] extraTags, bool ignoreLevel = false)    {
        if (fallback == null) Debug.LogError("TerrainGenerator: assign a Fallback tile", this);
        if (!ignoreLevel && level != null)                                          
        {                                                                           
            if (level.Matches(width, height))                                       
            {                                                                       
                level.CopyTo(types, extraTags, fallback);                          
                TagShores(width, height, types, extraTags);                        
                return;                                                             
            }                                                                       
            Debug.LogWarning($"TerrainGenerator: level '{level.name}' is {level.Width}x{level.Height} but the grid is {width}x{height}. generating terrain instead", this);
        }                                                                           
        
        if (randomizeSeed) seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        var rng = new System.Random(seed);
        Vector2 heightOffset = RandomOffset(rng);
        Vector2 moistureOffset = RandomOffset(rng);

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            float h = Fractal(x, y, heightScale, heightOffset);
            float m = Fractal(x, y, moistureScale, moistureOffset);
            types[i] = Pick(h, m);
            extraTags[i] = TileTag.None;
        }

        if (overlays != null)
        {
            foreach (Overlay o in overlays)
            {
                if (o == null || o.tile == null) continue;
                Vector2 offset = RandomOffset(rng);
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (!CanReplace(o, types[i])) continue;
                    if (Mathf.PerlinNoise(x * o.noiseScale + offset.x, y * o.noiseScale + offset.y) >= o.threshold)
                        types[i] = o.tile;
                }
            }
        }

        if (levelMap != null) ApplyLevelMap(width, height, types, extraTags);

        TagShores(width, height, types, extraTags);
    }

    private TileType Pick(float h, float m)
    {
        if (rules != null)
        {
            foreach (Rule r in rules)
            {
                if (r == null || r.tile == null) continue;
                if (h >= r.heightRange.x && h <= r.heightRange.y &&
                    m >= r.moistureRange.x && m <= r.moistureRange.y)
                    return r.tile;
            }
        }
        return fallback;
    }

    private static bool CanReplace(Overlay o, TileType current)
    {
        if (o.canReplace == null || o.canReplace.Length == 0) return true;
        foreach (TileType t in o.canReplace)
            if (t == current) return true;
        return false;
    }

    private void ApplyLevelMap(int width, int height, TileType[] types, TileTag[] extraTags)
    {
        if (!levelMap.isReadable)
        {
            Debug.LogError("TerrainGenerator: turn on read/write in the level map's import settings", levelMap);
            return;
        }

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Color c = levelMap.GetPixel(x * levelMap.width / width, y * levelMap.height / height);
            if (c.a < 0.5f) continue;   // transparent = keep generated tile

            foreach (ColorMapping mapping in colorMappings)
            {
                if (mapping == null || !Close(c, mapping.color)) continue;
                int i = y * width + x;
                if (mapping.tile != null) types[i] = mapping.tile;
                extraTags[i] |= mapping.extraTags;
                break;
            }
        }
    }

    private static void TagShores(int width, int height, TileType[] types, TileTag[] extraTags)
    {
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            if (IsWater(types[i])) continue;

            foreach (Vector2Int d in Neighbours)
            {
                int nx = x + d.x, ny = y + d.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (IsWater(types[ny * width + nx]))
                {
                    extraTags[i] |= TileTag.Shore;
                    break;
                }
            }
        }
    }

    private static bool IsWater(TileType t) => t != null && (t.Tags & TileTag.Water) != 0;

    private bool Close(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) <= colorTolerance &&
        Mathf.Abs(a.g - b.g) <= colorTolerance &&
        Mathf.Abs(a.b - b.b) <= colorTolerance;

    private float Fractal(int x, int y, float scale, Vector2 offset)
    {
        float sum = 0f, amplitude = 1f, frequency = scale, total = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += Mathf.PerlinNoise(x * frequency + offset.x, y * frequency + offset.y) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }
        return sum / total;
    }

    private static Vector2 RandomOffset(System.Random rng) =>
        new Vector2((float)rng.NextDouble() * 10000f, (float)rng.NextDouble() * 10000f);
}