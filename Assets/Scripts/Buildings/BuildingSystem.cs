using System;
using System.Collections.Generic;
using Puzzle.Core;
using Puzzle.Economy;
using Puzzle.Grid;
using UnityEngine;

namespace Puzzle.Buildings
{
    /// <summary>
    /// Places, finds and removes buildings. Plain C# object created by Game.
    /// Placing pays the cost, spawns the prefab, marks the grid cells, and starts production and upkeep;
    /// other systems react through <see cref="Placed"/> / <see cref="Removed"/> (e.g. PopulationSystem adds housing).
    /// </summary>
    public class BuildingSystem
    {
        /// <summary>Raised after a building has been placed and set up.</summary>
        public event Action<Building> Placed;

        /// <summary>Raised just before a building is destroyed.</summary>
        public event Action<Building> Removed;

        /// <summary>Every placed building, in placement order.</summary>
        public IReadOnlyList<Building> All => _buildings;

        private readonly GridMap _grid;
        private readonly ResourceSystem _resources;
        private readonly TickSystem _ticks;
        private readonly Transform _parent;
        private readonly List<Building> _buildings = new List<Building>();
        private readonly Dictionary<Vector2Int, Building> _byCell = new Dictionary<Vector2Int, Building>();

        public BuildingSystem(GridMap grid, ResourceSystem resources, TickSystem ticks, Transform parent)
        {
            _grid = grid;
            _resources = resources;
            _ticks = ticks;
            _parent = parent;
        }

        // ---------- Queries ----------

        /// <summary>The footprint of a building centered on a cell (may be out of bounds).</summary>
        public static RectInt FootprintAt(BuildingData data, Vector2Int centerCell)
        {
            Vector2Int size = data.Size;
            return new RectInt(centerCell - (size - Vector2Int.one) / 2, size);
        }

        public bool IsAreaFree(RectInt area) => _grid.IsAreaAll(area, CellState.Empty);

        public bool CanAfford(BuildingData data) => data != null && _resources.CanAfford(data.Cost);

        /// <summary>True if the area is free and the cost is affordable.</summary>
        public bool CanPlace(BuildingData data, RectInt area) => CanAfford(data) && IsAreaFree(area);

        /// <summary>Total upkeep of one resource across all buildings, per tick.</summary>
        public float UpkeepPerTick(ResourceType type)
        {
            float total = 0f;
            foreach (Building building in _buildings)
            {
                if (building.Upkeep == null) continue;
                foreach (ResourceAmount item in building.Upkeep.CostPerTick)
                {
                    if (item.Type == type) total += item.Amount;
                }
            }
            return total;
        }

        /// <summary>Total production of one resource across all buildings, per tick.</summary>
        public float ProductionPerTick(ResourceType type)
        {
            float total = 0f;
            foreach (Building building in _buildings)
            {
                if (building.Producer == null) continue;
                foreach (ResourceAmount item in building.Producer.OutputPerTick)
                {
                    if (item.Type == type) total += item.Amount;
                }
            }
            return total;
        }

        /// <summary>Every building with at least one cell inside the area, each listed once.</summary>
        public List<Building> GetInArea(RectInt area)
        {
            var found = new List<Building>();
            foreach (Vector2Int cell in area.allPositionsWithin)
            {
                if (TryGetAt(cell, out Building building) && !found.Contains(building)) found.Add(building);
            }
            return found;
        }

        /// <summary>The building covering a cell, if any.</summary>
        public bool TryGetAt(Vector2Int cell, out Building building) =>
            _byCell.TryGetValue(cell, out building) && building != null;

        // ---------- Placing & removing ----------

        /// <summary>Pays for and places a building. Returns null if it can't be placed there.</summary>
        public Building Place(BuildingData data, RectInt area, Quaternion rotation)
        {
            if (data == null)
            {
                Debug.LogWarning("Can't place a building with no data.");
                return null;
            }

            if (!IsAreaFree(area) || !_resources.TrySpend(data.Cost)) return null;

            GameObject instance = Spawn(data, area, rotation);
            Building building = instance.AddComponent<Building>();
            building.Init(data, area);

            _grid.SetArea(area, CellState.Occupied);
            foreach (Vector2Int cell in area.allPositionsWithin)
            {
                _byCell[cell] = building;
            }

            if (data.Produces)
            {
                building.Inventory = new Inventory();
                building.Producer = new Producer(building.Inventory, data.ProductionPerTick);
                _ticks.Register(building.Producer, TickPhase.Production);
            }

            if (data.HasUpkeep)
            {
                building.Upkeep = new Upkeep(_resources, data.UpkeepPerTick);
                _ticks.Register(building.Upkeep, TickPhase.Consumption);
            }

            _buildings.Add(building);
            Placed?.Invoke(building);
            return building;
        }

        // The prefab if there is one. Otherwise an empty object: roads get their piece from RoadSystem,
        // other buildings get a placeholder box until a model is assigned.
        private GameObject Spawn(BuildingData data, RectInt area, Quaternion rotation)
        {
            Vector3 position = _grid.AreaToWorldCenter(area);
            if (data.Prefab != null) return UnityEngine.Object.Instantiate(data.Prefab, position, rotation, _parent);

            var instance = new GameObject(data.DisplayName);
            instance.transform.SetParent(_parent, false);
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            if (!data.IsRoad) Placeholders.Building(data.Size, _grid.CellSize, instance.transform);
            return instance;
        }

        /// <summary>Removes a building: stops its features, frees its cells and destroys it. No refund.</summary>
        public void Remove(Building building)
        {
            if (building == null || !_buildings.Remove(building)) return;

            Removed?.Invoke(building);

            if (building.Producer != null) _ticks.Unregister(building.Producer);
            if (building.Upkeep != null) _ticks.Unregister(building.Upkeep);

            _grid.SetArea(building.Area, CellState.Empty);
            foreach (Vector2Int cell in building.Area.allPositionsWithin)
            {
                _byCell.Remove(cell);
            }

            UnityEngine.Object.Destroy(building.gameObject);
        }
    }
}
