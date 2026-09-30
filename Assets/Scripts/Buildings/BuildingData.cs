using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Building", fileName = "NewBuilding")]
public class BuildingData : ScriptableObject
{
    [SerializeField] private string displayName = "DefaultBuildingName";
    [SerializeField] private GameObject prefab;
    [Tooltip("Footprint in cells")]
    [SerializeField] private Vector2Int size = Vector2Int.one;

    public string DisplayName => displayName;
    public GameObject Prefab => prefab;
    public Vector2Int Size => Vector2Int.Max(size, Vector2Int.one);
}
