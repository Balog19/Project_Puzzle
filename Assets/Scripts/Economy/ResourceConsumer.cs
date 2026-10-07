using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Added to a placed building by ResourceManager. Every interval it takes its upkeep
/// from the pool, scaled by the residents of the House on the same object.
/// If the pool runs short it takes what's there and flags itself as not supplied.
/// </summary>
public class ResourceConsumer : MonoBehaviour
{
    // Serialized only so it's visible in the Inspector during Play.
    [SerializeField] private bool isSupplied = true;

    /// <summary>Raised when IsSupplied flips. Hook shortage penalties up here later.</summary>
    public event Action<ResourceConsumer> SupplyChanged;

    public bool IsSupplied => isSupplied;

    private ResourceManager _resources;
    private IReadOnlyList<ResourceAmount> _perResident;
    private float _interval;
    private float _timer;
    private House _house;

    public void Init(ResourceManager resources, IReadOnlyList<ResourceAmount> perResident, float interval)
    {
        _resources = resources;
        _perResident = perResident;
        _interval = Mathf.Max(0.1f, interval);
        _timer = 0f;
    }

    private void Update()
    {
        if (_resources == null || _perResident == null) return;

        _timer += Time.deltaTime;
        if (_timer < _interval) return;
        _timer -= _interval;

        Consume();
    }

    private void Consume()
    {
        // Looked up lazily: the House may be added after this component.
        if (_house == null) _house = GetComponent<House>();
        int residents = _house != null ? _house.Residents : 0;

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
