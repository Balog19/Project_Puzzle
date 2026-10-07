using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the town's population. The total grows over time; people are homeless
/// until placed buildings provide housing, and move in as soon as there's room.
/// Houses fill up in the order they were placed.
/// </summary>
public class PopulationManager : MonoBehaviour
{
    [Tooltip("Placed buildings with housing capacity add room for people.")]
    [SerializeField] private BuildingPlacer placer;

    [Header("Growth")]
    [SerializeField, Min(0)] private int startingPopulation = 5;
    [Tooltip("Seconds between population increases.")]
    [SerializeField, Min(0.1f)] private float growthInterval = 10f;
    [SerializeField, Min(0)] private int growthAmount = 1;

    /// <summary>Raised whenever the total, housing or residents change.</summary>
    public event Action Changed;

    public int Total { get; private set; }
    public int HousingCapacity { get; private set; }
    public int Housed => Mathf.Min(Total, HousingCapacity);
    public int Homeless => Total - Housed;
    public IReadOnlyList<House> Houses => _houses;

    private readonly List<House> _houses = new List<House>();
    private float _growthTimer;

    private void Awake()
    {
        Total = startingPopulation;
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
        Changed?.Invoke();
    }

    private void Update()
    {
        _growthTimer += Time.deltaTime;
        if (_growthTimer < growthInterval) return;

        _growthTimer -= growthInterval;
        Total += growthAmount;
        AssignResidents();
    }

    // ---------- Housing ----------

    public void RegisterHouse(House house)
    {
        if (house == null || _houses.Contains(house)) return;
        _houses.Add(house);
        HousingCapacity += house.Capacity;
        AssignResidents();
    }

    /// <summary>For when a house is removed. Anyone over capacity becomes homeless again.</summary>
    public void UnregisterHouse(House house)
    {
        if (!_houses.Remove(house)) return;
        HousingCapacity = Mathf.Max(0, HousingCapacity - house.Capacity);
        AssignResidents();
    }

    /// <summary>Fills houses in placement order until everyone who can be housed is.</summary>
    private void AssignResidents()
    {
        int remaining = Total;
        foreach (House house in _houses)
        {
            int moveIn = Mathf.Min(house.Capacity, remaining);
            house.SetResidents(moveIn);
            remaining -= moveIn;
        }

        Changed?.Invoke();
    }

    private void OnBuildingPlaced(BuildingData building, GameObject instance, RectInt area)
    {
        if (building.HousingCapacity <= 0) return;
        instance.AddComponent<House>().Init(this, building.HousingCapacity);
    }
}
