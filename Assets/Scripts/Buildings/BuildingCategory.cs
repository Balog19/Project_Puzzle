using UnityEngine;

namespace Puzzle.Buildings
{
    /// <summary>
    /// A group in the build menu (Housing, Production, Transportation...). Data asset.
    /// Create via Create > Puzzle > Building Category and add it to the GameConfig's Categories list;
    /// it gets its own button in the build bar, and buildings pick it in their Category field.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle/Building Category", fileName = "NewCategory")]
    public class BuildingCategory : ScriptableObject
    {
        [Tooltip("Name shown on the category button.")]
        [SerializeField] private string displayName = "Category";
        [Tooltip("Optional icon shown on the category button.")]
        [SerializeField] private Sprite icon;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
    }
}
