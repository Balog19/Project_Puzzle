using System;
using System.Collections.Generic;
using Puzzle.Core;
using Puzzle.Economy;

namespace Puzzle.Population
{
    /// <summary>One need of one person, e.g. whether this person ate this tick.</summary>
    public class NeedState
    {
        public NeedType Type { get; }

        /// <summary>True if the need was paid for on the last tick (e.g. fed); false = starving.</summary>
        public bool IsMet { get; internal set; } = true;

        /// <summary>"Fed" / "Starving" etc., from the need asset.</summary>
        public string StatusLabel => IsMet ? Type.MetLabel : Type.UnmetLabel;

        public NeedState(NeedType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// A single person. Plain data, not a GameObject: owned, ticked and moved between houses by
    /// PopulationSystem. Each tick they pay for their needs and generate money for each demand that is met
    /// (being housed, being fed...). New per-person state (job, happiness...) goes here.
    /// </summary>
    public class Person
    {
        public int Id { get; }

        /// <summary>The house this person lives in, or null if homeless. Changed via PopulationSystem.MoveIn/MoveOut.</summary>
        public House Home { get; internal set; }

        public bool IsHoused => Home != null;

        public IReadOnlyList<NeedState> Needs => _needs;

        /// <summary>True if an unmet need is stopping all income (e.g. starving).</summary>
        public bool IsIncomeBlocked
        {
            get
            {
                foreach (NeedState need in _needs)
                {
                    if (!need.IsMet && need.Type.StopsAllIncomeWhenUnmet) return true;
                }
                return false;
            }
        }

        /// <summary>Raised when a need goes from met to unmet.</summary>
        public event Action<Person, NeedState> NeedUnmet;

        private readonly List<NeedState> _needs = new List<NeedState>();

        public Person(int id)
        {
            Id = id;
        }

        public void AddNeed(NeedType type)
        {
            if (type != null) _needs.Add(new NeedState(type));
        }

        public NeedState GetNeed(NeedType type) => _needs.Find(n => n.Type == type);

        /// <summary>Pays for every need from the pool. A need is unmet this tick if the pool can't cover it.</summary>
        public void Tick(ResourceSystem pool)
        {
            foreach (NeedState need in _needs)
            {
                ResourceType resource = need.Type.Resource;
                float amount = need.Type.AmountPerTick;

                bool met = resource == null || amount <= 0f || pool.Get(resource) >= amount;
                if (met) pool.Take(resource, amount);

                bool wasMet = need.IsMet;
                need.IsMet = met;
                if (wasMet && !met) NeedUnmet?.Invoke(this, need);
            }
        }

        /// <summary>Money this person generates per tick: housing tax plus one tax per met need, or 0 if blocked.</summary>
        public float IncomePerTick(GameConfig config)
        {
            if (IsIncomeBlocked) return 0f;

            float income = IsHoused ? config.IncomePerTickHoused : 0f;
            foreach (NeedState need in _needs)
            {
                if (need.IsMet) income += need.Type.IncomePerTick;
            }
            return income;
        }

        public override string ToString() => $"Person #{Id}";
    }
}
