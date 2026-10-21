using System;
using System.Collections.Generic;
using UnityEngine;

namespace Puzzle.Economy
{
    /// <summary>
    /// Goods held by one building (e.g. the Wheat a Farmland has grown). Producers fill it; people
    /// within road reach take consumed goods from it. Amounts are floats and never go below zero.
    /// Unlike <see cref="ResourceSystem"/> (the global stockpile), these goods are only usable where they are.
    /// </summary>
    public class Inventory : IStock
    {
        /// <summary>Raised when an amount changes: the resource and its new amount.</summary>
        public event Action<ResourceType, float> Changed;

        private readonly Dictionary<ResourceType, float> _amounts = new Dictionary<ResourceType, float>();

        /// <summary>Everything held, with amounts (for display).</summary>
        public IEnumerable<KeyValuePair<ResourceType, float>> Contents => _amounts;

        public float Get(ResourceType type) =>
            type != null && _amounts.TryGetValue(type, out float amount) ? amount : 0f;

        public void Add(ResourceType type, float amount)
        {
            if (type == null || amount <= 0f) return;

            float newAmount = Get(type) + amount;
            _amounts[type] = newAmount;
            Changed?.Invoke(type, newAmount);
        }

        /// <summary>Removes up to the amount and returns how much was actually taken.</summary>
        public float Take(ResourceType type, float amount)
        {
            if (type == null || amount <= 0f) return 0f;

            float taken = Mathf.Min(amount, Get(type));
            if (taken <= 0f) return 0f;

            float newAmount = Get(type) - taken;
            _amounts[type] = newAmount;
            Changed?.Invoke(type, newAmount);
            return taken;
        }
    }
}
