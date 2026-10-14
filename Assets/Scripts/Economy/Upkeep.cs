using System.Collections.Generic;
using Puzzle.Core;

namespace Puzzle.Economy
{
    /// <summary>
    /// Charges a building's running cost every tick (usually Money). Created by BuildingSystem for
    /// each placed building with upkeep, and ticked in the Consumption phase.
    /// Money can go into debt; the building keeps working either way.
    /// </summary>
    public class Upkeep : ITickable
    {
        /// <summary>What is charged each tick.</summary>
        public IReadOnlyList<ResourceAmount> CostPerTick { get; }

        private readonly ResourceSystem _resources;

        public Upkeep(ResourceSystem resources, IReadOnlyList<ResourceAmount> costPerTick)
        {
            _resources = resources;
            CostPerTick = costPerTick;
        }

        public void Tick()
        {
            foreach (ResourceAmount item in CostPerTick)
            {
                _resources.Charge(item.Type, item.Amount);
            }
        }
    }
}
