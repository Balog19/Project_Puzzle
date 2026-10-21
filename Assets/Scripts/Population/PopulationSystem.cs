using System;
using System.Collections.Generic;
using Puzzle.Buildings;
using Puzzle.Core;
using Puzzle.Economy;
using Puzzle.Transport;

namespace Puzzle.Population
{
    /// <summary>
    /// Owns every <see cref="Person"/>. Plain C# object created by Game and ticked in the Population phase.
    /// - The population grows over time.
    /// - Homeless people move into houses with room (in placement order); existing residents are never reshuffled.
    /// - Every tick, each person takes what they need from buildings within road reach of their house
    ///   (homeless people can't get anything), then everyone's taxes are added to Money in one go.
    /// </summary>
    public class PopulationSystem : ITickable, IDisposable
    {
        /// <summary>Raised whenever people are added or move in/out.</summary>
        public event Action Changed;

        /// <summary>Raised when a person's need runs out and can't be refilled. Hook penalties up here.</summary>
        public event Action<Person, NeedState> PersonNeedUnmet;

        public int Total => _people.Count;
        public int Housed { get; private set; }
        public int Homeless => Total - Housed;
        public int HousingCapacity { get; private set; }

        /// <summary>People whose income is blocked by an unmet need (e.g. starving), as of the last tick.</summary>
        public int Starving { get; private set; }

        /// <summary>Total taxes paid on the last tick.</summary>
        public float IncomePerTick { get; private set; }
        public IReadOnlyList<Person> People => _people;
        public IReadOnlyList<House> Houses => _houses;

        private readonly GameConfig _config;
        private readonly ResourceSystem _resources;
        private readonly BuildingSystem _buildings;
        private readonly TickSystem _ticks;
        private readonly SupplySystem _supply;
        private readonly List<IStock> _sources = new List<IStock>();
        private static readonly List<IStock> NoSources = new List<IStock>();

        private readonly List<Person> _people = new List<Person>();
        private readonly List<House> _houses = new List<House>();
        private int _nextPersonId = 1;
        private int _growthTicks;

        public PopulationSystem(GameConfig config, ResourceSystem resources, BuildingSystem buildings,
            TickSystem ticks, SupplySystem supply)
        {
            _supply = supply;
            _config = config;
            _resources = resources;
            _buildings = buildings;
            _ticks = ticks;

            for (int i = 0; i < config.StartingPopulation; i++)
            {
                AddPerson();
            }

            _buildings.Placed += OnBuildingPlaced;
            _buildings.Removed += OnBuildingRemoved;
            _ticks.Register(this, TickPhase.Population);
        }

        public void Dispose()
        {
            _buildings.Placed -= OnBuildingPlaced;
            _buildings.Removed -= OnBuildingRemoved;
            _ticks.Unregister(this);
        }

        // ---------- Tick ----------

        public void Tick()
        {
            TickGrowth();

            float income = 0f;
            int starving = 0;
            foreach (Person person in _people)
            {
                person.Tick(SourcesFor(person));
                income += person.IncomePerTick(_config);
                if (person.IsIncomeBlocked) starving++;
            }

            IncomePerTick = income;
            _resources.Add(_config.Money, income);

            if (starving != Starving)
            {
                Starving = starving;
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// What this person can take from, nearest first: producers' own stock, and the shared storage pool
        /// through any storage in road reach (listed once even if several storages reach the house).
        /// </summary>
        private IReadOnlyList<IStock> SourcesFor(Person person)
        {
            if (!person.IsHoused) return NoSources;

            _sources.Clear();
            foreach (Link link in _supply.SuppliersFor(person.Home.Building))
            {
                if (!_sources.Contains(link.Stock)) _sources.Add(link.Stock);
            }
            return _sources;
        }

        private void TickGrowth()
        {
            _growthTicks++;
            if (_growthTicks < _config.GrowthIntervalTicks) return;
            _growthTicks = 0;

            for (int i = 0; i < _config.GrowthAmount; i++)
            {
                AddPerson();
            }
            HouseHomeless();
        }

        // ---------- People ----------

        /// <summary>Adds a new homeless person with every configured need.</summary>
        public Person AddPerson()
        {
            var person = new Person(_nextPersonId++);

            foreach (NeedType need in _config.Needs)
            {
                person.AddNeed(need);
            }

            person.NeedUnmet += (p, need) => PersonNeedUnmet?.Invoke(p, need);
            _people.Add(person);
            Changed?.Invoke();
            return person;
        }

        // ---------- Housing ----------

        /// <summary>Moves a person into a house (out of their current one first). False if the house is full.</summary>
        public bool MoveIn(Person person, House house)
        {
            if (person == null || house == null || !house.HasRoom) return false;
            if (person.Home == house) return true;

            MoveOutQuietly(person);
            house.AddResident(person);
            person.Home = house;
            Housed++;

            Changed?.Invoke();
            return true;
        }

        /// <summary>Makes a person homeless.</summary>
        public void MoveOut(Person person)
        {
            if (MoveOutQuietly(person)) Changed?.Invoke();
        }

        /// <summary>Puts homeless people into houses with room, in placement order.</summary>
        public void HouseHomeless()
        {
            int houseIndex = 0;
            foreach (Person person in _people)
            {
                if (person.IsHoused) continue;

                while (houseIndex < _houses.Count && !_houses[houseIndex].HasRoom) houseIndex++;
                if (houseIndex >= _houses.Count) break;

                MoveIn(person, _houses[houseIndex]);
            }
        }

        private bool MoveOutQuietly(Person person)
        {
            if (person?.Home == null) return false;

            person.Home.RemoveResident(person);
            person.Home = null;
            Housed--;
            return true;
        }

        // ---------- Buildings ----------

        private void OnBuildingPlaced(Building building)
        {
            if (!building.Data.IsHousing) return;

            var house = new House(building, building.Data.HousingCapacity);
            building.House = house;
            _houses.Add(house);
            HousingCapacity += house.Capacity;

            HouseHomeless();
            Changed?.Invoke();
        }

        // Residents become homeless, then get rehoused elsewhere if there's room.
        private void OnBuildingRemoved(Building building)
        {
            House house = building.House;
            if (house == null || !_houses.Remove(house)) return;

            HousingCapacity -= house.Capacity;
            foreach (Person person in new List<Person>(house.Residents))
            {
                MoveOutQuietly(person);
            }

            building.House = null;
            HouseHomeless();
            Changed?.Invoke();
        }
    }
}
