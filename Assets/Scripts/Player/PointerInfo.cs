using UnityEngine;

namespace Puzzle.Player
{
    /// <summary>
    /// Everything about the mouse for one frame, worked out once by PlayerController and handed to
    /// the current interaction mode, so no mode has to raycast or check the UI itself.
    /// </summary>
    public readonly struct PointerInfo
    {
        /// <summary>Mouse position in screen pixels.</summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>True if the mouse is over a HUD element (clicks should not reach the world).</summary>
        public bool IsOverUI { get; }

        /// <summary>Where the mouse ray hits the grid's plane, if it does.</summary>
        public Vector3? WorldPoint { get; }

        /// <summary>The grid cell under the mouse, or null if off the grid.</summary>
        public Vector2Int? Cell { get; }

        public PointerInfo(Vector2 screenPosition, bool isOverUI, Vector3? worldPoint, Vector2Int? cell)
        {
            ScreenPosition = screenPosition;
            IsOverUI = isOverUI;
            WorldPoint = worldPoint;
            Cell = cell;
        }
    }
}
