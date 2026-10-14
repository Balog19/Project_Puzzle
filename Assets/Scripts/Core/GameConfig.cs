using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Economy;
using Puzzle.Population;
using UnityEngine;

namespace Puzzle.Core
{
    /// <summary>
    /// Every design value for a game session in one asset (like Unreal's game/project settings).
    /// Assigned to the <see cref="Game"/> object. Create via Create > Puzzle > Game Config.
    /// Adding a resource or building here is all it takes for it to appear in the HUD.
    /// Every rate in the game is per tick (20 ticks = 1 second at the default tick rate), never per second.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid")]
        [Tooltip("Number of cells along world X.")]
        [SerializeField, Min(1)] private int gridWidth = 60;
        [Tooltip("Number of cells along world Z.")]
        [SerializeField, Min(1)] private int gridHeight = 60;
        [Tooltip("Size of one cell in world units (meters).")]
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [Tooltip("Material using the Puzzle/GridOverlay shader.")]
        [SerializeField] private Material gridMaterial;
        [Tooltip("How far the grid overlay floats above the grid origin, to avoid z-fighting with the ground.")]
        [SerializeField, Min(0f)] private float gridOverlayHeight = 0.02f;

        [Header("Time")]
        [Tooltip("Game ticks per second. Production, needs and growth all run on this clock.")]
        [SerializeField, Min(1)] private int ticksPerSecond = 20;
        [Tooltip("Most ticks run in one frame after a hitch.")]
        [SerializeField, Min(1)] private int maxTicksPerFrame = 10;

        [Header("Resources")]
        [Tooltip("Resources shown in the top bar, in this order, with the amount the game starts with.")]
        [SerializeField] private List<ResourceAmount> resources = new List<ResourceAmount>();

        [Header("Buildings")]
        [Tooltip("Buildings in the build menu, in this order.")]
        [SerializeField] private List<BuildingData> buildings = new List<BuildingData>();

        [Header("Population")]
        [Tooltip("People at the start of the game. They start homeless.")]
        [SerializeField, Min(0)] private int startingPopulation = 5;
        [Tooltip("Ticks between population increases.")]
        [SerializeField, Min(1)] private int growthIntervalTicks = 200;
        [Tooltip("People added each growth interval.")]
        [SerializeField, Min(0)] private int growthAmount = 1;
        [Tooltip("Needs every person has, e.g. Hunger. Each pays its own tax while met.")]
        [SerializeField] private List<NeedType> needs = new List<NeedType>();

        [Header("Taxes")]
        [Tooltip("The resource people pay taxes in.")]
        [SerializeField] private ResourceType money;
        [Tooltip("Money each housed person generates per tick (unless an unmet need blocks their income).")]
        [SerializeField, Min(0f)] private float incomePerTickHoused = 0.02f;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;
        public Material GridMaterial => gridMaterial;
        public float GridOverlayHeight => gridOverlayHeight;

        public int TicksPerSecond => ticksPerSecond;
        public int MaxTicksPerFrame => maxTicksPerFrame;

        /// <summary>Resources in display order; Amount is the starting stock.</summary>
        public IReadOnlyList<ResourceAmount> Resources => resources;
        public IReadOnlyList<BuildingData> Buildings => buildings;

        public int StartingPopulation => startingPopulation;
        public int GrowthIntervalTicks => growthIntervalTicks;
        public int GrowthAmount => growthAmount;
        public IReadOnlyList<NeedType> Needs => needs;

        public ResourceType Money => money;
        public float IncomePerTickHoused => incomePerTickHoused;
    }
}
