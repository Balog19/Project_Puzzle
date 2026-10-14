using System;
using UnityEngine;

namespace Puzzle.Economy
{
    /// <summary>
    /// An amount of one resource, e.g. 5 Wood or 0.03 Wood per tick. Used for costs, production,
    /// upkeep and starting stock, and shows up as an editable row in the Inspector.
    /// Amounts are floats so per-tick rates can be fractional; the UI rounds them for display.
    /// </summary>
    [Serializable]
    public struct ResourceAmount
    {
        [Tooltip("Which resource.")]
        [SerializeField] private ResourceType type;
        [Tooltip("How much of it. For rates, this is per tick.")]
        [SerializeField, Min(0f)] private float amount;

        public ResourceType Type => type;
        public float Amount => amount;

        public ResourceAmount(ResourceType type, float amount)
        {
            this.type = type;
            this.amount = amount;
        }
    }
}
