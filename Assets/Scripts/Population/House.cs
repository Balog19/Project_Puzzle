using System;
using UnityEngine;

/// <summary>
/// Added to a placed building with housing capacity by PopulationManager.
/// Knows how many people live here; the PopulationManager decides who moves in.
/// </summary>
public class House : MonoBehaviour
{
    // Serialized only so they're visible in the Inspector during Play.
    [SerializeField] private int capacity;
    [SerializeField] private int residents;

    public event Action<House> ResidentsChanged;

    public int Capacity => capacity;
    public int Residents => residents;

    private PopulationManager _population;

    public void Init(PopulationManager population, int houseCapacity)
    {
        _population = population;
        capacity = Mathf.Max(0, houseCapacity);
        _population.RegisterHouse(this);
    }

    /// <summary>Called by PopulationManager when it reassigns residents.</summary>
    internal void SetResidents(int count)
    {
        count = Mathf.Clamp(count, 0, capacity);
        if (count == residents) return;

        residents = count;
        ResidentsChanged?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (_population != null) _population.UnregisterHouse(this);
    }
}
