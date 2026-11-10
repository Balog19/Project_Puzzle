using System;
using UnityEngine;

// flags so a tile can be multiple things at once
[Flags]
public enum TileTag
{
    None           = 0,
    Ground         = 1 << 0,
    Fertile        = 1 << 1,
    Forest         = 1 << 2,
    Water          = 1 << 3,
    Ore            = 1 << 4,
    Rock           = 1 << 5,
    Shore          = 1 << 6,   //added automatically to land next to water
    ProductionSlot = 1 << 7,
    HousingSlot    = 1 << 8,
}

[Serializable]
public class DecorationSettings
{
    public GameObject[] prefabs;
    [Range(0f, 1f)] public float spawnChance = 1f;
    [Min(0)] public int minCount = 1;
    [Min(0)] public int maxCount = 3;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    [Tooltip("clear border around cell")]
    [Range(0f, 0.45f)] public float edgePadding = 0.1f;
    [Tooltip("min distance between models (as fraction of cell size)")]
    [Range(0f, 1f)] public float minSpacing = 0.25f;
    public bool randomYRotation = true;
    [Tooltip("makes the end of stuff liek forests thin out/fade out")]
    public bool thinAtEdges = true;
}

// multiplies efficiency for each footprint cell with this tag
[Serializable]
public class TileBonus
{
    public TileTag tag;
    public float multiplier = 1.25f;
}

// one kind of land
// create by doing create -> puzzle -> tile type
[CreateAssetMenu(menuName = "Puzzle/Tile Type", fileName = "NewTile")]
public class TileType : ScriptableObject
{
    [SerializeField] private string displayName = "Grass";
    [SerializeField] private Color color = new Color(0.45f, 0.7f, 0.35f);

    [Header("Properties")]
    [SerializeField] private TileTag tags = TileTag.Ground;
    [SerializeField] private bool buildable = true;
    [Tooltip("controls fertility of land (production for bildings that use 'Uses Fertility' (1 = normal)")]
    [SerializeField, Range(0f, 2f)] private float fertility = 1f;
    [SerializeField] private ResourceType resource;
    [SerializeField, Min(0)] private int resourceAmount;

    [Header("Models")]
    [SerializeField] private DecorationSettings decorations = new DecorationSettings();
    [Tooltip("what this becomes when a building clears it (forest -> grass) *optional field")]
    [SerializeField] private TileType clearedVersion;

    public string DisplayName => displayName;
    public Color Color => color;
    public TileTag Tags => tags;
    public bool Buildable => buildable;
    public float Fertility => fertility;
    public ResourceType Resource => resource;
    public int ResourceAmount => resourceAmount;
    public DecorationSettings Decorations => decorations;
    public TileType ClearedVersion => clearedVersion;
}