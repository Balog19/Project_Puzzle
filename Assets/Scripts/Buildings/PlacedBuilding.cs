using UnityEngine;

/// <summary>
/// Added to every placed building by BuildingPlacer. Says what the building is and which cells it covers.
/// </summary>
public class PlacedBuilding : MonoBehaviour
{
    public BuildingData Data { get; private set; }
    public RectInt Area { get; private set; }

    public void Init(BuildingData data, RectInt area)
    {
        Data = data;
        Area = area;
    }
}
