using Puzzle.Buildings;
using Puzzle.Economy;
using Puzzle.Grid;
using Puzzle.Population;
using Puzzle.Transport;
using UnityEngine;

namespace Puzzle.Core
{
    /// <summary>
    /// The one object that runs the game (like Unreal's GameMode owning its subsystems).
    /// Place a single "Game" object in the scene and assign a <see cref="GameConfig"/>; it creates every
    /// system as a plain C# object and wires them together. Everything else reaches them through
    /// <see cref="Instance"/>, e.g. <c>Game.Instance.Resources.Get(wood)</c>.
    ///
    /// The grid is centered on this object's position.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Game : MonoBehaviour
    {
        /// <summary>The running game. Set in Awake, before any other script's Awake.</summary>
        public static Game Instance { get; private set; }

        [Tooltip("All design values for this game: grid, tick rate, resources, buildings, population.")]
        [SerializeField] private GameConfig config;

        public GameConfig Config => config;

        // ---------- Systems (created in Awake, in dependency order) ----------

        /// <summary>Cell data and world/cell conversions.</summary>
        public GridMap Grid { get; private set; }
        /// <summary>The grid overlay in the world, including cursor and selection highlights.</summary>
        public GridView GridView { get; private set; }
        /// <summary>The fixed game clock.</summary>
        public TickSystem Ticks { get; private set; }
        /// <summary>The shared storage pool (all storages + the starting stockpile); pays building costs.</summary>
        public ResourceSystem Resources { get; private set; }
        /// <summary>Placing, finding and removing buildings.</summary>
        public BuildingSystem Buildings { get; private set; }
        /// <summary>Road tiles: auto-connecting pieces and road distances.</summary>
        public RoadSystem Roads { get; private set; }
        /// <summary>Road reach between buildings: what houses can take from, what storages collect from.</summary>
        public SupplySystem Supply { get; private set; }
        /// <summary>People, their needs, and housing.</summary>
        public PopulationSystem Population { get; private set; }

        // ---------- Lifecycle ----------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("There is more than one Game object in the scene; disabling this one.", this);
                enabled = false;
                return;
            }

            if (config == null)
            {
                Debug.LogError("Game has no GameConfig assigned. Create one via Create > Puzzle > Game Config.", this);
                enabled = false;
                return;
            }

            Instance = this;

            Grid = new GridMap(config.GridWidth, config.GridHeight, config.CellSize, GridOriginFor(config));
            GridView = GridView.Create(Grid, config.GridMaterial, config.GridOverlayHeight, transform);
            Ticks = new TickSystem(config.TicksPerSecond, config.MaxTicksPerFrame);
            Resources = new ResourceSystem(config.Resources);
            Buildings = new BuildingSystem(Grid, Resources, Ticks, CreateChild("Buildings"));
            Roads = new RoadSystem(Grid, Buildings);
            Supply = new SupplySystem(Roads, Buildings, Resources, Ticks, config.MaxSupplyRoadDistance);
            Population = new PopulationSystem(config, Resources, Buildings, Ticks, Supply);
        }

        private void Update()
        {
            Ticks.Advance(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            Population?.Dispose();
            Supply?.Dispose();
            Roads?.Dispose();
            Instance = null;
        }

        // ---------- Helpers ----------

        /// <summary>Bottom-left corner of cell (0,0), so the grid is centered on this object.</summary>
        private Vector3 GridOriginFor(GameConfig c) =>
            transform.position - new Vector3(c.GridWidth * c.CellSize, 0f, c.GridHeight * c.CellSize) * 0.5f;

        private Transform CreateChild(string childName)
        {
            var child = new GameObject(childName).transform;
            child.SetParent(transform, false);
            return child;
        }

        // ---------- Scene view ----------

        private void OnDrawGizmos()
        {
            if (config == null) return;

            // Grid outline, so the play area is visible while editing.
            Vector3 origin = GridOriginFor(config);
            Vector3 size = new Vector3(config.GridWidth * config.CellSize, 0f, config.GridHeight * config.CellSize);
            Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
            Gizmos.DrawWireCube(origin + size * 0.5f, size);
        }
    }
}
