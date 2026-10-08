using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Added to a placed building by ResourceManager. Adds its output to the pool every interval.
/// Driven by the TickManager; each building counts its own cycle from when it was placed.
/// </summary>
public class ResourceProducer : MonoBehaviour, ITickable
{
    private ResourceManager _resources;
    private IReadOnlyList<ResourceAmount> _output;
    private float _interval;
    private int _intervalTicks = 1;
    private int _elapsedTicks;

    /// <summary>0 to 1 progress toward the next output.</summary>
    public float Progress => (float)_elapsedTicks / _intervalTicks;
    /// <summary>Seconds between outputs.</summary>
    public float Interval => _interval;
    public IReadOnlyList<ResourceAmount> Output => _output;

    public void Init(ResourceManager resources, IReadOnlyList<ResourceAmount> output, float interval)
    {
        _resources = resources;
        _output = output;
        _interval = Mathf.Max(0.1f, interval);
        _intervalTicks = TickManager.Instance != null ? TickManager.Instance.SecondsToTicks(_interval) : 1;
        _elapsedTicks = 0;
    }

    private void OnEnable() => TickManager.TryRegister(this, TickPhase.Production, this);

    private void OnDisable() => TickManager.TryUnregister(this);

    public void Tick()
    {
        if (_resources == null || _output == null) return;

        _elapsedTicks++;
        if (_elapsedTicks < _intervalTicks) return;
        _elapsedTicks = 0;

        foreach (ResourceAmount item in _output)
        {
            _resources.Add(item.Type, item.Amount);
        }
    }
}
