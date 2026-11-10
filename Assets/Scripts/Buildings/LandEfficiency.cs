using UnityEngine;
public class LandEfficiency : MonoBehaviour // adds how good the land under a biulding is to each building placed by BuildingPlacer
// 1 = normal
// 1.5 = 50% faster production
// 0.5 = 50% slower production
// added before BuildingPlaced fires so ResourceManager reads it
{
    [SerializeField] private float value = 1f; // serialied so visible in inspector
    public BuildingData Building { get; private set; }
    public float Value => value;
    public void Init(BuildingData building, float efficiency)
    {
        Building = building;
        value = efficiency;
    }
}
