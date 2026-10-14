using System;
using System.Collections.Generic;
using Puzzle.Buildings;

namespace Puzzle.Population
{
    /// <summary>
    /// The housing feature of a building (reached via <c>building.House</c>). Created by PopulationSystem
    /// for buildings with housing capacity. Only PopulationSystem moves people in or out, so
    /// <see cref="Person.Home"/> and <see cref="Residents"/> always agree.
    /// </summary>
    public class House
    {
        /// <summary>Raised when someone moves in or out.</summary>
        public event Action<House> ResidentsChanged;

        /// <summary>The building this house belongs to.</summary>
        public Building Building { get; }

        public int Capacity { get; }
        public IReadOnlyList<Person> Residents => _residents;
        public int ResidentCount => _residents.Count;
        public bool HasRoom => _residents.Count < Capacity;

        private readonly List<Person> _residents = new List<Person>();

        public House(Building building, int capacity)
        {
            Building = building;
            Capacity = Math.Max(0, capacity);
        }

        internal void AddResident(Person person)
        {
            if (!HasRoom || _residents.Contains(person)) return;
            _residents.Add(person);
            ResidentsChanged?.Invoke(this);
        }

        internal void RemoveResident(Person person)
        {
            if (_residents.Remove(person)) ResidentsChanged?.Invoke(this);
        }
    }
}
