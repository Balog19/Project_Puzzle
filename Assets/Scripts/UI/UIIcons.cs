using UnityEngine;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// Shows a sprite in an icon tile, or the first letter of a name when there is no sprite yet,
    /// so every button has an icon from day one.
    /// </summary>
    public static class UIIcons
    {
        /// <param name="tile">The element that shows the sprite as its background.</param>
        /// <param name="letter">The label shown instead when there is no sprite (may be null).</param>
        public static void Apply(VisualElement tile, Label letter, Sprite sprite, string name)
        {
            bool hasSprite = sprite != null;

            tile.style.backgroundImage = hasSprite ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);

            if (letter == null) return;
            letter.text = hasSprite || string.IsNullOrEmpty(name) ? "" : name.Substring(0, 1).ToUpperInvariant();
            letter.style.display = hasSprite ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
