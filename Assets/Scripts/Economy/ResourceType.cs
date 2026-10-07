using UnityEngine;

/// <summary>
/// One kind of resource (Wood, Stone, Food...). Create via Project window: Create > Puzzle > Resource.
/// Adding a new resource is just a new asset; no code changes needed.
/// </summary>
[CreateAssetMenu(menuName = "Puzzle/Resource", fileName = "NewResource")]
public class ResourceType : ScriptableObject
{
    [SerializeField] private string displayName = "Resource";
    [Tooltip("Optional, for UI later.")]
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
}
