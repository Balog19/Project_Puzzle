using Puzzle.Economy;
using Puzzle.Population;
using UnityEngine;

namespace Puzzle.Buildings
{
    /// <summary>
    /// A placed building (like an Actor). Added to the spawned prefab by BuildingSystem; don't add it
    /// by hand. Holds what the building is, where it is, and its optional features.
    /// New building features go here as properties, created by the system that owns them.
    /// </summary>
    public class Building : MonoBehaviour
    {
        /// <summary>The building type.</summary>
        public BuildingData Data { get; private set; }

        /// <summary>Cells this building covers.</summary>
        public RectInt Area { get; private set; }

        /// <summary>Its production, or null if it doesn't produce.</summary>
        public Producer Producer { get; internal set; }

        /// <summary>Its running cost, or null if it has none.</summary>
        public Upkeep Upkeep { get; internal set; }

        /// <summary>Its housing, or null if nobody lives here. Set by PopulationSystem.</summary>
        public House House { get; internal set; }

        public string DisplayName => Data != null ? Data.DisplayName : name;

        internal void Init(BuildingData data, RectInt area)
        {
            Data = data;
            Area = area;
            name = data.DisplayName;
        }

        /// <summary>World point just above the top-center of the model, e.g. for floating UI.</summary>
        public Vector3 GetTopCenter(float extraHeight = 0f)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return transform.position + Vector3.up * extraHeight;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return new Vector3(bounds.center.x, bounds.max.y + extraHeight, bounds.center.z);
        }
    }
}
