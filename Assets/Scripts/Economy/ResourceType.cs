using UnityEngine;

namespace Puzzle.Economy
{
    /// <summary>
    /// One kind of resource (Wood, Wheat, Money...). Data asset, like an Unreal Data Asset.
    /// Create via Create > Puzzle > Resource, then add it to the GameConfig's Resources list
    /// to show it in the top bar. No code changes needed for new resources.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle/Resource", fileName = "NewResource")]
    public class ResourceType : ScriptableObject
    {
        [Tooltip("Name shown in the UI.")]
        [SerializeField] private string displayName = "Resource";
        [Tooltip("Optional icon for the UI.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Can this go below zero? On for Money (debt); off for goods, which stop at 0.")]
        [SerializeField] private bool allowNegative;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public bool AllowNegative => allowNegative;
    }
}
