using System;
using System.Collections.Generic;
using Puzzle.Economy;
using UnityEngine;

namespace Puzzle.Buildings
{
    /// <summary>
    /// One building type: what it looks like, its footprint, what it costs, and what it does.
    /// All rates are per tick (20 ticks = 1 second by default).
    /// Data asset (like an Unreal Data Asset). Create via Create > Puzzle > Building and add it to
    /// the GameConfig's Buildings list to give it a button in its category's build tray.
    /// </summary>
    /// <summary>
    /// The models a road switches between as it connects to neighbouring roads. One model per piece;
    /// the game rotates it. Each model, at rotation 0, should have its pivot at the tile's centre and base, and:
    /// Straight runs north-south (along +Z), Corner joins north and east, T joins north, east and south.
    /// Leave any empty to use a simple placeholder. Dead ends and lone tiles use Straight.
    /// </summary>
    [Serializable]
    public class RoadPieces
    {
        [Tooltip("Runs north-south (along +Z) at rotation 0. Also used for dead ends and lone tiles.")]
        public GameObject straight;
        [Tooltip("Joins north and east at rotation 0.")]
        public GameObject corner;
        [Tooltip("Joins north, east and south at rotation 0 (open to the west).")]
        public GameObject tJunction;
        [Tooltip("Joins all four sides.")]
        public GameObject cross;
    }

    [CreateAssetMenu(menuName = "Puzzle/Building", fileName = "NewBuilding")]
    public class BuildingData : ScriptableObject
    {
        [Tooltip("Name shown in the UI.")]
        [SerializeField] private string displayName = "Building";
        [Tooltip("Build-menu group this building is listed under.")]
        [SerializeField] private BuildingCategory category;
        [Tooltip("Icon on its build button. Without one, the button shows the name's first letter.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Model spawned when placed; its pivot should be at the base. Empty = a placeholder box. Roads use Road Pieces instead.")]
        [SerializeField] private GameObject prefab;
        [Tooltip("Footprint in cells (x = grid X, y = grid Z).")]
        [SerializeField] private Vector2Int size = Vector2Int.one;
        [Tooltip("People who can live here. 0 = not a house.")]
        [SerializeField, Min(0)] private int housingCapacity;

        [Header("Cost")]
        [Tooltip("Paid from the resource pool when placed.")]
        [SerializeField] private List<ResourceAmount> cost = new List<ResourceAmount>();

        [Header("Per tick")]
        [Tooltip("Resources added every tick once placed. Leave empty for none.")]
        [SerializeField] private List<ResourceAmount> productionPerTick = new List<ResourceAmount>();
        [Tooltip("Running cost charged every tick (usually Money; Money can go into debt).")]
        [SerializeField] private List<ResourceAmount> upkeepPerTick = new List<ResourceAmount>();

        [Header("Storage")]
        [Tooltip("This is a storage: it collects goods from producers within road range into the shared storage pool, " +
                 "and delivers from that pool to houses within road range.")]
        [SerializeField] private bool isStorage;
        [Tooltip("Most goods this storage moves into the pool per tick, across everything it reaches.")]
        [SerializeField, Min(0f)] private float storageCollectPerTick = 1f;

        [Header("Road")]
        [Tooltip("This is a road: placed by drag-painting, connects to neighbouring roads, and carries goods.")]
        [SerializeField] private bool isRoad;
        [Tooltip("Models the road switches between as it connects. Empty = placeholder.")]
        [SerializeField] private RoadPieces roadPieces = new RoadPieces();

        public string DisplayName => displayName;
        public BuildingCategory Category => category;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public Vector2Int Size => Vector2Int.Max(size, Vector2Int.one);
        public int HousingCapacity => housingCapacity;
        public IReadOnlyList<ResourceAmount> Cost => cost;
        public IReadOnlyList<ResourceAmount> ProductionPerTick => productionPerTick;
        public IReadOnlyList<ResourceAmount> UpkeepPerTick => upkeepPerTick;

        public bool Produces => productionPerTick.Count > 0;
        public bool HasUpkeep => upkeepPerTick.Count > 0;
        public bool IsHousing => housingCapacity > 0;
        public bool IsRoad => isRoad;
        public RoadPieces RoadPieces => roadPieces;
        public bool IsStorage => isStorage;
        public float StorageCollectPerTick => storageCollectPerTick;
    }
}
