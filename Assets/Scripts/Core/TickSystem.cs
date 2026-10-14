using System;
using System.Collections.Generic;
using UnityEngine;

namespace Puzzle.Core
{
    /// <summary>Order things run in within a single tick.</summary>
    public enum TickPhase
    {
        /// <summary>Buildings add resources.</summary>
        Production,
        /// <summary>Things that spend resources (reserved for building upkeep etc.).</summary>
        Consumption,
        /// <summary>Population growth, housing and people's needs.</summary>
        Population
    }

    /// <summary>
    /// The single game clock. Runs a fixed number of ticks per second and steps every registered
    /// <see cref="ITickable"/> in phase order, so all timed logic stays in sync regardless of frame rate.
    /// Created and advanced by <see cref="Game"/>; it follows scaled time, so Time.timeScale
    /// pauses or speeds up the whole game.
    /// </summary>
    public class TickSystem
    {
        /// <summary>Raised after every tick, once all phases have run.</summary>
        public event Action Ticked;

        /// <summary>Ticks per second of game time.</summary>
        public int TicksPerSecond { get; }

        /// <summary>Seconds of game time per tick.</summary>
        public float TickDuration => 1f / TicksPerSecond;

        /// <summary>Number of ticks run since the game started.</summary>
        public int CurrentTick { get; private set; }

        private readonly int _maxTicksPerFrame;
        private readonly TickPhase[] _phaseOrder = (TickPhase[])Enum.GetValues(typeof(TickPhase));
        private readonly Dictionary<TickPhase, List<ITickable>> _phases = new Dictionary<TickPhase, List<ITickable>>();

        // Changes made while a tick is running are applied after it, so lists aren't modified mid-loop.
        private readonly List<(ITickable tickable, TickPhase phase, bool add)> _pending =
            new List<(ITickable, TickPhase, bool)>();

        private float _accumulator;
        private bool _ticking;

        /// <param name="ticksPerSecond">Fixed tick rate.</param>
        /// <param name="maxTicksPerFrame">Caps catch-up after a frame hitch so the game doesn't spiral.</param>
        public TickSystem(int ticksPerSecond, int maxTicksPerFrame)
        {
            TicksPerSecond = Mathf.Max(1, ticksPerSecond);
            _maxTicksPerFrame = Mathf.Max(1, maxTicksPerFrame);

            foreach (TickPhase phase in _phaseOrder)
            {
                _phases[phase] = new List<ITickable>();
            }
        }

        // ---------- Registration ----------

        /// <summary>Starts ticking something in the given phase. Safe to call during a tick.</summary>
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

        /// <summary>Stops ticking something. Safe to call during a tick.</summary>
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

        // ---------- Ticking ----------

        /// <summary>Called by Game every frame with scaled delta time; runs as many ticks as are due.</summary>
        public void Advance(float deltaTime)
        {
            _accumulator += deltaTime;

            int ticksThisFrame = 0;
            while (_accumulator >= TickDuration && ticksThisFrame < _maxTicksPerFrame)
            {
                _accumulator -= TickDuration;
                RunTick();
                ticksThisFrame++;
            }

            // Too far behind: drop the backlog instead of trying to catch up forever.
            if (ticksThisFrame == _maxTicksPerFrame) _accumulator = 0f;
        }

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
}
