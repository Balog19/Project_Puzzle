using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Building", fileName = "NewBuilding")]
public class BuildingData : ScriptableObject
{
    [SerializeField] private string displayName = "DefaultBuildingName";
    [SerializeField] private GameObject prefab;
    [Tooltip("Footprint in cells")]
    [SerializeField] private Vector2Int size = Vector2Int.one;
    [Tooltip("People this building can house")]
    [SerializeField, Min(0)] private int housingCapacity;

    [Header("Cost")]
    [SerializeField] private List<ResourceAmount> cost = new List<ResourceAmount>();

    [Header("Production")]
    [Tooltip("Resources produced every Production Interval once placed. Leave empty for none.")]
    [SerializeField] private List<ResourceAmount> production = new List<ResourceAmount>();
    [SerializeField, Min(0.1f)] private float productionInterval = 10f;

    [Header("Upkeep")]
    [Tooltip("Consumed per resident every Consumption Interval. Only applies to buildings that house people.")]
    [SerializeField] private List<ResourceAmount> consumptionPerResident = new List<ResourceAmount>();
    [SerializeField, Min(0.1f)] private float consumptionInterval = 10f;

    public string DisplayName => displayName;
    public GameObject Prefab => prefab;
    public Vector2Int Size => Vector2Int.Max(size, Vector2Int.one);
    public int HousingCapacity => housingCapacity;
    public IReadOnlyList<ResourceAmount> Cost => cost;
    public IReadOnlyList<ResourceAmount> Production => production;
    public float ProductionInterval => productionInterval;
    public IReadOnlyList<ResourceAmount> ConsumptionPerResident => consumptionPerResident;
    public float ConsumptionInterval => consumptionInterval;
}
