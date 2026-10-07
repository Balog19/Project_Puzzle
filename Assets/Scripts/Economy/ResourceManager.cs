using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The global resource pool. Holds how much of each ResourceType the player has,
/// pays building costs, and attaches a ResourceProducer / ResourceConsumer to placed
/// buildings that produce or have upkeep.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    [SerializeField] private List<ResourceAmount> startingResources = new List<ResourceAmount>();
    [Tooltip("Placed buildings with production start producing into this pool.")]
    [SerializeField] private BuildingPlacer placer;

    /// <summary>Raised when a resource's amount changes: the resource and its new amount.</summary>
    public event Action<ResourceType, int> Changed;

    private readonly Dictionary<ResourceType, int> _amounts = new Dictionary<ResourceType, int>();

    private void Awake()
    {
        foreach (ResourceAmount start in startingResources)
        {
            if (start.Type == null) continue;
            _amounts[start.Type] = Get(start.Type) + start.Amount;
        }
    }

    private void OnEnable()
    {
        if (placer != null) placer.BuildingPlaced += OnBuildingPlaced;
    }

    private void OnDisable()
    {
        if (placer != null) placer.BuildingPlaced -= OnBuildingPlaced;
    }

    private void Start()
    {
        // UI may have subscribed before Awake set the starting values.
        foreach (KeyValuePair<ResourceType, int> pair in _amounts)
        {
            Changed?.Invoke(pair.Key, pair.Value);
        }
    }

    // ---------- Queries ----------

    public int Get(ResourceType type) =>
        type != null && _amounts.TryGetValue(type, out int amount) ? amount : 0;

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

    public void Add(ResourceType type, int amount)
    {
        if (type == null || amount == 0) return;

        int newAmount = Mathf.Max(0, Get(type) + amount);
        _amounts[type] = newAmount;
        Changed?.Invoke(type, newAmount);
    }

    /// <summary>Removes up to the amount and returns how much was actually taken (the pool never goes below 0).</summary>
    public int Take(ResourceType type, int amount)
    {
        if (type == null || amount <= 0) return 0;

        int taken = Mathf.Min(amount, Get(type));
        Add(type, -taken);
        return taken;
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

    // ---------- Production & upkeep ----------

    private void OnBuildingPlaced(BuildingData building, GameObject instance, RectInt area)
    {
        if (building.Production.Count > 0)
            instance.AddComponent<ResourceProducer>().Init(this, building.Production, building.ProductionInterval);

        if (building.ConsumptionPerResident.Count > 0)
            instance.AddComponent<ResourceConsumer>().Init(this, building.ConsumptionPerResident, building.ConsumptionInterval);
    }
}
