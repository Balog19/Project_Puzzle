using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Economy;

namespace Puzzle.Transport
{
    /// <summary>
    /// The collecting side of a Storage building (reached via <c>building.Storage</c>). Every tick it moves
    /// goods from producers within road reach, nearest first, into the shared storage pool, up to
    /// BuildingData.StorageCollectPerTick in total. Created by SupplySystem; ticked in the Logistics phase,
    /// after people have eaten, so houses supplied straight from a producer are never starved by storage.
    /// </summary>
    public class StorageCollector : ITickable
    {
        /// <summary>The storage building.</summary>
        public Building Building { get; }

        /// <summary>Total goods moved into the pool on the last tick.</summary>
        public float CollectedLastTick { get; private set; }

        private readonly SupplySystem _supply;
        private readonly ResourceSystem _pool;
        private readonly List<KeyValuePair<ResourceType, float>> _contents = new List<KeyValuePair<ResourceType, float>>();

        public StorageCollector(Building building, SupplySystem supply, ResourceSystem pool)
        {
            Building = building;
            _supply = supply;
            _pool = pool;
        }

        public void Tick()
        {
            float budget = Building.Data.StorageCollectPerTick;
            float collected = 0f;

            foreach (Link producer in _supply.ProducersFor(Building))
            {
                Inventory inventory = producer.Building.Inventory;
                if (inventory == null) continue;

                // Copy first: taking changes the inventory while we'd be looping over it.
                _contents.Clear();
                _contents.AddRange(inventory.Contents);

                foreach (var item in _contents)
                {
                    if (budget - collected <= 0f) break;

                    float taken = inventory.Take(item.Key, budget - collected);
                    _pool.Add(item.Key, taken);
                    collected += taken;
                }

                if (budget - collected <= 0f) break;
            }

            CollectedLastTick = collected;
        }
    }
}
