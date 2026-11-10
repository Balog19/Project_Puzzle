using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Building", fileName = "NewBuilding")]
public class BuildingData : ScriptableObject
{
    [SerializeField] private string displayName = "DefaultBuildingName";
    [SerializeField] private GameObject prefab;
    [Tooltip("Footprint in cells")]
    [SerializeField] private Vector2Int size = Vector2Int.one;
    [Tooltip("People this building can house")]
    [SerializeField, Min(0)] private int housingCapacity;

    [Header("Cost")]
    [SerializeField] private List<ResourceAmount> cost = new List<ResourceAmount>();

    [Header("Production")]
    [Tooltip("Resources produced every Production Interval once placed. Leave empty for none.")]
    [SerializeField] private List<ResourceAmount> production = new List<ResourceAmount>();
    [SerializeField, Min(0.1f)] private float productionInterval = 10f;

    [Header("Upkeep")]
    [Tooltip("Consumed per resident every Consumption Interval. Only applies to buildings that house people.")]
    [SerializeField] private List<ResourceAmount> consumptionPerResident = new List<ResourceAmount>();
    [SerializeField, Min(0.1f)] private float consumptionInterval = 10f;

    [Header("Land Rules")]
    [Tooltip("every cell must have ALL of these tags. None = no requirement")]
    [SerializeField] private TileTag requiredTags = TileTag.None;
    [Tooltip("at least one cell must have one of these (ex: Shore for a well). None = no requirement")]
    [SerializeField] private TileTag requireAnyTags = TileTag.None;
    [Tooltip("no cell can have any of these")]
    [SerializeField] private TileTag forbiddenTags = TileTag.Water;
    [Tooltip("allow tiles marked not buildable (a mine on rock, a bridge over water)")]
    [SerializeField] private bool ignoreUnbuildable;
    [Tooltip("remove trees/rocks under the building")]
    [SerializeField] private bool clearDecorations = true;
    [Tooltip("turn the land into its cleared version (forest -> grass)")]
    [SerializeField] private bool convertToClearedTile;

    [Header("Land efficiency")]
    [Tooltip("production speed multipliers for footprint cells with a tag (Forest x1.5 for a lumberjack)")]
    [SerializeField] private List<TileBonus> tileBonuses = new List<TileBonus>();
    [Tooltip("production speed also scales with the land's fertility (farms)")]
    [SerializeField] private bool usesFertility;

    public string DisplayName => displayName;
    public GameObject Prefab => prefab;
    public Vector2Int Size => Vector2Int.Max(size, Vector2Int.one);
    public int HousingCapacity => housingCapacity;
    public IReadOnlyList<ResourceAmount> Cost => cost;
    public IReadOnlyList<ResourceAmount> Production => production;
    public float ProductionInterval => productionInterval;
    public IReadOnlyList<ResourceAmount> ConsumptionPerResident => consumptionPerResident;
    public float ConsumptionInterval => consumptionInterval;

   public TileTag RequiredTags => requiredTags;
    public TileTag RequireAnyTags => requireAnyTags;
    public TileTag ForbiddenTags => forbiddenTags;
    public bool IgnoreUnbuildable => ignoreUnbuildable;
    public bool ClearDecorations => clearDecorations;
    public bool ConvertToClearedTile => convertToClearedTile;
    public IReadOnlyList<TileBonus> TileBonuses => tileBonuses;
    public bool UsesFertility => usesFertility;
}
