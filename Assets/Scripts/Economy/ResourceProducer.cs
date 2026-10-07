using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Added to a placed building by ResourceManager. Adds its output to the pool every interval.
/// Each building has its own timer, so it can later be paused, staffed or demolished individually.
/// </summary>
public class ResourceProducer : MonoBehaviour
{
    private ResourceManager _resources;
    private IReadOnlyList<ResourceAmount> _output;
    private float _interval;
    private float _timer;

    public void Init(ResourceManager resources, IReadOnlyList<ResourceAmount> output, float interval)
    {
        _resources = resources;
        _output = output;
        _interval = Mathf.Max(0.1f, interval);
        _timer = 0f;
    }

    private void Update()
    {
        if (_resources == null || _output == null) return;

        _timer += Time.deltaTime;
        if (_timer < _interval) return;
        _timer -= _interval;

        foreach (ResourceAmount item in _output)
        {
            _resources.Add(item.Type, item.Amount);
        }
    }
}
