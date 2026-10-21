using System;
using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Economy;
using UnityEngine;

namespace Puzzle.Transport
{
    /// <summary>A building within road reach of another, how far it is, and the stock it gives access to.</summary>
    public readonly struct Link
    {
        public Building Building { get; }
        public int RoadDistance { get; }

        /// <summary>What can be taken through this link: a producer's Inventory, or the shared storage pool for a storage.</summary>
        public IStock Stock { get; }

        public Link(Building building, int roadDistance, IStock stock)
        {
            Building = building;
            RoadDistance = roadDistance;
            Stock = stock;
        }
    }

    /// <summary>
    /// Who can reach whom by road, within GameConfig.MaxSupplyRoadDistance tiles:
    /// - a house can take goods from producers and storages in reach (<see cref="SuppliersFor"/>);
    /// - a storage collects from producers in reach (<see cref="ProducersFor"/>).
    /// All storages share one storage pool (the ResourceSystem). Also gives each storage its
    /// <see cref="StorageCollector"/>. Plain C# object created by Game; reach is cached and rebuilt
    /// only after roads or buildings change.
    /// </summary>
    public class SupplySystem : ITickable, IDisposable
    {
        /// <summary>
        /// Raised (at the start of the next tick) when who-reaches-whom may have changed. Deferred on purpose:
        /// while a building is being placed other systems are still attaching its features (e.g. its House).
        /// </summary>
        public event Action Changed;

        /// <summary>The furthest two buildings can be apart, in road tiles.</summary>
        public int MaxDistance { get; }

        private readonly RoadSystem _roads;
        private readonly BuildingSystem _buildings;
        private readonly ResourceSystem _pool;
        private readonly TickSystem _ticks;

        private readonly Dictionary<Building, List<Link>> _suppliersByHouse = new Dictionary<Building, List<Link>>();
        private readonly Dictionary<Building, List<Link>> _producersByStorage = new Dictionary<Building, List<Link>>();
        private static readonly List<Link> None = new List<Link>();
        private bool _dirty = true;
        private bool _changePending;

        public SupplySystem(RoadSystem roads, BuildingSystem buildings, ResourceSystem pool, TickSystem ticks, int maxDistance)
        {
            _roads = roads;
            _buildings = buildings;
            _pool = pool;
            _ticks = ticks;
            MaxDistance = Mathf.Max(1, maxDistance);

            _roads.Changed += MarkDirty;
            _buildings.Placed += OnPlaced;
            _buildings.Removed += OnRemoved;
            _ticks.Register(this, TickPhase.Production);
        }

        public void Dispose()
        {
            _ticks.Unregister(this);
            _roads.Changed -= MarkDirty;
            _buildings.Placed -= OnPlaced;
            _buildings.Removed -= OnRemoved;
        }

        // ---------- Queries ----------

        /// <summary>Producers and storages a house can take goods from, nearest first.</summary>
        public IReadOnlyList<Link> SuppliersFor(Building house) => Lookup(_suppliersByHouse, house);

        /// <summary>Producers a storage collects from, nearest first.</summary>
        public IReadOnlyList<Link> ProducersFor(Building storage) => Lookup(_producersByStorage, storage);

        /// <summary>How much of a resource is still sitting in producers (not yet in storage).</summary>
        public float TotalStored(ResourceType type)
        {
            float total = 0f;
            foreach (Building building in _buildings.All)
            {
                if (building.Inventory != null) total += building.Inventory.Get(type);
            }
            return total;
        }

        private IReadOnlyList<Link> Lookup(Dictionary<Building, List<Link>> table, Building building)
        {
            if (building == null) return None;
            if (_dirty) Rebuild();
            return table.TryGetValue(building, out List<Link> list) ? list : None;
        }

        // ---------- Buildings ----------

        private void OnPlaced(Building building)
        {
            if (building.Data.IsStorage)
            {
                building.Storage = new StorageCollector(building, this, _pool);
                _ticks.Register(building.Storage, TickPhase.Logistics);
            }
            MarkDirty();
        }

        private void OnRemoved(Building building)
        {
            if (building.Storage != null) _ticks.Unregister(building.Storage);
            MarkDirty();
        }

        private void MarkDirty()
        {
            _dirty = true;
            _changePending = true;
        }

        /// <summary>Announces changes from the last tick, once everything placed has been fully set up.</summary>
        public void Tick()
        {
            if (!_changePending) return;
            _changePending = false;
            _dirty = true;
            Changed?.Invoke();
        }

        // ---------- Cache ----------

        // Walk the roads once from every source (producer or storage) and record which targets
        // (houses, storages) that walk touches.
        private void Rebuild()
        {
            _dirty = false;
            _suppliersByHouse.Clear();
            _producersByStorage.Clear();

            var targets = new List<Building>();
            foreach (Building building in _buildings.All)
            {
                if (building.House != null || building.Storage != null) targets.Add(building);
            }

            foreach (Building source in _buildings.All)
            {
                bool isProducer = source.Inventory != null;
                bool isStorage = source.Storage != null;
                if (!isProducer && !isStorage) continue;

                Dictionary<Vector2Int, int> distances = _roads.DistancesFrom(source, MaxDistance);
                if (distances.Count == 0) continue;

                IStock stock = isStorage ? (IStock)_pool : source.Inventory;

                foreach (Building target in targets)
                {
                    if (target == source) continue;

                    int distance = DistanceTo(target, distances);
                    if (distance == int.MaxValue) continue;

                    var link = new Link(source, distance, stock);
                    if (target.House != null) Add(_suppliersByHouse, target, link);
                    if (target.Storage != null && isProducer) Add(_producersByStorage, target, link);
                }
            }

            foreach (List<Link> list in _suppliersByHouse.Values) list.Sort(ByDistance);
            foreach (List<Link> list in _producersByStorage.Values) list.Sort(ByDistance);
        }

        // Shortest road distance to any road tile touching the target.
        private int DistanceTo(Building target, Dictionary<Vector2Int, int> distances)
        {
            int best = int.MaxValue;
            foreach (Vector2Int road in _roads.RoadsTouching(target.Area))
            {
                if (distances.TryGetValue(road, out int d) && d < best) best = d;
            }
            return best;
        }

        private static void Add(Dictionary<Building, List<Link>> table, Building key, Link link)
        {
            if (!table.TryGetValue(key, out List<Link> list))
            {
                list = new List<Link>();
                table[key] = list;
            }
            list.Add(link);
        }

        private static int ByDistance(Link a, Link b) => a.RoadDistance.CompareTo(b.RoadDistance);
    }
}
