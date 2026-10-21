using System.Collections.Generic;
using Puzzle.Core;

namespace Puzzle.Economy
{
    /// <summary>
    /// Adds a building's production to that building's own <see cref="Inventory"/> every tick.
    /// Goods stay there until people within road reach take them (or, later, storage collects them).
    /// Created by BuildingSystem for each placed building that produces; ticked in the Production phase.
    /// </summary>
    public class Producer : ITickable
    {
        /// <summary>What is made each tick.</summary>
        public IReadOnlyList<ResourceAmount> OutputPerTick { get; }

        private readonly Inventory _target;

        public Producer(Inventory target, IReadOnlyList<ResourceAmount> outputPerTick)
        {
            _target = target;
            OutputPerTick = outputPerTick;
        }

        public void Tick()
        {
            foreach (ResourceAmount item in OutputPerTick)
            {
                _target.Add(item.Type, item.Amount);
            }
        }
    }
}
