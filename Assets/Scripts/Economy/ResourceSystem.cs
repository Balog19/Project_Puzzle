using System;
using System.Collections.Generic;
using UnityEngine;

namespace Puzzle.Economy
{
    /// <summary>
    /// The global resource pool: how much of each <see cref="ResourceType"/> is in storage.
    /// Every Storage building shares it (they collect into it and deliver from it), the starting
    /// resources begin in it, and building costs are paid from it.
    /// Plain C# object created by Game. Amounts are floats (rates can be fractional).
    /// Goods never go below zero; types with <see cref="ResourceType.AllowNegative"/> (Money) can go into debt.
    /// </summary>
    public class ResourceSystem : IStock
    {
        /// <summary>Raised when a resource's amount changes: the resource and its new amount.</summary>
        public event Action<ResourceType, float> Changed;

        private readonly Dictionary<ResourceType, float> _amounts = new Dictionary<ResourceType, float>();

        /// <param name="startingAmounts">Initial stock (from GameConfig).</param>
        public ResourceSystem(IEnumerable<ResourceAmount> startingAmounts)
        {
            if (startingAmounts == null) return;

            foreach (ResourceAmount start in startingAmounts)
            {
                if (start.Type != null) _amounts[start.Type] = Get(start.Type) + start.Amount;
            }
        }

        // ---------- Queries ----------

        /// <summary>Current amount of a resource (0 if never added). Can be negative for debt types.</summary>
        public float Get(ResourceType type) =>
            type != null && _amounts.TryGetValue(type, out float amount) ? amount : 0f;

        /// <summary>True if every part of the cost is available (debt never counts as available).</summary>
        public bool CanAfford(IReadOnlyList<ResourceAmount> cost)
        {
            if (cost == null) return true;

            foreach (ResourceAmount item in cost)
            {
                if (item.Type != null && Get(item.Type) < item.Amount) return false;
            }
            return true;
        }

        // ---------- Changes ----------

        /// <summary>Adds (or with a negative amount, removes) a resource. Clamped at 0 unless the type allows debt.</summary>
        public void Add(ResourceType type, float amount)
        {
            if (type == null || amount == 0f) return;

            float newAmount = Get(type) + amount;
            if (!type.AllowNegative) newAmount = Mathf.Max(0f, newAmount);

            _amounts[type] = newAmount;
            Changed?.Invoke(type, newAmount);
        }

        /// <summary>Adds every entry in the list (e.g. a building's production for one tick).</summary>
        public void Add(IReadOnlyList<ResourceAmount> amounts)
        {
            if (amounts == null) return;
            foreach (ResourceAmount item in amounts)
            {
                Add(item.Type, item.Amount);
            }
        }

        /// <summary>Removes up to the amount (never into debt) and returns how much was actually taken.</summary>
        public float Take(ResourceType type, float amount)
        {
            if (type == null || amount <= 0f) return 0f;

            float taken = Mathf.Min(amount, Mathf.Max(0f, Get(type)));
            Add(type, -taken);
            return taken;
        }

        /// <summary>
        /// Charges an ongoing cost such as upkeep: debt types go negative if needed,
        /// other types just lose what's there.
        /// </summary>
        public void Charge(ResourceType type, float amount)
        {
            if (type == null || amount <= 0f) return;

            if (type.AllowNegative) Add(type, -amount);
            else Take(type, amount);
        }

        /// <summary>Spends the whole cost, or nothing if any part can't be afforded.</summary>
        public bool TrySpend(IReadOnlyList<ResourceAmount> cost)
        {
            if (!CanAfford(cost)) return false;
            if (cost == null) return true;

            foreach (ResourceAmount item in cost)
            {
                Add(item.Type, -item.Amount);
            }
            return true;
        }
    }
}
