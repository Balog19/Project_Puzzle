using System.Collections.Generic;
using Puzzle.Core;

namespace Puzzle.Economy
{
    /// <summary>
    /// Adds a building's production to the resource pool every tick. Created by BuildingSystem for
    /// each placed building that produces, and ticked in the Production phase.
    /// </summary>
    public class Producer : ITickable
    {
        /// <summary>What is added each tick.</summary>
        public IReadOnlyList<ResourceAmount> OutputPerTick { get; }

        private readonly ResourceSystem _resources;

        public Producer(ResourceSystem resources, IReadOnlyList<ResourceAmount> outputPerTick)
        {
            _resources = resources;
            OutputPerTick = outputPerTick;
        }

        public void Tick() => _resources.Add(OutputPerTick);
    }
}
