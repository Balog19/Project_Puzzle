using System.Collections.Generic;
using Puzzle.Economy;
using UnityEngine;

namespace Puzzle.Buildings
{
    /// <summary>
    /// One building type: what it looks like, its footprint, what it costs, and what it does.
    /// All rates are per tick (20 ticks = 1 second by default).
    /// Data asset (like an Unreal Data Asset). Create via Create > Puzzle > Building and add it to
    /// the GameConfig's Buildings list to give it a build-menu button.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle/Building", fileName = "NewBuilding")]
    public class BuildingData : ScriptableObject
    {
        [Tooltip("Name shown in the UI.")]
        [SerializeField] private string displayName = "Building";
        [Tooltip("Model spawned when placed. Its pivot should be at the base.")]
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

        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public Vector2Int Size => Vector2Int.Max(size, Vector2Int.one);
        public int HousingCapacity => housingCapacity;
        public IReadOnlyList<ResourceAmount> Cost => cost;
        public IReadOnlyList<ResourceAmount> ProductionPerTick => productionPerTick;
        public IReadOnlyList<ResourceAmount> UpkeepPerTick => upkeepPerTick;

        public bool Produces => productionPerTick.Count > 0;
        public bool HasUpkeep => upkeepPerTick.Count > 0;
        public bool IsHousing => housingCapacity > 0;
    }
}
