using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Order things run in within a single tick.</summary>
public enum TickPhase
{
    Production,  // buildings add resources
    Consumption, // buildings pay upkeep
    Population   // growth and moving into houses
}

/// <summary>
/// The single game clock. Runs a fixed number of ticks per second and steps every registered
/// ITickable in phase order, so all timed logic stays in sync regardless of frame rate.
/// Uses scaled time, so Time.timeScale pauses or speeds up the whole game.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }

    [SerializeField, Min(1)] private int ticksPerSecond = 20;
    [Tooltip("Caps how many ticks run in one frame after a hitch, so the game doesn't spiral.")]
    [SerializeField, Min(1)] private int maxTicksPerFrame = 10;

    /// <summary>Raised after every tick, once all phases have run.</summary>
    public event Action Ticked;

    public int TicksPerSecond => ticksPerSecond;
    public float TickDuration => 1f / ticksPerSecond;
    public int CurrentTick { get; private set; }

    private readonly Dictionary<TickPhase, List<ITickable>> _phases = new Dictionary<TickPhase, List<ITickable>>();
    private readonly TickPhase[] _phaseOrder = (TickPhase[])Enum.GetValues(typeof(TickPhase));

    // Changes made while a tick is running are applied after it, so lists aren't modified mid-loop.
    private readonly List<(ITickable tickable, TickPhase phase, bool add)> _pending =
        new List<(ITickable, TickPhase, bool)>();

    private float _accumulator;
    private bool _ticking;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TickManager: more than one in the scene; ignoring this one.", this);
            enabled = false;
            return;
        }

        Instance = this;
        foreach (TickPhase phase in _phaseOrder)
        {
            _phases[phase] = new List<ITickable>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        _accumulator += Time.deltaTime;

        int ticksThisFrame = 0;
        while (_accumulator >= TickDuration && ticksThisFrame < maxTicksPerFrame)
        {
            _accumulator -= TickDuration;
            RunTick();
            ticksThisFrame++;
        }

        // Too far behind: drop the backlog instead of trying to catch up forever.
        if (ticksThisFrame == maxTicksPerFrame) _accumulator = 0f;
    }

    // ---------- Registration ----------

    public void Register(ITickable tickable, TickPhase phase)
    {
        if (tickable == null) return;

        if (_ticking)
        {
            _pending.Add((tickable, phase, true));
            return;
        }

        List<ITickable> list = _phases[phase];
        if (!list.Contains(tickable)) list.Add(tickable);
    }

    public void Unregister(ITickable tickable)
    {
        if (tickable == null) return;

        if (_ticking)
        {
            _pending.Add((tickable, default, false));
            return;
        }

        foreach (List<ITickable> list in _phases.Values)
        {
            list.Remove(tickable);
        }
    }

    /// <summary>Converts seconds to a whole number of ticks (at least 1).</summary>
    public int SecondsToTicks(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * ticksPerSecond));

    /// <summary>
    /// Registers with the scene's TickManager, logging a clear warning if there isn't one.
    /// Returns false when nothing was registered.
    /// </summary>
    public static bool TryRegister(ITickable tickable, TickPhase phase, UnityEngine.Object context)
    {
        if (Instance == null)
        {
            Debug.LogWarning("No TickManager in the scene: add one so production, upkeep and growth run.", context);
            return false;
        }

        Instance.Register(tickable, phase);
        return true;
    }

    public static void TryUnregister(ITickable tickable)
    {
        if (Instance != null) Instance.Unregister(tickable);
    }

    // ---------- Ticking ----------

    private void RunTick()
    {
        _ticking = true;
        CurrentTick++;

        foreach (TickPhase phase in _phaseOrder)
        {
            List<ITickable> list = _phases[phase];
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Tick();
            }
        }

        _ticking = false;
        ApplyPending();

        Ticked?.Invoke();
    }

    private void ApplyPending()
    {
        foreach (var change in _pending)
        {
            if (change.add) Register(change.tickable, change.phase);
            else Unregister(change.tickable);
        }
        _pending.Clear();
    }
}
