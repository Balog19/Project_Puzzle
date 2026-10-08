using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Added to a placed building by ResourceManager. Every interval it takes its upkeep
/// from the pool, scaled by the residents of the House on the same object.
/// If the pool runs short it takes what's there and flags itself as not supplied.
/// Driven by the TickManager; each building counts its own cycle from when it was placed.
/// </summary>
public class ResourceConsumer : MonoBehaviour, ITickable
{
    // Serialized only so it's visible in the Inspector during Play.
    [SerializeField] private bool isSupplied = true;

    /// <summary>Raised when IsSupplied flips. Hook shortage penalties up here later.</summary>
    public event Action<ResourceConsumer> SupplyChanged;

    public bool IsSupplied => isSupplied;

    /// <summary>0 to 1 progress toward the next upkeep payment.</summary>
    public float Progress => (float)_elapsedTicks / _intervalTicks;
    /// <summary>Seconds between upkeep payments.</summary>
    public float Interval => _interval;
    public IReadOnlyList<ResourceAmount> PerResident => _perResident;

    /// <summary>Residents of the House on this building; upkeep is PerResident times this.</summary>
    public int Residents
    {
        get
        {
            // Looked up lazily: the House may be added after this component.
            if (_house == null) _house = GetComponent<House>();
            return _house != null ? _house.Residents : 0;
        }
    }

    private ResourceManager _resources;
    private IReadOnlyList<ResourceAmount> _perResident;
    private float _interval;
    private int _intervalTicks = 1;
    private int _elapsedTicks;
    private House _house;

    public void Init(ResourceManager resources, IReadOnlyList<ResourceAmount> perResident, float interval)
    {
        _resources = resources;
        _perResident = perResident;
        _interval = Mathf.Max(0.1f, interval);
        _intervalTicks = TickManager.Instance != null ? TickManager.Instance.SecondsToTicks(_interval) : 1;
        _elapsedTicks = 0;
    }

    private void OnEnable() => TickManager.TryRegister(this, TickPhase.Consumption, this);

    private void OnDisable() => TickManager.TryUnregister(this);

    public void Tick()
    {
        if (_resources == null || _perResident == null) return;

        _elapsedTicks++;
        if (_elapsedTicks < _intervalTicks) return;
        _elapsedTicks = 0;

        Consume();
    }

    private void Consume()
    {
        int residents = Residents;

        bool supplied = true;
        foreach (ResourceAmount item in _perResident)
        {
            int needed = item.Amount * residents;
            if (needed <= 0) continue;

            int taken = _resources.Take(item.Type, needed);
            if (taken < needed) supplied = false;
        }

        if (supplied == isSupplied) return;
        isSupplied = supplied;
        SupplyChanged?.Invoke(this);
    }
}
